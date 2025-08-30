// Program..: SummerTime.cs
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
internal class SummerTime(TibberApiClient tb, ChargerApiClient wb, HomeManagerApiClient hm, PVInverterApiClient pv, EnvilineApiClient hp)
{
   /* Process variables */

   /* EPEX spot market Energy (Buying & Selling in Euro cents) */
   private const decimal MyBuyPrice = -0.006M;                                            // My Buy price	(excl. tax unit € cent/kWh)
   private const decimal MyBuyMarge = 0.30M;                                              // My Buy marge	(excl. tax  ,,  € cent/kWh)
   private const decimal MySalesPrice = 0.00M;                                            // My Sales price (excl. tax  ,,  € cent/kWh)
   private const decimal MySalesMarge = 0.18M;                                            // My Sales marge	(excl. tax  ,,  € cent/kWh)

   /* Battery -> SOC  -> Charge & Discharge power */
   private const int SOCMax = 100;                                                        // SOC = 100%
   private const int SOCMin = 0;                                                          // SOC = 0%
   private const int BySellingSOCMin = 50;                                                // SOC = 50%            if SOC < 50% stop selling

   /* EV -> EVCharger */
   private const int WBChargeDuration = 120;                                              // WB Charge duration   (minutes)  
   private const int WBChargeEnergy = 10;                                                 // WB Charge energy		(kWh)  
   private ChargingMode Charging = ChargingMode.Stop;

   /* HM -> Sunny Home manager */

   /* HP -> Heat pump, DHW Domestic Hot Watter */
   private const int dhwTemp = 0;
   private const int indoorTempMin = 12;
   private const int indoorTempMax = 18;
   private const int indoorTempNormal = 15;
   private const int outdoorTempMin = 0;
   private DHWChargeStatus dhwChargeStatus = DHWChargeStatus.stop;
   private DHWChargeStatus dhwChargeStatusOld = DHWChargeStatus.init;

   /* PV -> Photovoltaics*/
   private const int PVActivePowerDCMin = 0;                                              // PV Startup powerDC = 0 (Watt) 

   /* General */
   private readonly bool expectedDayYield = true;                                         // ToDo: Implement weather forcast (solar yield)
   private ProcessStatus status = ProcessStatus.Init;

