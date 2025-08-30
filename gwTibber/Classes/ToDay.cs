// Program..: ToDay.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 22/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class ToDay Tibber EPEX Spot market data

namespace gwTibber.Classes;

public class ToDay
{
   private readonly int length = 0;
   private readonly List<EPEX.HourPrice> PricesInfo = [];

   public DateTime MinPriceStartsAt;
   public DateTime MaxPriceStartAt;
   public int BatteryTimeShiftChargingToTimeSlot = 0;
   public int MaxPriceTimeSlotAfterBatteryTimeShiftChargingTimeSlot = 0;

   public decimal MinPrice = 0;
   public decimal MaxPrice = 0;
   public double SunSet = 0;
   public double SunRise = 0;

   public bool Valide = false;

   private enum EPEXPrice
   {
      Minimum,
      Maximum
   }

   public ToDay(EPEX.PriceInfo data, double rise, double set)
   {
      this.SunRise = rise + (DateTime.Now.IsDaylightSavingTime() ? 2 : 1);    // dayLichtSavingTime;
      this.SunSet = set + (DateTime.Now.IsDaylightSavingTime() ? 2 : 1);      // dayLichtSavingTime;

      this.PricesInfo = [.. data.Today];

      /* +++++++++  Tibber Remove this code after Tibber has switched to EPEX Spot Quarterly hour prices (96 records) +++++++++++ */
      if (this.PricesInfo.Count is 24 or 23 or 25)                           //	Make 24 hours => Quaterly hours   
      {
         var countOrg = this.PricesInfo.Count;

         for (int i = 0; i <= (countOrg * 4) - 1; i += 4)
            this.PricesInfo.InsertRange(i, this.PricesInfo[i], this.PricesInfo[i], this.PricesInfo[i]);
      }
      /* +++++++++ Tibber end remove this code +++++++++ */

      length = this.PricesInfo.Count;

      if (this.PricesInfo.Count is 96 or 92 or 100)                           //	Summer winter time switch 24 hours?  
      {
         EPEX.HourPrice ToDayMin = this.PricesInfo.MinBy(r => r.Energy);
         EPEX.HourPrice ToDayMax = this.PricesInfo.MaxBy(r => r.Energy);

         MinPrice = ToDayMin.Energy;
         MaxPrice = ToDayMax.Energy;
         MinPriceStartsAt = ToDayMin.StartsAt;
         MaxPriceStartAt = ToDayMax.StartsAt;

         // Wait until this TimeSlot to charge the Battery
         BatteryTimeShiftChargingToTimeSlot = GetTimeSlotForConsecutiveEPEXPricesInRange(TimeSlot(new TimeOnly(TS(this.SunRise).Hours, TS(this.SunRise).Minutes)),
                                                                                         TimeSlot(new TimeOnly(TS(this.SunSet).Hours, TS(this.SunSet).Minutes)), 
                                                                                         consecutiveEPEXSpotPrices: 4, 
                                                                                         EPEXPrice.Minimum);

         MaxPriceTimeSlotAfterBatteryTimeShiftChargingTimeSlot = GetTimeSlotForConsecutiveEPEXPricesInRange(BatteryTimeShiftChargingToTimeSlot + 1,
                                                                                                            this.PricesInfo.Count,
                                                                                                            consecutiveEPEXSpotPrices: 4,
                                                                                                            price: EPEXPrice.Maximum);
         Valide = true;
      }
   }

   private int GetTimeSlotForConsecutiveEPEXPricesInRange(int startTimeSlot, int endTimeSlot, int consecutiveEPEXSpotPrices, EPEXPrice price = EPEXPrice.Maximum)
   {
      if (PricesInfo.Count < consecutiveEPEXSpotPrices || startTimeSlot < 0 || startTimeSlot > PricesInfo.Count - consecutiveEPEXSpotPrices)
         return startTimeSlot;

      decimal windowSum = StartSum(startTimeSlot, consecutiveEPEXSpotPrices);
      decimal maxSum = windowSum;

      // Slide the window over PricesInfo
      for (int ndx = startTimeSlot + 1; ndx <= PricesInfo.Count - consecutiveEPEXSpotPrices & ndx < endTimeSlot; ndx++)
      {
         windowSum = windowSum - PricesInfo[ndx - 1].Energy + PricesInfo[ndx + consecutiveEPEXSpotPrices - 1].Energy;

         switch (price)
         {
            case EPEXPrice.Maximum:
               if (windowSum > maxSum)
               {
                  maxSum = windowSum;
                  startTimeSlot = ndx;
               }
               break;
            default:
               if (windowSum < maxSum)
               {
                  maxSum = windowSum;
                  startTimeSlot = ndx;
               }
               break;
         }
      }

      return startTimeSlot;
   }

   private decimal StartSum(int startTimeSlot, int windowSize)
   {
      decimal windowSum = 0;

      for (int ndx = 0; ndx < windowSize; ndx++)
         windowSum += PricesInfo[startTimeSlot + ndx].Energy;

      return windowSum;
   }

   public EPEX.HourPrice Prices(int timeSlot)
   {
      return this.PricesInfo[timeSlot];
   }

   public EPEX.HourPrice Prices(DateTime startsAt)
   {
      return this.PricesInfo[TimeSlot(startsAt)];
   }

   private int TimeSlot(TimeOnly startsAt)
   {
      var timeSlot = (startsAt.Hour * 60 + startsAt.Minute) / 15;

      return timeSlot >= length ? length : timeSlot;
   }
   public int TimeSlot(DateTime startsAt)
   {
      var timeSlot = (startsAt.Hour * 60 + startsAt.Minute) / 15;

      return timeSlot >= length ? length : timeSlot;
   }
   private static TimeSpan TS(double value) => TimeSpan.FromHours(value);
}
