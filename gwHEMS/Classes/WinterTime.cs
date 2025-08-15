// Program..: WinterTime.cs
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
internal class WinterTime(TibberApiClient tb, ChargerApiClient wb, ModbusApiClient pv, EnvilineApiClient hp)
{
   /* EPEX spot market Energy (Buying & Selling in Euro cents) */
   private const decimal MyBuyPrice = 0.0M;                                                                 // My Buy price	(excl. tax unit €/kWh)
   private const decimal MyBuyMarge = 0.35M;                                                                // My Buy marge	(excl. tax  ,,  €/kWh)
   private const decimal MySalesPrice = 0.35M;                                                              // My Seles price	(excl. tax  ,,  €/kWh) 
   private const decimal MySalesMarge = 0.55M;                                                              // My Sales marge	(excl. tax  ,,  €/kWh)
   private const decimal MyToMorrowSalesPrice = 0.00M;

   /* Battery -> SOC  -> Charge & Discharge power */
   private const int SOCMax = 100;                                                                          // SOC = 100%
   private const int SOCMin = 10;                                                                           // SOC = 10%
   private const int SOCBuyMin = 35;                                                                        // SOC = 35%
   private const int SOCMinBySelling = 50;                                                                  // SOC = 50%               if SOC < 50% stop selling
   private const int SOCPVSurplusSetPoint = 95;                                                             // SOC = 95%
   private const int BatteryChargePowerMin = -1000;                                                         // Charge power		      (Watt) 
   private const int BatteryChargePowerMax = -6000;                                                         // Charge power		      (Watt) 
   private const int BatteryDischargePowerMin = 1000;                                                       // Discharge power		   (Watt) 
   private const int BatteryDischargePowerMax = 6000;                                                       // Discharge power		   (Watt) 
   /* EV */
   private const int WBBatteryChargePowerMax = -6000;                                                       // WallBox Charge power	   (Watt)
   private const int WBBatteryChargePowerMin = -1000;                                                       // WallBox Charge power	   (Watt) 
   private const int WBBatteryDischargePowerMin = 1000;                                                     // WallBox Discharge power (Watt) 
   private const int WBBatteryDischargePowerMax = 6000;                                                     // WallBox Discharge power	(Watt) 
   private const int WBChargeDuration = 120;                                                                // WB Charge duration      (minutes)  
   private const int WBChargeEnergy = 10;                                                                   // WB Charge energy		   (kWh)  

   /* Heatpump */
   private const int dhwTemp = 0;
   private const int indoorTempMin = 12;
   private const int indoorTempMax = 20;
   private const int indoorTempNormal = 15;
   private const int outdoorTempMin = 0;

   private DHWOperationmode dhwOperationMode = DHWOperationmode.Off;
   private DHWOperationmode dhwOperationModeOld = DHWOperationmode.notset;

   private DHWChargeStatus dhwChargeStatus = DHWChargeStatus.stop;
   // private DHWChargeStatus dhwChargeStatusOld = DHWChargeStatus.init;

   /* WallBox EVCharger */
   private ChargingModeEnum Charging = ChargingModeEnum.Stop;

   /* General */
   private readonly bool expectedDayYieldMin = false;                                                       // ToDo: Implement weather forcast (solar yield)
   private ProcessStatus status = ProcessStatus.Init;

   public void Process(DateTime currDateTime, OptimizationTargetEnum optimizationTarget)
   {
      AppliancesControl(currDateTime, optimizationTarget);                                                  // General (Allwayes check for negative energy prices)

      if (Charging == ChargingModeEnum.PVSurPlus)
      {
         WallBoxPVSurPlusCharging(currDateTime, optimizationTarget);
      }
      else if (Charging == ChargingModeEnum.Fast)
      {
         BallBoxFastCharging(currDateTime, optimizationTarget);
      }
      else if (optimizationTarget == OptimizationTargetEnum.Economical && tb.ToDay.Prices[currDateTime.Hour].Energy < MyBuyPrice)   // Test for negative energy price(s)
      {
         pv.Battery.PowerACForced = BatteryChargePowerMin;                                                                          // Set BatteryControll = Remote and Charge battery with 3000 watt
         status = ProcessStatus.BatteryChargedFromGrid;
         
         Logging.Log("Battery", "Charge from grid:");
      }
      else  // No WallBox Charging or Energy buying
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
                  pv.PowerDC < 0 &&
                  tb.ToDay.MinPrice <= MyBuyPrice)
         {
            pv.Battery.Control(BatteryControlEnum.Remote);
            status = ProcessStatus.BatteryChargeDelayed;
            
            Logging.Log("Battery", $"Charge delayed to: {tb.ToDay.BatteryChargeDelayedToHour}:00");
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
               pv.Battery.PowerACForced = WBBatteryChargePowerMin;                                             // Set BatteryControll = Remote and charge battery with 1000 watt
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
      if (tb.ToDay.Prices[currDateTime.Hour].Energy < MyBuyPrice)                                              // Test for negative energy price(s)
      {
         pv.Battery.PowerACForced = BatteryChargePowerMax;                                                     // Set BatteryControll = Remote and Charge battery with 3000 watt
         status = ProcessStatus.BatteryChargedFromGrid;
        
         Logging.Log("Battery", "Charge from grid");
      }
      else if (tb.ToMorrow != null &&
               currDateTime.Hour == tb.ToDay.EnergyMaxHour &&
              (tb.ToMorrow.MinPrice < MyToMorrowSalesPrice || Math.Abs(tb.ToDay.MaxPrice - tb.ToMorrow.MinPrice) > MySalesMarge) && // TODO: Remove "tb.ToMorrow.MinPrice < MyBuyPrice (if salderingsregeling expieres)           
               pv.Battery.SOC >= SOCMinBySelling)
      {
         pv.Battery.PowerACForced = WBBatteryDischargePowerMin;                                                // Set BatteryControll = Remote and Discharge battery with 5500 watt

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

      // Heating by HeatPump Compressor
      dhwOperationMode = tb.ToDay.Prices[currDateTime.Hour].Energy <= MyBuyPrice ? DHWOperationmode.eco : DHWOperationmode.Off;  // Negative energy-prices make hot water (With the HP Compressor)
      // Heating by Electric Element
      dhwChargeStatus = tb.ToDay.Prices[currDateTime.Hour].Energy <= MyBuyPrice ? DHWChargeStatus.start : DHWChargeStatus.stop;  // Negative energy-prices make hot water (With the Electric Heating-Element)

      if (dhwOperationMode != dhwOperationModeOld)
      {
         hp.IndoorTempSetpoint(dhwOperationMode == DHWOperationmode.eco ? indoorTempMax : indoorTempNormal);                     // Allways Set HP Indoortemp

         if (optimizationTarget == OptimizationTargetEnum.Economical)
            hp.DHWCharge(dhwChargeStatus);
         else
            hp.DHWoperationMode = dhwChargeStatus == DHWChargeStatus.start ? DHWOperationmode.eco : DHWOperationmode.Off;

         hp.DHWoperationMode = dhwOperationMode;

         dhwOperationModeOld = dhwOperationMode;
         Logging.Log("HP", "DHW Compressor", "", $"{dhwOperationMode}");
      }
   }
}