   public void Process(DateTime currDateTime, OptimizationTarget optimization)
   {
      /* Test arrea */

      // var pvPowerAC = pv.PowerAC;
      // var pvPowerDC = pv.PowerDC;
      // var pvBatteryPower = pv.Battery.Power;

      // Console.WriteLine($"PV AC   -> {pvPowerAC} Watt");
      // Console.WriteLine($"PV DC   -> {pvPowerDC} Watt");
      // Console.WriteLine($"Battery -> {pvBatteryPower} Watt");

      // hm.ActivePowerACLimit = 100;                                                                        
      // pv.Battery.Control(BatteryControlEnum.Remote);
      //var status1 = wb.SetChargeMode(ChargingMode.Stop);
      // wb.SetChargeDurationAndEnergy(60, 8);

      // wb.SetChargeMode(ChargingMode.PVSurPlus);
      // wb.SetChargeMode(ChargingMode.Stop);

      AppliancesControl(currDateTime, optimization);                                      // General (Allwayes check for low/negative energy prices)

      if (Charging == ChargingMode.PVSurPlus)
      {
         EVChargerPVSurPlusCharging(currDateTime, optimization);
      }
      else if (Charging == ChargingMode.Fast)
      {
         EVChargerFastCharging(currDateTime, optimization);
      }
      else if (tb.ToDay.Prices(startsAt: currDateTime).Energy < MyBuyPrice)               // Negative energy prices  (EVCharger is not charging)
      {
         PVPlantNegativePricesManagement(optimization);
      }
      else                                                                                // Positive energy prices (EVCharger is not charging)  
      {
         if (status == ProcessStatus.BatteryChargeTimeShift)                              // HEMS is in battery delay mode  
         {
            if (tb.ToDay.TimeSlot(startsAt: currDateTime) >= tb.ToDay.BatteryTimeShiftChargingToTimeSlot)
            {
               pv.Battery.Control = BatteryControl.Inverter;

               status = ProcessStatus.BatteryChargeFromPV;
            }
            else                                                                          // HEMS is in normal mode () 
            {
               hm.PVPlantActivePowerACLimit(PowerPercentage.Maximum, pv);                 // Cancel zero export mode (Energy prices are positive)
               pv.Battery.Power = (int)Battery.BatteryPower.Blocked;                      // Battery block charging
            }
         }
         else if ((optimization ==OptimizationTarget.Economical || optimization == OptimizationTarget.CostAware) &&
                  expectedDayYield &&
                  tb.ToDay.TimeSlot(startsAt: currDateTime) < tb.ToDay.BatteryTimeShiftChargingToTimeSlot &&
                  pv.PowerDC > PVActivePowerDCMin &&                                       // DC power (String power > 0 Watt)
                  pv.Battery.Power <= 0)                                                   // Battery power = 0 Watt (not discharging)
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

   private void PVPlantNegativePricesManagement(OptimizationTarget optimization)
   {
      hm.PVPlantActivePowerACLimit(PowerPercentage.ZeroExport, pv);                       // First Set zero export mode (BatteryChargeFromPV is also triggert)

      if (status == ProcessStatus.BatteryChargeTimeShift)                                 // HEMS is in battery is charged with time-shifting     
      {
         if (optimization == OptimizationTarget.Economical)
            pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMidium;
         else if (optimization == OptimizationTarget.CostAware)
            pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMin;
         else if (optimization == OptimizationTarget.Balanced)
            pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMin;
         else
            pv.Battery.Control = BatteryControl.Remote;                                  // Don't use the battery 
      }
      else
      {
         if (optimization == OptimizationTarget.Economical)
            pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMax;
         else if (optimization == OptimizationTarget.CostAware)
            pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMidium;
         else if (optimization == OptimizationTarget.Balanced)
            pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMin;
         else
            pv.Battery.Control = BatteryControl.Inverter;                                 // Ecological use the battery to fill the gaps 
      }
   }

   private void EVChargerFastCharging(DateTime currDateTime, OptimizationTarget optimization)
   {
      var energyPrice = tb.ToDay.Prices(startsAt: currDateTime).Energy;

      if (status == ProcessStatus.BatteryChargeTimeShift)                                 // To Do Battary allways => SOC >= 50% 
      {
         if (energyPrice < MyBuyPrice)                                                    // Negative prices get energy (allways) from the GRID 
         {
            hm.PVPlantActivePowerACLimit(PowerPercentage.ZeroExport, pv);                 // Set zero export mode (Energy prices are negative)

            if (optimization == OptimizationTarget.Economical)
               pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMin;              // Set BatteryControll = Remote and charge battery with 1000 watt
            else
               pv.Battery.Power = (int)Battery.BatteryPower.Blocked;

            Logging.Log("WB", $"1. Charging {Charging}", "", $"({energyPrice * 100:00.00} cent/kWh)");
         }
         else                                                                             // Positive prices get energy from the Battery? 
         {
            hm.PVPlantActivePowerACLimit(PowerPercentage.Maximum, pv);                    // Cancel zero export mode (Energy prices are positive)

            if (optimization == OptimizationTarget.Economical)
               pv.Battery.PowerRepeat = (int)Battery.BatteryPower.Blocked;                // Set BatteryControll = Remote and charge battery with 0 watt
            else
               pv.Battery.PowerRepeat = (int)Battery.BatteryPower.Blocked;

            Logging.Log("WB", $"2. Charging {Charging}", "", $"({energyPrice * 100:00.00} cent/kWh)");
         }
      }
      else                                                                                // Battery charging not delayed
      {
         if (tb.ToDay.Prices(startsAt: currDateTime).Energy < MyBuyPrice)                 // Negative prices get energy (allways) from the GRID 
         {
            hm.PVPlantActivePowerACLimit(PowerPercentage.ZeroExport, pv);                 // Set zero export mode (Energy prices are negative)

            if (optimization == OptimizationTarget.Economical)
               pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMax;              // Set BatteryControll = Remote and charge battery with max watt
            else
               pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMin;              // Set BatteryControll = Remote and charge battery with min watt

            Logging.Log("WB", $"3. Charging {Charging}", "", $"({energyPrice * 100:00.00} cent/kWh)");
         }
         else
         {
            hm.PVPlantActivePowerACLimit(PowerPercentage.Maximum, pv);                    // Cancel zero export mode (Energy prices are positive)

            if (optimization == OptimizationTarget.Economical)
               pv.Battery.Control = BatteryControl.Remote;                                // Set BatteryControll = Remote control
            else
               pv.Battery.Control = BatteryControl.Remote;

            Logging.Log("WB", $"4. Charging {Charging}", "", $"({energyPrice * 100:00.00} cent/kWh)");
         }
      }
   }

   private void EVChargerPVSurPlusCharging(DateTime currDateTime, OptimizationTarget optimization)
   {
      var energyPrice = tb.ToDay.Prices(startsAt: currDateTime).Energy;

      if (status == ProcessStatus.BatteryChargeTimeShift)
      {
         if (energyPrice < MyBuyPrice)                                                    // Negative prices get energy (allways) from the GRID 
         {
            hm.PVPlantActivePowerACLimit(PowerPercentage.ZeroExport, pv);                 // Set zero export mode (Energy prices are negative)

            if (optimization == OptimizationTarget.Economical)
               pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMax;              // Set BatteryControl = Remote and charge battery with 2000 watt
            else
               pv.Battery.Control = BatteryControl.Inverter;                              // Set BatteryControl = Inverter and charge the battery with PV   

            Logging.Log("WB", $"5. Charging {Charging}", "", $"({energyPrice * 100:00.00} cent/kWh)");
         }
         else
         {
            hm.PVPlantActivePowerACLimit(PowerPercentage.Maximum, pv);                    // Cancel zero export mode (Energy prices are positive)

            if (optimization == OptimizationTarget.Economical)
               pv.Battery.Control = BatteryControl.Inverter;                              // Set BatteryControl = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control = BatteryControl.Inverter;

            Logging.Log("WB", $"6. Charging {Charging}", "", $"({energyPrice * 100:00.00} cent/kWh)");
         }
      }
      else                                                                                // Battery charging not delayed
      {
         if (energyPrice < MyBuyPrice)                                                    // Negative prices get energy (allways) from the GRID 
         {
            hm.PVPlantActivePowerACLimit(PowerPercentage.ZeroExport, pv);                 // Set zero export mode (Energy prices are negative)

            if (optimization == OptimizationTarget.Economical)
               pv.Battery.PowerRepeat = (int)Battery.BatteryPower.ChargeMax;              // Set BatteryControl = Remote and charge battery with 1000 watt
            else
               pv.Battery.Control = BatteryControl.Inverter;                              // Set BatteryControl = Inverter and charge the battery with PV   

            Logging.Log("WB", $"7. Charging {Charging}", "", $"({energyPrice * 100:00.00} cent/kWh)");
         }
         else
         {
            hm.PVPlantActivePowerACLimit(PowerPercentage.Maximum, pv);                    // Cancel zero export mode (Energy prices are positive)

            if (optimization == OptimizationTarget.Economical)
               pv.Battery.Control = BatteryControl.Inverter;                              // Set BatteryControl = Inverter and charge the battery with PV
            else
               pv.Battery.Control = BatteryControl.Inverter;                              // Set BatteryControl = Inverter and charge the battery with PV

            Logging.Log("WB", $"8. Charging {Charging}", "", $"({energyPrice * 100:00.00} cent/kWh)");
         }
      }
   }

   private void IdleState(DateTime currDateTime)
   {
      if (tb.ToMorrow != null &&
          tb.ToDay.TimeSlot(startsAt: currDateTime) >= tb.ToDay.MaxPriceTimeSlotAfterBatteryTimeShiftChargingTimeSlot &&
          (MySalesPrice > tb.ToMorrow.MinPrice || Math.Abs(tb.ToDay.MaxPrice - tb.ToMorrow.MinPrice) >= MySalesMarge) && // TODO: Remove "tb.ToMorrow.MinPrice < MyBuyPrice (if salderingsregeling expieres)           
          pv.Battery.SOC >= BySellingSOCMin)
      {
         pv.Battery.PowerRepeat = (int)Battery.BatteryPower.DischargeMax;                // Set Battery Control = Remote and Discharge battery with xxx watt

         status = ProcessStatus.BatteryDischargeToGrid;
      }
      else if (status != ProcessStatus.Idle)                                              // HEMS idle mode
      {
         pv.Battery.Control = BatteryControl.Inverter;                                    // Synchronize battery control status becouse ActivePowerACLimit call
         hm.PVPlantActivePowerACLimit(PowerPercentage.Maximum, pv);                       // Cancel Zero export mode (automatically triggert -> BatteryChargeFromPV)   
                                                                                          // automaticaly triggers HM battery control to Inverter
         status = ProcessStatus.Idle;                                                     // Do nothing "until" day changed to next day

         Logging.Log("HEMS", "Idle mode", "", $"({pv.Battery.Control} control)");
      }
   }

   private void AppliancesControl(DateTime currDateTime, OptimizationTarget optimization)
   {
      // EVCharger
      this.Charging = wb.Status == Status.Charging
         ? wb.RotaryKnopPosition == RotaryKnop.Fast ? ChargingMode.Fast : ChargingMode.PVSurPlus
         : ChargingMode.Stop;

      // Season Summer No Home Heating, Only DHW Heating by Heat Pump compressor or Heating-Element
      dhwChargeStatus = tb.ToDay.Prices(startsAt: currDateTime).Energy <= MyBuyPrice ? DHWChargeStatus.start : DHWChargeStatus.stop;   // Negative energy-prices make hot water

      if (dhwChargeStatus != dhwChargeStatusOld)
      {
         if (optimization == OptimizationTarget.Economical)
         {
            hp.DHWCharge(dhwChargeStatus);                                                                                             // Negative energy-prices make hot water (With the Electric heating-rod)

            Logging.Log("HP", "DHW Electic-Element", "", $"{dhwChargeStatus}");
         }
         else
         {
            hp.DHWoperationMode = dhwChargeStatus == DHWChargeStatus.start ? DHWOperationMode.eco : DHWOperationMode.Off;              // Negative energy-prices make hot water (With the HP compressor)

            Logging.Log("HP", "DHW Compressor", "", $"{dhwChargeStatus}");
         }
         
         dhwChargeStatusOld = dhwChargeStatus;
      }
   }
}
