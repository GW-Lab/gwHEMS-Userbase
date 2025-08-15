// Program..: SummerTime.cs
// Author...: G. Wassink
// Design...:
// Date.....: 25/02/2024 Last revised: 18/06/2025
// Notice...: Copyright 2025, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class

using gwCharger;
using gwEnviline;
using gwLogging;
using gwModbus;
using gwTibber;

namespace GWHEMS.Classes;
internal class SummerTime(TibberApiClient tb, ChargerApiClient wb, ModbusApiClient pv, EnvilineApiClient hp)
{
   /* Process variables */

   /* EPEX spot market Energy (Buying & Selling in Euro cents) */
   private const decimal MyBuyPrice = -0.01M;                                                                  // My Buy price	(excl. tax unit € cent/kWh)
   private const decimal MyBuyMarge = 0.30M;                                                                   // My Buy marge	(excl. tax  ,,  € cent/kWh)
   private const decimal MySalesPrice = 0.35M;                                                                 // My Seles price	(excl. tax  ,,  € cent/kWh) 
   private const decimal MySalesMarge = 0.18M;                                                                 // My Sales marge	(excl. tax  ,,  € cent/kWh)
   private const decimal MyToMorrowSalesPrice = 0.00M;

   /* Battery -> SOC  -> Charge & Discharge power */
   private const int SOCMax = 100;                                                                             // SOC = 100%
   private const int SOCMin = 0;                                                                               // SOC = 0%
   private const int SOCMinBySelling = 50;                                                                     // SOC = 50%            if SOC < 50% stop selling
   private const int BatteryChargePowerMax = -6000;                                                            // Charge power			(Watt)
   private const int BatteryChargePowerMin = -1000;                                                            // Charge power			(Watt)
   private const int BatteryDischargePowerMax = 6250;                                                          // Discharge power		(Watt) 
   private const int BatteryDischargePowerMin = 1000;                                                          // Discharge power		(Watt) 
   private const int BatteryPowerAC = 0;                                                                       // Battery is not Charging or Discharging

   /* EV and EVCharger */
   private const int WBBatteryChargePowerMax = -6000;                                                          // Charge power			(Watt)
   private const int WBBatteryChargePowerMin = -2000;                                                          // Charge power			(Watt)
   private const int WBBatteryDischargePowerMax = 6000;                                                        // Discharge power		(Watt)
   private const int WBBatteryDischargePowerMin = 1000;                                                        // Discharge power		(Watt)
   private const int WBChargeDuration = 120;                                                                   // WB Charge duration   (minutes)  
   private const int WBChargeEnergy = 10;                                                                      // WB Charge energy		(kWh)  
   private ChargingModeEnum Charging = ChargingModeEnum.Stop;

   /* HP and DHW */
   private const int dhwTemp = 0;
   private const int indoorTempMin = 12;
   private const int indoorTempMax = 18;
   private const int indoorTempNormal = 15;
   private const int outdoorTempMin = 0;
   private DHWChargeStatus dhwChargeStatus = DHWChargeStatus.stop;
   private DHWChargeStatus dhwChargeStatusOld = DHWChargeStatus.init;

   /* PV */
   private const int PVActivePowerDCMin = 0;                                                                   // PV Startup powerDC = 0 (Watt) 

   /* General */
   private readonly bool expectedDayYieldMin = true;                                                           // ToDo: Implement weather forcast (solar yield)
   private ProcessStatus status = ProcessStatus.Init;

