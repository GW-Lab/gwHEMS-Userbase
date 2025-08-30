// Program..: SP2101W.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 30/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Publish  : dotnet publish --runtime linux-arm
// Publish  : dotnet publish --runtime linux-arm64
// Reserved.: Type Class (SP2101W) -> Interface/API to the Top of the Appication Edimax SP-2101W V3 Switch-Plug firmware 4.15

using System.Net;
using System.Text;
using System.Xml.Linq;

namespace gwEdimax;
public class SP2101W(IPAddress ip, string account, string password, int timeOutMiliseconds)
{
   public enum SwitchStatus : int
   {
      Unknow,
      On,
      Off
   }

   public SwitchStatus Status
   {
      get
      {
         using HttpClientHandler handler = new()
         {
            Credentials = new NetworkCredential(account, password)
         };

         using var client = new HttpClient(handler);

         Thread.Sleep(timeOutMiliseconds);                                                               // Wait a while

         try
         {
            var content = new StringContent(GetSWStatusCmd(), Encoding.UTF8, "application/xml");
            var response = client.PostAsync($"http://{ip}:10000/smartplug.cgi", content).Result;         // Send POST request
            response.EnsureSuccessStatusCode();

            var doc = XDocument.Parse(response.Content.ReadAsStringAsync().Result);                      // Parse XML

            return doc.Root?.Element("CMD")?.Value == "ON" ? SwitchStatus.On : SwitchStatus.Off;
         }
         catch
         {
            return SwitchStatus.Unknow;
         }

         static string GetSWStatusCmd() => $$"""
                                                <?xml version="1.0" encoding="UTF8"?>
                                                <SMARTPLUG id="edimax">
                                                   <CMD id="get">
                                                      <Device.System.Power.State></Device.System.Power.State>
                                                   </CMD>
                                                </SMARTPLUG>
                                                """;
      }

      set
      {
         using HttpClientHandler handler = new()
         {
            Credentials = new NetworkCredential(account, password)
         };

         using var client = new HttpClient(handler);

         Thread.Sleep(timeOutMiliseconds);                                                               // Wait a while

         try
         {
            var content = new StringContent(SetSWStatusCmd(value), Encoding.UTF8, "application/xml");
            var response = client.PostAsync($"http://{ip}:10000/smartplug.cgi", content).Result;         // Send POST request
            response.EnsureSuccessStatusCode();

            var doc = XDocument.Parse(response.Content.ReadAsStringAsync().Result);                      // Parse XML
         }
         catch
         {
            // var a = 1;
         }

         static string SetSWStatusCmd(SwitchStatus status) => $$"""
                                                                  <?xml version="1.0" encoding="UTF8"?>
                                                                  <SMARTPLUG id="edimax">
                                                                     <CMD id="setup">
                                                                        <Device.System.Power.State>{{(status == SwitchStatus.On ? "ON" : "OFF")}}</Device.System.Power.State>
                                                                     </CMD>
                                                                  </SMARTPLUG>
                                                                  """;
      }
   }

   public float Power()
   {
      using HttpClientHandler handler = new()
      {
         Credentials = new NetworkCredential(account, password)
      };

      string XMLPowerCmd = """
                              <?xml version="1.0" encoding="UTF8"?>
                               <SMARTPLUG id="edimax">
                                 <CMD id="get">
                                   <NOW_POWER>
                                     <Device.System.Power.NowPower/>
                                   </NOW_POWER>
                                 </CMD>
                              </SMARTPLUG>
                           """;

      using var client = new HttpClient(handler);

      Thread.Sleep(timeOutMiliseconds);                                                               // Wait a while

      try
      {
         var content = new StringContent(XMLPowerCmd, Encoding.UTF8, "application/xml");
         var response = client.PostAsync($"http://{ip}:10000/smartplug.cgi", content).Result;      // Send POST request
         response.EnsureSuccessStatusCode();

         var doc = XDocument.Parse(response.Content.ReadAsStringAsync().Result);                   // Parse XML
         var element = doc.Descendants("NOW_POWER").FirstOrDefault();

         if (element != null)
            return float.Parse(element.Value);
         else
            return 0;
      }
      catch (Exception ex)
      {
         return 0;
      }
   }
}

// <Device.System.Power.NowCurrent/>      // returns blank
// <Device.System.Power.NowVoltage/>      // returns blank  
// <Device.System.Power.NowEnergy/>       // returns blank