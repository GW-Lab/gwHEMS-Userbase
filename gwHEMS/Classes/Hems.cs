// Program..: Process.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 29/11/2024
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

using gwCharger;
using gwEmail;
using gwEnviline;
using gwLogging;
using gwModbus;
using gwTibber;
using System.Net;

namespace GWHEMS.Classes;

public class Hems
{
   /* local constants */
   private const int eventPeriodMinutes = 6;                                                                               // 6 minutes

   /* Local variables */
   private int currDay = 0;

   private OptimizationTargetEnum optimizationTarget = OptimizationTargetEnum.Economical;                                  // Until 01/01/2027 Economical (Salderingsregeling)

   /* Local Objects */
   private readonly EnvilineApiClient hp = new(IPAddress.Parse("x.x.x.x"), "username", "password");                        // Heatpump
   private readonly ModbusApiClient pv = new(IPAddress.Parse("x.x.x.x"));                                                  // Photovoltaic (stp6SE)
   private readonly ModbusApiClient hm = new(IPAddress.Parse("x.x.x.x"));                                                  // Photovoltaic (sunny home manager)
   private readonly ChargerApiClient wb = new(IPAddress.Parse("x.x.x.x"), "username", "password");                         // WallBox (evcharger)
   private readonly TibberApiClient tb = new("Tibber-Key", lat: 0.0, lng: 0.0);                                            // Tibber-Key and your home geo-Location
   private readonly EmailUserData eMailUserData = new("username", "password", "mxrecord", "from", ["to"], [""], true);
   
   // Periods
   private readonly SummerTime summer;
   private readonly WinterTime winter;
   
   // Timers
   private System.Timers.Timer? tmrSync;                                                                                   // Timer to synchoninize with the system clock 
   private Timer? tmrMainProcess;                                                                                          // Event handler (Timer Object)

   public Hems()
   {
      summer = new SummerTime(tb, wb, pv, hp);
      winter = new WinterTime(tb, wb, pv, hp);
      
      StartMainTimer();  
      
      SynchronizeToNextTimeSlot();
   }

   private  void SynchronizeToNextTimeSlot()
   {
      DateTime now = DateTime.Now;
      int minutesPastHour = now.Minute % eventPeriodMinutes;
      int secondsPastMinute = now.Second;

      // Wait until next Time slot
      int minutesToWait = eventPeriodMinutes - minutesPastHour;
      int secondsToWait = 60 - secondsPastMinute;

      TimeSpan timeToNextTimeSlot = new(0, minutesToWait, -secondsPastMinute);

      tmrSync = new System.Timers.Timer(timeToNextTimeSlot.TotalMilliseconds);
     
      tmrSync.Elapsed += (sender, e) =>
      {
         tmrSync.Stop();
         tmrSync.Dispose();
         
         StartMainTimer();
        
         Logging.Log("Main-Timer", "Synchronized on");
      };
     
      tmrSync.Start();
   }

   void StartMainTimer() => this.tmrMainProcess = new Timer(TmrMainProcessTick, null, 10000, (int)TimeSpan.FromMinutes(eventPeriodMinutes).TotalMilliseconds); // Start processing in 6 minutes

   private void TmrMainProcessTick(object? state)
   {
      //	tmrMain?.Change(Timeout.Infinite, Timeout.Infinite);																// For debug purposese only 
      // var r = hp.DHWWaterflow(0);

      //BatteryStatus bstatus = pv.Battery.Status;

      var currDateTime = DateTime.Now;                                                                      // Get current DateTime (Utc->Ignore Summer/Winter time)
      var season = Util.GetSeason(currDateTime);

      try
      {
         if (currDay != currDateTime.Day)                                                                   // Switch day on Midnight
         {
            tb.GetEPEX(currDateTime.Year, currDateTime.Month, currDateTime.Day);
            Util.InitDay(currDateTime,
                         season,
                         hp.OutdoorTemp,
                         hp.IndoorTemp,
                         hp.DHWTemp,
                         pv.Battery.SOC,
                         pv.Battery.Temprature,
                         pv.Battery.Status,
                         tb,
                         optimizationTarget,
                         eMailUserData);

            currDay = currDateTime.Day;
         }
         else if (currDateTime.Hour is 13 or 14)                                                            // Tibber: After One o'çlock midday Tibber publishes EPEX spot market tariffs/prices for ToDay and Tomorrow  (Try for two hours)
         {
            if (!tb.ToMorrow.Valide)                                                                        // ? ToMorrow already Valide
            {
               tb.GetEPEX(currDateTime.Year, currDateTime.Month, currDateTime.Day);

               if (tb.ToMorrow.Valide)                                                                      // Tomorrow EPEX spot market data is invalide so try load spot tariffs/prices again ()
                  Util.InitDay(currDateTime,
                               season,
                               hp.OutdoorTemp,
                               hp.IndoorTemp,
                               hp.DHWTemp,
                               pv.Battery.SOC,
                               pv.Battery.Temprature,
                               pv.Battery.Status,
                               tb,
                               optimizationTarget,
                               eMailUserData);
            }
         }

         if (tb.ToDay.Valide)                                                                               // Day
            if (season == Season.Summer)
               summer.Process(currDateTime, optimizationTarget);
            else
               winter.Process(currDateTime, optimizationTarget);
         else
         {
            pv.Battery.Control(BatteryControlEnum.Remote);

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