   public void Process(DateTime currDateTime, OptimizationTargetEnum optimizationTarget)
   {
      //var pvPowerAC = pv.PowerAC;
      //var pvPowerDC = pv.PowerDC;
      //var pvBatteryPower = pv.Battery.Power;

      //Console.WriteLine($"PV AC   -> {pvPowerAC} Watt");
      //Console.WriteLine($"PV DC   -> {pvPowerDC} Watt");
      //Console.WriteLine($"Battery -> {pvBatteryPower} Watt");

      // pv.ActivePowerACLimit = 0;                                                                            // 0%    => Plant in Zero Export mode is working only Battery control is not jet implemented
      // pv.ActivePowerACLimit = 100;                                                                          // 100%  => Plant in Normal mode is working only Battery control is not jet implemented

      AppliancesControl(currDateTime, optimizationTarget);                                                     // General (Allwayes check for low/negative energy prices)

      if (Charging == ChargingModeEnum.PVSurPlus)
      {
         WallBoxPVSurPlusCharging(currDateTime, optimizationTarget);
      }
      else if (Charging == ChargingModeEnum.Fast)
      {
         BallBoxFastCharging(currDateTime, optimizationTarget);
      }
      else                                                                                                     // WallBox (EVCharging) are not charging 
      {
         if (status == ProcessStatus.BatteryChargeDelayed)
         {
            if (currDateTime.Hour >= tb.ToDay.BatteryChargeDelayedToHour)
            {
               pv.Battery.Control(BatteryControlEnum.Inverter);

               status = ProcessStatus.BatteryChargeFromPV;
            }
            else
            {
               pv.Battery.Control(BatteryControlEnum.Remote);
            }
         }
         else if (expectedDayYieldMin &&
                  currDateTime.Hour < tb.ToDay.BatteryChargeDelayedToHour &&
                  pv.PowerDC > PVActivePowerDCMin &&                                                          // DC power (String power > 0 Watt)
                  pv.Battery.PowerAC <= BatteryPowerAC)                                                       // Battery power = 0 Watt (not discharging)
         {
            pv.Battery.Control(BatteryControlEnum.Remote);
            status = ProcessStatus.BatteryChargeDelayed;
            
            Logging.Log("Battery", $"Charge delayed to {tb.ToDay.BatteryChargeDelayedToHour}:00");
         }
         else
         {
            IdleState(currDateTime);
         }
      }
   }

   private void BallBoxFastCharging(DateTime currDateTime, OptimizationTargetEnum optimizationTarget)
   {
      Logging.Log("WB", "Charging", "", $"{Charging}");

      if (status == ProcessStatus.BatteryChargeDelayed)                                                        // To Do Battary allways => SOC >= 50% 
      {
         if (tb.ToDay.Prices[currDateTime.Hour].Energy < MyBuyPrice)                                           // Negative prices get energy (allways) from the GRID 
         {
            if (optimizationTarget == OptimizationTargetEnum.Economical)
               pv.Battery.PowerACForced = WBBatteryChargePowerMin;                                             // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control(BatteryControlEnum.Inverter);
         }
         else                                                                                                  // Positive prices get energy from the Battery? 
         {
            if (optimizationTarget == OptimizationTargetEnum.Economical)
               pv.Battery.PowerACForced = WBBatteryChargePowerMax;                                             // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control(BatteryControlEnum.Inverter);
         }
      }
      else                                                                                                     // Battery charging not delayed
      {
         if (tb.ToDay.Prices[currDateTime.Hour].Energy < MyBuyPrice)                                           // Negative prices get energy (allways) from the GRID 
         {
            if (optimizationTarget == OptimizationTargetEnum.Economical)
               pv.Battery.PowerACForced = WBBatteryChargePowerMax;                                             // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control(BatteryControlEnum.Remote);
         }
         else
         {
            if (optimizationTarget == OptimizationTargetEnum.Economical)
               pv.Battery.Control(BatteryControlEnum.Remote);                                                  // Set BatteryControll = Remote
            else
               pv.Battery.Control(BatteryControlEnum.Remote);
         }
      }
   }

