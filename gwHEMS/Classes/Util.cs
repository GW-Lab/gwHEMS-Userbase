// Program..: Util.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 22/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class (Util)

using gwEmail;
using gwLogging;
using gwTibber;

namespace gwHEMS.Classes;

[Flags]
public enum ProcessStatus
{
   Init = 0,
   BuyEnergy = 1,
   BatteryChargeTimeShift = 2,
   BatteryChargeFromPV = 4,
   BatteryChargedFromGrid = 8,
   BatteryDischargeToGrid = 16,
   SellEnergy = 32,
   EVCharging = 64,
   Idle = 128,
}

public enum OptimizationTarget
{
   Ecological = 0,    // Fully ecological
   EcoFriendly = 25,  // Mostly ecological, some cost consideration
   Balanced = 50,     // Equal weight to ecology and economy
   CostAware = 75,    // Mostly economical, some ecological consideration
   Economical = 100   // Fully economical
}

public enum Season
{
   Spring,
   Summer,
   Authemn,
   Winter
}

public static class Util
{
   public const string Version = "2.0.1";

   public static void InitDay(DateTime currDateTime, Season season, float outdoorTemp, float indoorTemp, float dhw, int batteryTemp, TibberApiClient tb, OptimizationTarget target, EmailUserData userData)
   {
      Logging.Log("Main-Timer", "Start", $"{currDateTime:dd-MM-yyyy HH:mm:ss}");
      Logging.Log("Current","Temperatures", $"Outdoor {outdoorTemp:00.0}˚C Indoor {indoorTemp:00.0}˚C DHW {dhw:00.0}˚C Battery {batteryTemp:00.0}˚C");
      Logging.Log("Season", $"{season}" , $"Sun-Rise {TS(tb.ToDay.SunRise).Hours:00}:{TS(tb.ToDay.SunRise).Minutes:00} Sun-Set {TS(tb.ToDay.SunSet).Hours:00}:{TS(tb.ToDay.SunSet).Minutes:00}");
      Logging.Log("Tibber", "Today    (min) ", $"{tb.ToDay.MinPriceStartsAt:HH:mm} {tb.ToDay.MinPrice * 100,6:00.00} cent/kWh");
      Logging.Log("Tibber", "Today    (max)", $"{tb.ToDay.MaxPriceStartAt:HH:mm} {tb.ToDay.MaxPrice * 100,6:00.00} cent/kWh");
      Logging.Log("Tibber", "Tomorrow (min) ", $"{tb.ToMorrow.MinPriceStartsAt:HH:mm} {tb.ToMorrow.MinPrice * 100,6:00.00} cent/kWh");
      Logging.Log("Tibber", "Tomorrow (max)", $"{tb.ToMorrow.MaxPriceStartAt:HH:mm} {tb.ToMorrow.MaxPrice * 100,6:00.00} cent/kWh");
      Logging.Log("Target", "Optimization  ", $"{target}");

      Console.WriteLine(">");

      //new EMail(userData).Send($"Report: {DateTime.Now:yyyy-MM-dd}",
      //									"<div style=\"font-size: x-smale\">" +
      //										"<table style=\"font-size: 13px\">" +
      //											  TableRow("Season", $"{season}: Outdoor {outdoorTemp:00.0}˚C Indoor {indoorTemp:00.0}˚C DHW {dhw:00.0}˚C") +
      //											  TableRow("Sun", $"{TS(tibber.ToDay.SunRise).Hours:00}:{TS(tibber.ToDay.SunRise).Minutes:00} Rise , {TS(tibber.ToDay.SunSet).Hours:00}:{TS(tibber.ToDay.SunSet).Minutes:00} Set") +
      //											  TableRow("Tibber", $"Today    low : {tibber.ToDay.MinHour:00}:00h {tibber.ToDay.MinPrice * 100:0.00} cnt/kWh") +
      //											  TableRow("Tibber", $"Today    high: {tibber.ToDay.MaxHour:00}:00h {tibber.ToDay.MaxPrice * 100:0.00} cnt/kWh") +
      //											  TableRow("Tibber", $"Tomorrow low : {tibber.ToMorrow.MinHour:00}:00h {tibber.ToMorrow.MinPrice * 100:0.00} cnt/kWh") +
      //											  TableRow("Tibber", $"Tomorrow high: {tibber.ToMorrow.MaxHour:00}:00h {tibber.ToMorrow.MaxPrice * 100:0.00} cnt/kWh") +
      //											  //		  (myBuyPrice >= tibber.ToDay.MinPrice ? TableRow("Tibber", $"Today     Buy: {tibber.ToDay.MinHour:00};00h") : "") +
      //											  //		  (mySalesMarge <= tibber.ToDay.MaxPrice - tibber.ToMorrow.MinPrice && mySalesPriceMin <= tibber.ToMorrow.MaxPrice ? TableRow("Tibber", $"Today     Sell: {tibber.ToDay.MaxHour:00};00h") : "") +
      //											  TableRow("Battery", $"SOC: {batterySOC}%") +
      //											  TableRow("Battery", $"Charging from PV: {(season == Season.Summer ? tibber.ToDay.BatteryChargeMinHour : tibber.ToDay.SunRise):00}:00") +

      //											  TableRow("", "") +
      //											  TableRow("App", $"Verion: {Version} (Powered by Pi-arm .Net {Environment.Version})") +
      //											"</table>" +
      //										"</div>");
   }

   public static string TableRow(string header, string txt) => $"<tr><td>{header}</td><td>{txt}</td></tr>";

   private static TimeSpan TS(double value) => TimeSpan.FromHours(value);

   public static TimeSpan TS(DateTime currDateTime) => new(currDateTime.Hour, currDateTime.Minute, 0);

   public static Season GetSeason(DateTime dt) => dt.Month is 1 or 2 or 11 or 12 ? Season.Winter : Season.Summer;
}
