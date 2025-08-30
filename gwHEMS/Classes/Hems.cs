// Program..: Process.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 30/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Publish  : dotnet publish --runtime linux-arm
// Publish  : dotnet publish --runtime linux-arm64
// Publish  : dotnet publish -r linux-arm -c Release --self-contained true
// Publish  : dotnet publish -r linux-arm64 -c Release --self-contained true
// Publish  : dotnet publish gwHEMS -c:Release -r:win-x64 --self-contained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output d:\gwhems
// Publish  : dotnet publish gwHEMS -c:Release -r:linux-arm --self-contained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output d:\gwhems
// Publish  : dotnet publish gwHEMS -c:Release -r:linux-arm --self-contained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output f:\publish-linux-arm
// Publish  : dotnet publish gwHEMS -c:Release -r:linux-arm64 --self-contained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output d:\gwhems
// Publish  : dotnet publish gwHEMS -c:Release -r:linux-arm64 --self-contained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output f:\publish-linux-arm64
// Publish  : dotnet publish gwHEMS -c:Release -r:linux-arm64 --self-contained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=True -p:TrimMode=link --output d:\gwhems // ILink warnings? 
// Reserved.: Type Class (Process)

using gwEdimax;
using gwEmail;
using gwEnviline;
using gwEVCharger;
using gwLogging;
using gwModbus;
using gwTibber;
using System.Net;

namespace gwHEMS.Classes;

public class Hems
{
   /* local constants */
   private const int MainEventIntervalInMinutes = 5;

   /* Local variables */
   private int currDay = 0;

   private OptimizationTarget optimization = OptimizationTarget.Balanced;                                                        // Until 01/01/2027 Economical (Salderingsregeling)

   /* Local Objects */
   private readonly EmailUserData eMailUserData = new("report@xx.xx",
                                                      "password",
                                                      "mxRecord",
                                                      "from",
                                                      ["to"], [""], true);

   private readonly EnvilineApiClient hp = new(IPAddress.Parse("0.0.0.0"), "unit-password", "gateway-password");                 // Heatpump
   private readonly PVInverterApiClient pv = new(IPAddress.Parse("0.0.0.0"));                                                    // Photovoltaic (STP6SE)
   private readonly HomeManagerApiClient hm = new(IPAddress.Parse("0.0.0.0"));                                                   // Photovoltaic (Sunny Home Manager)
   private readonly ChargerApiClient wb = new(IPAddress.Parse("0.0.0.0"), "username", "password");                               // WallBox (evcharger)
   private readonly TibberApiClient tb = new("accesskey",
                                             lat: 0.0,
                                             lng: 0.0);                                                                          // geo-Location
   private readonly EdimaxApiClient sw = new(IPAddress.Parse("0.0.0.0"), "username", "password");                                // Edimax Plug-Switch

   /* Season/Periods */
   private readonly SummerTime summer;
   private readonly WinterTime winter;

   /* Timers */
   private System.Timers.Timer? tmrSync;                                                                                         // Timer to synchoninize with the system clock 
   private Timer? tmrMainProcess;                                                                                                // Event handler (Timer Object)

   public Hems()
   {
      summer = new SummerTime(tb, wb, hm, pv, hp);
      winter = new WinterTime(tb, wb, hm, pv, hp);

      StartMainTimer();
      SynchronizeToEPEXTimeSlot();
   }

   private void SynchronizeToEPEXTimeSlot()
   {
      DateTime now = DateTime.Now;
      int minutesPastHour = now.Minute % MainEventIntervalInMinutes;
      int secondsPastMinute = now.Second;

      // Wait until next Time slot
      int minutesToWait = MainEventIntervalInMinutes - minutesPastHour;
      TimeSpan timeToNextTimeSlot = new(0, minutesToWait, -secondsPastMinute);
      tmrSync = new System.Timers.Timer(timeToNextTimeSlot.Add(timeToNextTimeSlot.Minutes == 0 ? new TimeSpan(0, 5, 0) : new TimeSpan(0, 0, 0)).TotalMilliseconds);

      tmrSync.Elapsed += (sender, e) =>
      {
         tmrSync.Dispose();
         tmrSync.Stop();

         StartMainTimer();

         Logging.Log("Main-Timer", "Synchronized", "", "EPEX Spot");
      };

      tmrMainProcess?.Dispose();
      tmrSync.Start();
   }

   void StartMainTimer() => this.tmrMainProcess = new Timer(TmrMainProcessTick,
                                                            null,
                                                            0,
                                                            (int)TimeSpan.FromMinutes(MainEventIntervalInMinutes).TotalMilliseconds); // Start processing in 6 minutes
   private void TmrMainProcessTick(object? state)
   {
      var currDateTime = DateTime.Now;                                                                      // Get current DateTime (Utc->Ignore Summer/Winter time)
      var season = Util.GetSeason(currDateTime);

      optimization = season == Season.Summer ? OptimizationTarget.Economical : OptimizationTarget.Balanced; // Until 01/01/2027 Economical (Salderingsregeling)winter.Process(currDateTime, optimization);

      try
      {
         if (currDay != currDateTime.Day)                                                                   // Switch day on Midnight
         {
            tb.EPEX(currDateTime.Year, currDateTime.Month, currDateTime.Day);
            Util.InitDay(currDateTime,
                         season,
                         hp.OutdoorTemp,
                         hp.IndoorTemp,
                         hp.DHWTemp,
                         pv.Battery.Temprature,
                         tb,
                         optimization,
                         eMailUserData);

            currDay = currDateTime.Day;
         }
         else if (currDateTime.Hour is 13 or 14)                                                            // Tibber: After One o'çlock midday Tibber publishes EPEX spot market tariffs/prices for ToDay and Tomorrow  (Try for two hours)
         {
            if (!tb.ToMorrow.Valide)                                                                        // ? ToMorrow already Valide
            {
               tb.EPEX(currDateTime.Year, currDateTime.Month, currDateTime.Day);

               if (tb.ToMorrow.Valide)                                                                      // Tomorrow EPEX spot market data is invalide so try load spot tariffs/prices again ()
                  Util.InitDay(currDateTime,
                               season,
                               hp.OutdoorTemp,
                               hp.IndoorTemp,
                               hp.DHWTemp,
                               pv.Battery.Temprature,
                               tb,
                               optimization,
                               eMailUserData);
            }
         }

         if (tb.ToDay.Valide)                                                                               // Day
            if (season == Season.Summer)
               summer.Process(currDateTime, optimization);
            else
               winter.Process(currDateTime, optimization);
         else
         {
            pv.Battery.Control = BatteryControl.Remote;

            Logging.Log("Tibber", "Error", $"{DateTime.Now:dd-MM-yyyy HH:mm}");
         }
      }
      catch (Exception ex)
      {
         Logging.Log("General", "Error", "", $"{ex.Message}");
      }
   }
   //	tmrMain?.Change(0, (int)TimeSpan.FromSeconds(eventPeriod).TotalMilliseconds);									//  
}

