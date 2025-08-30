// Program..: WinterTime.cs
// Author...: G. Wassink
// Design...:
// Date.....: 25/02/2024 Last revised: 22/08/2025
// Notice...: Copyright 2025, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class

using gwEnviline;
using gwEVCharger;
using gwLogging;
using gwModbus;
using gwTibber;

namespace gwHEMS.Classes;
internal class WinterTime(TibberApiClient tb, ChargerApiClient wb, HomeManagerApiClient hm, PVInverterApiClient pv, EnvilineApiClient hp)
{
   /* EPEX spot market Energy (Buying & Selling in Euro cents) */
   private const decimal MyBuyPrice = 0.0M;                                                           // My Buy price	(excl. tax unit €/kWh)
   private const decimal MyBuyMarge = 0.35M;                                                          // My Buy marge	(excl. tax  ,,  €/kWh)
   private const decimal MySalesPrice = 0.35M;                                                        // My Seles price	(excl. tax  ,,  €/kWh) 
   private const decimal MySalesMarge = 0.55M;                                                        // My Sales marge	(excl. tax  ,,  €/kWh)
   private const decimal MyToMorrowSalesPrice = 0.00M;

   /* Battery -> SOC  -> Charge & Discharge power */
   private const int SOCMax = 100;                                                                    // SOC = 100%
   private const int SOCMin = 10;                                                                     // SOC = 10%
   private const int SOCBuyMin = 35;                                                                  // SOC = 35%
   private const int SOCMinBySelling = 50;                                                            // SOC = 50%   if SOC < 50% stop selling
   private const int SOCPVSurplusSetPoint = 95;                                                       // SOC = 95%

   /* EVCharger  */
   private const int WBChargeDuration = 120;                                                          // WB Charge duration (minutes)  
   private const int WBChargeEnergy = 10;                                                             // WB Charge energy	 (kWh)  

   /* HM -> Sunny Home manager */

   /* Heatpump */
   private const int dhwTemp = 0;
   private const int indoorTempMin = 12;
   private const int indoorTempMax = 20;
   private const int indoorTempNormal = 15;
   private const int outdoorTempMin = 0;

   private DHWOperationMode dhwOperationMode = DHWOperationMode.Off;
   private DHWOperationMode dhwOperationModeOld = DHWOperationMode.notset;

   private DHWChargeStatus dhwChargeStatus = DHWChargeStatus.stop;
   // private DHWChargeStatus dhwChargeStatusOld = DHWChargeStatus.init;
   /* PV -> Photovoltaics*/
   
   private const int PVActivePowerDCMin = 0;                                                          // PV Startup powerDC = 0 (Watt) 

   /* WallBox EVCharger */
   private ChargingMode Charging = ChargingMode.Stop;

   /* General */
   private readonly bool expectedDayYieldMin = false;                                                 // ToDo: Implement weather forcast (solar yield)
   private ProcessStatus status = ProcessStatus.Init;

   public void Process(DateTime currDateTime, OptimizationTarget optimization)
   {
      AppliancesControl(currDateTime, optimization);                                                  // General (Allwayes check for negative energy prices)

      // hm.ActivePowerACLimit = 0;

      if (Charging == ChargingMode.PVSurPlus)
      {
         WallBoxPVSurPlusCharging(currDateTime, optimization);
      }
      else if (Charging == ChargingMode.Fast)
      {
         BallBoxFastCharging(currDateTime, optimization);
      }
      else if (tb.ToDay.Prices(startsAt: currDateTime).Energy < MyBuyPrice)                           // Test for negative energy price(s)
      {
         if (optimization == OptimizationTarget.Economical)
         {
            pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMax;                                  // Set BatteryControl = Remote and charge battery with 1000 watt

            Logging.Log("Battery", "Charge from grid:");
         }
         else
         {
            hm.PVPlantActivePowerACLimit(PowerPercentage.ZeroExport, pv);                               // Set zero export mode (BatteryChargeFromPV is also triggert)
            pv.Battery.Control = BatteryControl.Inverter;
         }         
      }
      else  // No WallBox Charging or Energy buying
      {
         if (status == ProcessStatus.BatteryChargeTimeShift)
         {
            if (tb.ToDay.TimeSlot(startsAt: currDateTime) >= tb.ToDay.BatteryTimeShiftChargingToTimeSlot)
            {
               pv.Battery.Control = BatteryControl.Inverter;

               status = ProcessStatus.BatteryChargeFromPV;
            }
            else
            {
               pv.Battery.Control = BatteryControl.Remote;
            }
         }
         else if ((optimization == OptimizationTarget.Economical || optimization == OptimizationTarget.CostAware) &&
                  expectedDayYieldMin &&
                  tb.ToDay.TimeSlot(startsAt: currDateTime) < tb.ToDay.BatteryTimeShiftChargingToTimeSlot &&
                  pv.PowerDC > PVActivePowerDCMin &&                                                      // DC power (String power > 0 Watt)
                  pv.Battery.Power <= 0)
         {
            pv.Battery.Control = BatteryControl.Remote;
            status = ProcessStatus.BatteryChargeTimeShift;

            Logging.Log("Battery", $"Charging starts {tb.ToDay.Prices(tb.ToDay.BatteryTimeShiftChargingToTimeSlot).StartsAt:HH:mm}", "", "(time-shift charging)");
         }
         else
         {
            IdleState(currDateTime);
         }
      }
   }

