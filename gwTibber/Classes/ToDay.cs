// Program..: ToDay.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 11/06/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class (ToDay)								To Day Tibber EPEX spot market data

namespace gwTibber.Classes;

public class ToDay
{
   public int MinPriceHour = 0;
   public int MaxPriceHour = 0;
   public int BatteryChargeDelayedToHour = 0;
   public int EnergyMaxHour = 0;

   public decimal MinPrice = 0;
   public decimal MaxPrice = 0;
   public double SunSet = 0;
   public double SunRise = 0;
   public bool Valide = false;
   public List<Price> Prices = [];

   public ToDay(TibberApiQueryResponse data, double rise, double set)
   {
      this.SunRise = rise + (DateTime.Now.IsDaylightSavingTime() ? 2 : 1);    // dayLichtSavingTime;
      this.SunSet = set + (DateTime.Now.IsDaylightSavingTime() ? 2 : 1);      // dayLichtSavingTime;

      this.Prices = [.. data.Data.Viewer.Homes.First().CurrentSubscription.PriceInfo.Today];

      if (this.Prices.Count is 24 or 23 or 25)                                //	Summer winter time switch 24 hours?  
      {
         Price ToDayMin = this.Prices.MinBy(r => r.Energy);
         Price ToDayMax = this.Prices.MaxBy(r => r.Energy);

         MinPrice = (decimal)ToDayMin.Energy;                                 // convert Euro -> cent
         MaxPrice = (decimal)ToDayMax.Energy;                                 // convert Euro -> cent
         MinPriceHour = DateTime.Parse(ToDayMin.StartsAt).Hour;
         MaxPriceHour = DateTime.Parse(ToDayMax.StartsAt).Hour;
     
         // Time to Buy energy: BatteryChargeMinHour must be in daylight time -> So there is posible solar power
         BatteryChargeDelayedToHour = DateTime.Parse(this.Prices.Where(r => DateTime.Parse(r.StartsAt).Hour >= this.SunRise &&
                                      DateTime.Parse(r.StartsAt).Hour <= this.SunSet).MinBy(r => r.Energy).StartsAt).Hour;   

         // Time to sell energy: BatteryDischargeMinHour must be the max-energy price after BatteryChargeDelayedToHour
         EnergyMaxHour = DateTime.Parse(this.Prices.Where(r => DateTime.Parse(r.StartsAt).Hour > BatteryChargeDelayedToHour).MaxBy(r => r.Energy).StartsAt).Hour;

         Valide = true;
      }
   }
}
