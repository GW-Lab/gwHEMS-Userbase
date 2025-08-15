// Program..: ChargerApiClient.cs
// Author...: G. Wassink
// Design...:
// Date.....: 25/10/2024 Last revised: 19/06/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Publish  : dotnet publish --runtime linux-arm64
// Reserved.: Type Class (Process)

// https://www.photovoltaikforum.com/thread/158333-sma-ev-charger-abfrage/?pageNo=1

using gwCharger.Classes.JSON;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace gwCharger.Classes;

public class SMAEVCharger22
{
   protected const int ChargeLimitMin = 6;                          // Amp
   protected const int ChargeLimitMax = 16;                         // Amp

   protected const int PowerLimitMin = 6;                           // Watt
   protected const int PowerLimitMax = 16;                          // Watt

   private readonly IPAddress ip;
   private readonly int eventPeriod = 59;                           // RefreshToken every 59 Minutes

   private string accessToken = "";
   private string refreshToken = "";
   
   private static readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true, IncludeFields = true };

   private Timer? tmrRefreshToken = null;

   private void Start() => tmrRefreshToken = new Timer(TmrRefreshTokenTick, null, 10000, (int)TimeSpan.FromMinutes(eventPeriod).TotalMilliseconds);  // Start processing in 6 minutes

   public SMAEVCharger22(IPAddress ip, string account, string password)
   {
      this.ip = ip;

      var login = Login<TokenJson>(account, password);            // OK
      this.accessToken = login.Access_token;
      this.refreshToken = login.Refresh_token;

      Start();                                                    // Start RefreshToken process	
   }

   private void TmrRefreshTokenTick(object? state)
   {
      var result = RefreshToken<TokenJson>();                     // OK
      this.accessToken = result.Access_token;
      this.refreshToken = result.Refresh_token;
   }

   private T Login<T>(string account, string password) where T : new()
   {
      try
      {
         using var client = new HttpClient();
         var tokenContent = new StringContent($"grant_type=password&username={account}&password={password}", Encoding.UTF8, "application/x-www-form-urlencoded");
         using var tokenResponse = client.PostAsync($"http://{ip}/api/v1/token", tokenContent).Result;

         if (tokenResponse.IsSuccessStatusCode)
         {
            var result = JsonSerializer.Deserialize<T>(tokenResponse.Content.ReadAsStringAsync().Result, jsonOptions);

            return result ?? new T();                                                   // Get Access Token from JSON
         }
         else
         {
            return new T();
         }
      }
      catch // (HttpIOException ex)
      {
         return new T();      // var m = ex.Message;
      }
   }

   private T RefreshToken<T>() where T : new()
   {
      try
      {
         using var client = new HttpClient();
         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.accessToken);
         var tokenContent = new StringContent($"grant_type=refresh_token&refresh_token={this.refreshToken}", Encoding.UTF8, "application/x-www-form-urlencoded");

         using var tokenResponse = client.PostAsync($"http://{ip}/api/v1/token", tokenContent).Result;

         if (tokenResponse.IsSuccessStatusCode)
         {
            var result = JsonSerializer.Deserialize<T>(tokenResponse.Content.ReadAsStringAsync().Result, jsonOptions);

            return result ?? new T();                                                   // Get Access Token from JSON
         }
         else
         {
            return new T();
         }
      }
      catch // (HttpIOException ex)
      {
         return new T();      // var m = ex.Message;
      }
   }

   protected T GetMeasurements<T>() where T : class, new()
   {
      try
      {
         using var client = new HttpClient();
         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.accessToken);

         var dataContent = new StringContent("""[{"componentId":"IGULD:SELF"}]""", Encoding.UTF8, "application/x-www-form-urlencoded");
         var dataResponse = client.PostAsync($"http://{ip}/api/v1/measurements/live/", dataContent).Result;

         if (dataResponse.IsSuccessStatusCode)
         {
            // var json =dataResponse.Content.ReadAsStringAsync().Result;
            var result = JsonSerializer.Deserialize<T>(dataResponse.Content.ReadAsStringAsync().Result, jsonOptions);

            return result ?? new T();
         }

         return new T();
      }
      catch // (HttpIOException ex)
      {
         return new T();      // var m = ex.Message;
      }
   }

   protected T GetParameters<T>() where T : class, new()
   {
      try
      {
         using var client = new HttpClient();
         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.accessToken);
         client.DefaultRequestHeaders.Add("Referer", $"http://{ip}/webui/Plant:1,IGULD:SELF/configuration/view-parameters");

         var dataContent = new StringContent("""{"queryItems":[{"componentId":"IGULD:SELF"}]}""", Encoding.UTF8, "application/json");
         var dataResponse = client.PostAsync($"http://{ip}/api/v1/parameters/search", dataContent).Result;

         if (dataResponse.IsSuccessStatusCode)
         {
            var result = JsonSerializer.Deserialize<T>(dataResponse.Content.ReadAsStringAsync().Result, jsonOptions);

            return result ?? new T();
         }
         ;

         return new T();
      }
      catch // (HttpIOException ex)
      {
         return new T();
      }
   }

   protected bool SetParameter(int value, string parameter)
   {
      try
      {
         using var client = new HttpClient();
         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.accessToken);

         using var request = new HttpRequestMessage(HttpMethod.Put, $"http://{ip}/api/v1/parameters/IGULD:SELF");
         request.Headers.Add("Referer", $"http://{ip}/webui/Plant:1,IGULD:SELF/configuration/view-parameters");
         request.Content = new StringContent($$"""{"values":[{"channelId":"{{parameter}}","value":{{value}}}]}""");
         request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

         return client.SendAsync(request).Result.IsSuccessStatusCode;
      }
      catch // (HttpIOException ex)
      {
         return false;
      }
   }

   public bool SetWifiSignalPower(int power = 0)
   {
      try
      {
         power = (power < PowerLimitMin ? PowerLimitMin : (power > PowerLimitMax ? PowerLimitMax : power));

         using var client = new HttpClient();
         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.accessToken);

         using var request = new HttpRequestMessage(HttpMethod.Put, $"http://{ip}/api/v1/parameters/IGULD:SELF");
         request.Headers.Add("Referer", $"http://{ip}/webui/Plant:1,IGULD:SELF/configuration/view-parameters");
         request.Content = new StringContent($$"""{"values":[{"channelId":"Measurement.Wl.SigPwr","value":{{(int)ChargingModeEnum.Target}} ,"value":{{power}}}]}""");
         request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

         return client.SendAsync(request).Result.IsSuccessStatusCode;
      }
      catch // (HttpIOException ex)
      {
         return false; //var m = ex.Message;
      }
   }

   public bool SetChargeDurationAndEnergy(int minutes = 0, int energy = 0)
   {
      try
      {
         var timeStamp = DateTime.Now;

         using var client = new HttpClient();
         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.accessToken);

         using var request = new HttpRequestMessage(HttpMethod.Put, $"http://{ip}/api/v1/parameters/IGULD:SELF");
         request.Headers.Add("Referer", $"http://{ip}/webui/Plant:1,IGULD:SELF/configuration/view-parameters");
         request.Content = new StringContent($$"""{"values":[{"channelId":"Parameter.Chrg.Plan.DurTmm","timestamp":"{{timeStamp:yyyy-MM-ddTHH:mm:ss.000Z}}","value":{{minutes}}},{"channelId":"Parameter.Chrg.Plan.En","timestamp":"{{timeStamp:yyyy-MM-ddTHH:mm:ss.000Z}}","value":{{energy}}}]}""");
         request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

         return client.SendAsync(request).Result.IsSuccessStatusCode;
      }
      catch // (HttpIOException ex)
      {
         return false; //var m = ex.Message;
      }
   }

   public bool Reboot()
   {
      try
      {
         using var client = new HttpClient();
         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.accessToken);

         using var request = new HttpRequestMessage(HttpMethod.Put, $"http://{ip}/api/v1/parameters/IGULD:SELF");
         request.Headers.Add("Referer", $"http://{ip}/webui/Plant:1,IGULD:SELF/configuration/view-parameters");
         request.Content = new StringContent("""{"values":[{"channelId":"Parameter.Sys.DevRstr","value":1146}]}""");
         request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

         return client.SendAsync(request).Result.IsSuccessStatusCode;
      }
      catch // (HttpIOException ex)
      {
         return false; //var m = ex.Message;
      }
   }

   public double ChargePower => GetMeasurements<List<MeasurmentsJson>>()[14].values[0].value;               // Charging Power Watt:	Measurement.Metering.GridMs.TotWIn.ChaSta 
                                                                                                            // public double EnergyTotal => charger.GetData<List<ChargerJSON>>()[16].values[0].value / 1000;		// Charge Energy kWh:   Measurement.Metering.GridMs.TotWhIn.ChaSta 
   public ChargerStatusEnum Status                                                                          // Charging Status:		Measurement.Operation.EVeh.ChaStt 200111 = disconnected  200112 = connected 200113 = charging
   {
      get
      {
         if ((ChargerStatusEnum)GetMeasurements<List<MeasurmentsJson>>()[17].values[0].value == ChargerStatusEnum.DisConnected)
            return ChargerStatusEnum.DisConnected;
         else if ((ChargerStatusEnum)GetMeasurements<List<MeasurmentsJson>>()[17].values[0].value == ChargerStatusEnum.Connected)
            return ChargerStatusEnum.Connected;
         else
            return ChargerStatusEnum.Charging;
      }
   }

   // public double EnergyTotalNew => charger.GetData<List<ChargerJSON>>()[15].values[0].value / 1000;					// ???? Charge Energy kWh:   Measurement.Metering.GridMs.TotWhIn.ChaSta 
   public double EnergyTotal => GetMeasurements<List<MeasurmentsJson>>()[16].values[0].value / 1000;                 // Charge Energy kWh:   Measurement.Metering.GridMs.TotWhIn.ChaSta 

   //	public double PowerNew => charger.GetData<List<ChargerJSON>>()[13].values[0].value;										// ???? Charge Power Watt:	Measurement.Metering.GridMs.TotWIn.ChaSta 
   public double GridPower => GetMeasurements<List<MeasurmentsJson>>()[14].values[0].value;                          // Charge Power Watt:	Measurement.Metering.GridMs.TotWIn.ChaSta 
}