   private void BallBoxFastCharging(DateTime currDateTime, OptimizationTarget optimization)
   {
      Logging.Log("WB", "Charging", "", $"{Charging}");

      if (status == ProcessStatus.BatteryChargeTimeShift)                                       // To Do Battary allways => SOC >= 50% 
      {
         if (tb.ToDay.Prices(startsAt: currDateTime).Energy < MyBuyPrice)                       // Negative prices get energy (allways) from the GRID 
         {
            if (optimization == OptimizationTarget.Economical)
               pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMin;                    // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control = BatteryControl.Inverter;
         }
         else                                                                                   // Positive prices get energy from the Battery? 
         {
            if (optimization == OptimizationTarget.Economical)
               pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMin;                    // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control = BatteryControl.Inverter;
         }
      }
      else                                                                                      // Battery charging not delayed
      {
         if (tb.ToDay.Prices(startsAt: currDateTime).Energy < MyBuyPrice)                       // Negative prices get energy (allways) from the GRID 
         {
            if (optimization == OptimizationTarget.Economical)
               pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMin;                    // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control = BatteryControl.Remote;
         }
         else
         {
            if (optimization == OptimizationTarget.Economical)
               pv.Battery.Control = BatteryControl.Remote;                                       // Set BatteryControll = Remote
            else
               pv.Battery.Control = BatteryControl.Remote;
         }
      }
   }

   private void WallBoxPVSurPlusCharging(DateTime currDateTime, OptimizationTarget optimization)
   {
      Logging.Log("WB", "Charging", "", $"{Charging}");

      if (status == ProcessStatus.BatteryChargeTimeShift)
      {
         if (tb.ToDay.Prices(startsAt: currDateTime).Energy < MyBuyPrice)                       // Negative prices get energy (allways) from the GRID 
         {
            if (optimization == OptimizationTarget.Economical)
               pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMin;                    // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control = BatteryControl.Inverter;
         }
         else
         {
            if (optimization == OptimizationTarget.Economical)
               pv.Battery.Control = BatteryControl.Inverter;                                     // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control = BatteryControl.Inverter;
         }
      }
      else                                                                                      // Battery charging not delayed
      {
         if (tb.ToDay.Prices(startsAt: currDateTime).Energy < MyBuyPrice)                       // Negative prices get energy (allways) from the GRID 
         {
            if (optimization == OptimizationTarget.Economical)
               pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMax;                    // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control = BatteryControl.Inverter;
         }
         else
         {
            if (optimization == OptimizationTarget.Economical)
               pv.Battery.Control = BatteryControl.Inverter;                                     // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control = BatteryControl.Inverter;
         }
      }
   }

   private void IdleState(DateTime currDateTime)
   {
      if (tb.ToDay.Prices(startsAt: currDateTime).Energy < MyBuyPrice)                          // Test for negative energy price(s)
      {
         pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMax;                          // BatteryChargePowerMax; // Set BatteryControll = Remote and Charge battery with 3000 watt
         status = ProcessStatus.BatteryChargedFromGrid;

         Logging.Log("Battery", "Charge from grid");
      }
      else if (tb.ToMorrow != null &&
               tb.ToDay.TimeSlot(startsAt: currDateTime) >= tb.ToDay.MaxPriceTimeSlotAfterBatteryTimeShiftChargingTimeSlot &&
              (tb.ToMorrow.MinPrice < MyToMorrowSalesPrice ||
               Math.Abs(tb.ToDay.MaxPrice - tb.ToMorrow.MinPrice) > MySalesMarge) &&            // TODO: Remove "tb.ToMorrow.MinPrice < MyBuyPrice (if salderingsregeling expieres)           
               pv.Battery.SOC >= SOCMinBySelling)
      {
         pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMin;                          //  WBBatteryDischargePowerMin; // Set BatteryControll = Remote and Discharge battery with 5500 watt

         status = ProcessStatus.BatteryDischargeToGrid;
      }
      else if (status != ProcessStatus.Idle)
      {
         pv.Battery.Control = BatteryControl.Inverter;
         hm.PVPlantActivePowerACLimit(PowerPercentage.Maximum, pv);                             // Cancel Zero export mode (automatically triggert -> BatteryChargeFromPV)  

         status = ProcessStatus.Idle;                                                           // Do nothing until day changed to next day

         Logging.Log("HEMS", "Idle mode", "", $"{pv.Battery.Control} control");
      }
   }

   private void AppliancesControl(DateTime currDateTime, OptimizationTarget optimization)
   {
      // WallBox
      this.Charging = wb.Status == Status.Charging
         ? wb.RotaryKnopPosition == RotaryKnop.Fast ? ChargingMode.Fast : ChargingMode.PVSurPlus
         : ChargingMode.Stop;

      // Heating by HeatPump Compressor
      dhwOperationMode = tb.ToDay.Prices(startsAt: currDateTime).Energy <= MyBuyPrice ? DHWOperationMode.eco : DHWOperationMode.Off;  // Negative energy-prices make hot water (With the HP Compressor)
      // Heating by Electric Element
      dhwChargeStatus = tb.ToDay.Prices(startsAt: currDateTime).Energy <= MyBuyPrice ? DHWChargeStatus.start : DHWChargeStatus.stop;  // Negative energy-prices make hot water (With the Electric Heating-Element)

      if (dhwOperationMode != dhwOperationModeOld)
      {
         hp.IndoorTempSetpoint(dhwOperationMode == DHWOperationMode.eco ? indoorTempMax : indoorTempNormal);                           // Allways Set HP Indoortemp

         if (optimization == OptimizationTarget.Economical)
            hp.DHWCharge(dhwChargeStatus);
         else
            hp.DHWoperationMode = dhwChargeStatus == DHWChargeStatus.start ? DHWOperationMode.eco : DHWOperationMode.Off;

         hp.DHWoperationMode = dhwOperationMode;

         dhwOperationModeOld = dhwOperationMode;
         Logging.Log("HP", "DHW Compressor", "", $"{dhwOperationMode}");
      }
   }
}