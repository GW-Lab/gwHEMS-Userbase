// Program..: Util.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 18/06/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class (Util)

using gwEmail;
using gwTibber;
using gwLogging;
using gwModbus;

namespace GWHEMS.Classes;

[Flags]
public enum ProcessStatus
{
   Init = 0,
   BuyEnergy = 1,
   BatteryChargeDelayed = 2,
   BatteryChargeFromPV = 4,
   BatteryChargedFromGrid = 8,
   BatteryDischargeToGrid = 16,
   SellEnergy = 32,
   EVCharging = 64,
   Idle = 128,
}

public enum OptimizationTargetEnum
{
   None = 0,
   Ecological,
   Economical,
}

public enum Season
{
   //	Spring,
   Summer,
   //	Authemn,
   Winter
}

public static class Util
{
   public const string Version = "1.0.0";

   public static void InitDay(DateTime currDateTime, Season season, float outdoorTemp, float indoorTemp, float dhw, uint batterySOC, int batteryTemp, BatteryStatus batteryStatus , TibberApiClient tibber, OptimizationTargetEnum target, EmailUserData userData)
   {
      Logging.Log("Init", "Start Main-Timer", $"{currDateTime:dd-MM-yyyy HH:mm}");
      Logging.Log("Current","Temperatures", $"Outdoor {outdoorTemp:00.0}˚C Indoor {indoorTemp:00.0}˚C DHW {dhw:00.0}˚C Battery {batteryTemp:00.0}˚C");
      Logging.Log("Season", $"{season}" , $"Sun-Rise {TS(tibber.ToDay.SunRise).Hours:00}:{TS(tibber.ToDay.SunRise).Minutes:00} Sun-Set {TS(tibber.ToDay.SunSet).Hours:00}:{TS(tibber.ToDay.SunSet).Minutes:00}");
      Logging.Log("Tibber", "Today    (low) ", $"{tibber.ToDay.MinPriceHour:00}:00 {tibber.ToDay.MinPrice * 100:00.00} cent/kWh");
      Logging.Log("Tibber", "Today    (high)", $"{tibber.ToDay.MaxPriceHour:00}:00 {tibber.ToDay.MaxPrice * 100:00.00} cent/kWh");
      Logging.Log("Tibber", "Tomorrow (low) ", $"{tibber.ToMorrow.MinPriceHour:00}:00 {tibber.ToMorrow.MinPrice * 100:00.00} cent/kWh");
      Logging.Log("Tibber", "Tomorrow (high)", $"{tibber.ToMorrow.MaxPriceHour:00}:00 {tibber.ToMorrow.MaxPrice * 100:00.00} cent/kWh");
      Logging.Log("Target", "Optimization  ", $"{target}");

      Console.WriteLine(">");
   }

   public static string TableRow(string header, string txt) => $"<tr><td>{header}</td><td>{txt}</td></tr>";

   private static TimeSpan TS(double value) => TimeSpan.FromHours(value);

   public static TimeSpan TS(DateTime currDateTime) => new(currDateTime.Hour, currDateTime.Minute, 0);

   public static Season GetSeason(DateTime dt) => dt.Month is 1 or 2 or 11 or 12 ? Season.Winter : Season.Summer;

   // public static void Log(string col1, string col2, string dtMask ) => Console.WriteLine($"{col1} {col2} {DateTime.Now:dtMask} "); 

   public static int GetQuaterHourIndex(DateTime dt)  // If Tibber changes to QuaterHours then this method can be Implemented 
   {
      return (dt.Hour * 60 + dt.Minute) / 15;
   }
}