   private void WallBoxPVSurPlusCharging(DateTime currDateTime, OptimizationTargetEnum optimizationTarget)
   {
      Logging.Log("WB", "Charging", "", $"{Charging}");

      if (status == ProcessStatus.BatteryChargeDelayed)
      {
         if (tb.ToDay.Prices[currDateTime.Hour].Energy < MyBuyPrice)                                           // Negative prices get energy (allways) from the GRID 
         {
            if (optimizationTarget == OptimizationTargetEnum.Economical)
               pv.Battery.PowerACForced = WBBatteryChargePowerMin;                                             // Set BatteryControll = Remote and charge battery with 2000 watt
            else
               pv.Battery.Control(BatteryControlEnum.Inverter);
         }
         else
         {
            if (optimizationTarget == OptimizationTargetEnum.Economical)
               pv.Battery.Control(BatteryControlEnum.Inverter);                                                // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control(BatteryControlEnum.Inverter);
         }
      }
      else                                                                                                     // Battery charging not delayed
      {
         if (tb.ToDay.Prices[currDateTime.Hour].Energy < MyBuyPrice)                                           // Negative prices get energy (allways) from the GRID 
         {
            if (optimizationTarget == OptimizationTargetEnum.Economical)
               pv.Battery.PowerACForced = WBBatteryChargePowerMax;                                             // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control(BatteryControlEnum.Inverter);
         }
         else
         {
            if (optimizationTarget == OptimizationTargetEnum.Economical)
               pv.Battery.Control(BatteryControlEnum.Inverter);                                                // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control(BatteryControlEnum.Inverter);
         }
      }
   }

   private void IdleState(DateTime currDateTime)
   {
      if (tb.ToDay.Prices[currDateTime.Hour].Energy < MyBuyPrice)                                              // Test for low/negative energy price(s)
      {
         pv.Battery.PowerACForced = BatteryChargePowerMax;                                                     // Set BatteryControll = Remote and Charge battery with 3000 watt

         status = ProcessStatus.BatteryChargedFromGrid;
      }
      else if (tb.ToMorrow != null &&
               currDateTime.Hour == tb.ToDay.EnergyMaxHour &&
              (MyToMorrowSalesPrice > tb.ToMorrow.MinPrice || Math.Abs(tb.ToDay.MaxPrice - tb.ToMorrow.MinPrice) > MySalesMarge) && // TODO: Remove "tb.ToMorrow.MinPrice < MyBuyPrice (if salderingsregeling expieres)           
               pv.Battery.SOC >= SOCMinBySelling)
      {
         pv.Battery.PowerACForced = BatteryDischargePowerMax;                                                  // Set BatteryControll = Remote and Discharge battery with 5500 watt

         status = ProcessStatus.BatteryDischargeToGrid;
      }
      else if (status != ProcessStatus.Idle)
      {
         pv.Battery.Control(BatteryControlEnum.Inverter);
         status = ProcessStatus.Idle;                                                                          // Do nothing until day changed to next day
        
         Logging.Log("Hems", "Idle mode", "", "start");
      }
   }

   private void AppliancesControl(DateTime currDateTime, OptimizationTargetEnum optimizationTarget)
   {
      // WallBox
      this.Charging = wb.Status == ChargerStatusEnum.Charging
         ? wb.RotarySwitchChargeMode == ChargerSwitchEnum.Fast ? ChargingModeEnum.Fast : ChargingModeEnum.PVSurPlus
         : ChargingModeEnum.Stop;

      // Season Summer No Home Heating, Only DHW Heating by Heat Pump compressor or Heating-Element
      dhwChargeStatus = tb.ToDay.Prices[currDateTime.Hour].Energy <= MyBuyPrice ? DHWChargeStatus.start : DHWChargeStatus.stop;  // Negative energy-prices make hot water

      if (dhwChargeStatus != dhwChargeStatusOld)
      {
         if (optimizationTarget == OptimizationTargetEnum.Economical)
         {
            hp.DHWCharge(dhwChargeStatus);                                                                                       // Negative energy-prices make hot water (With the Electric heating-rod)

            Logging.Log("HP", "DHW Electic-Element", "", $"{dhwChargeStatus}");
         }
         else
         {
            hp.DHWoperationMode = dhwChargeStatus == DHWChargeStatus.start ? DHWOperationmode.eco : DHWOperationmode.Off;        // Negative energy-prices make hot water (With the HP compressor)

            Logging.Log("HP", "DHW Compressor", "", $"{dhwChargeStatus}");
         }
         dhwChargeStatusOld = dhwChargeStatus;
      }
   }
}
