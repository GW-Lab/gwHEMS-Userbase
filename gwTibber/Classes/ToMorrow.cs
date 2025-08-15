// Program..: ToMorrow.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 16/06/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class (ToMorrow)		To Morrow Tibber EPEX spot market data

namespace gwTibber.Classes;

public class ToMorrow																		
{
	public int MinPriceHour = 0;
	public int MaxPriceHour = 0;
	public decimal MinPrice = 0;
	public decimal MaxPrice = 0;
	public double SunSet = 0;
	public double SunRise = 0;
	public bool Valide = false;
	public List<Price> Prices = [];

	public ToMorrow(TibberApiQueryResponse data, double rise, double set)
	{
		this.SunRise = rise + (DateTime.Now.IsDaylightSavingTime() ? 2 :1);	// dayLichtSavingTime;
		this.SunSet = set + (DateTime.Now.IsDaylightSavingTime() ? 2 : 1);   // dayLichtSavingTime;
		
		this.Prices = [.. data.Data.Viewer.Homes.First().CurrentSubscription.PriceInfo.Tomorrow];

		if (this.Prices.Count is 24 or 23 or 25)										// Summer winter time switch 24 hours?
		{
			Price ToMorrowMin = this.Prices.MinBy(r => r.Energy);
			Price ToMorrowMax = this.Prices.MaxBy(r => r.Energy);

			MinPrice = (decimal)ToMorrowMin.Energy;
			MaxPrice = (decimal)ToMorrowMax.Energy;
			MinPriceHour = DateTime.Parse(ToMorrowMin.StartsAt).Hour;
			MaxPriceHour = DateTime.Parse(ToMorrowMax.StartsAt).Hour;
			Valide = true;
		}
	}
}
