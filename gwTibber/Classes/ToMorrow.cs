// Program..: ToMorrow.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 22/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class To Morrow Tibber EPEX Spot market data

namespace gwTibber.Classes;

public class ToMorrow
{
   private static int length = 0;

   public DateTime MinPriceStartsAt;
   public DateTime MaxPriceStartAt;
   public decimal MinPrice = 0;
   public decimal MaxPrice = 0;
   public double SunSet = 0;
   public double SunRise = 0;
   public bool Valide = false;
   private List<EPEX.HourPrice> PricesInfo = [];

   public ToMorrow(EPEX.PriceInfo data, double rise, double set)
   {
      this.SunRise = rise + (DateTime.Now.IsDaylightSavingTime() ? 2 : 1);    // dayLichtSavingTime;
      this.SunSet = set + (DateTime.Now.IsDaylightSavingTime() ? 2 : 1);      // dayLichtSavingTime;

      this.PricesInfo = [.. data.Tomorrow];

      /* +++++++++ Tibber => Remove this code after Tibber has switched to 96 records +++++++++ */
      if (this.PricesInfo.Count is 24 or 23 or 25)                            //	Make 24 hours => Quaterly hours   
      {
         var countOrg = this.PricesInfo.Count;

         for (int i = 0; i <= (countOrg * 4) - 1; i += 4)
            this.PricesInfo.InsertRange(i, this.PricesInfo[i], this.PricesInfo[i], this.PricesInfo[i]);
      }
      /* ++++++++ Tibber => end remove this code +++++++ */

      length = this.PricesInfo.Count;

      if (this.PricesInfo.Count is 96 or 92 or 100)                            //	Summer winter time switch 24 hours?  
      {
         EPEX.HourPrice ToMorrowMin = this.PricesInfo.MinBy(r => r.Energy);
         EPEX.HourPrice ToMorrowMax = this.PricesInfo.MaxBy(r => r.Energy);

         MinPrice = ToMorrowMin.Energy;
         MaxPrice = ToMorrowMax.Energy;
         MinPriceStartsAt = ToMorrowMin.StartsAt;
         MaxPriceStartAt = ToMorrowMax.StartsAt;

         Valide = true;
      }
   }

   public static int TimeSlot(DateTime startsAt)
   {
      var timeSlot = (startsAt.Hour * 60 + startsAt.Minute) / 15;

      return timeSlot >= length ? length : timeSlot;
   }

   public EPEX.HourPrice Prices(int timeSlot)
   {
      return this.PricesInfo[timeSlot];
   }

   public EPEX.HourPrice Prices(DateTime startsAt)
   {
      return this.PricesInfo[TimeSlot(startsAt)];
   }
}
