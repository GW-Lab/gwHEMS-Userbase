// Program..: EPEX.cs
// Author...: G. Wassink
// Design...:
// Date.....: 25/02/2024 Last revised: 20/08/2025
// Notice...: Copyright 2025, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class(EPEX)

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace gwTibber.Classes;

public class EPEX(string accessToken) : IDisposable
{
   //  private record GraphQlRequest(string query);
   private record GraphQlError(string Message);
   private record GraphQlResponse<T>(T Data, GraphQlError[] Errors);
   private record ResponseData(Viewer Viewer);
   private record Viewer(Home[] Homes);
   private record Home(Subscription CurrentSubscription);
   private record Subscription(PriceInfo PriceInfo);
   public record PriceInfo(HourPrice[] Today, HourPrice[] Tomorrow);
   public record HourPrice(DateTime StartsAt, decimal Total, decimal Energy, decimal Tax);

   private readonly ProductInfoHeaderValue userAgent = new("Gijs-home-automation-system", "1.0");
   private readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true, IncludeFields = true };
   //  private const string query = "{viewer{homes{currentSubscription{priceInfo{current{total energy tax startsAt}today{total energy tax startsAt}tomorrow{total energy tax startsAt}}}}}}";
   private const string jsonQuery = """{"query":"{viewer{homes{currentSubscription{priceInfo{current{total energy tax startsAt}today{total energy tax startsAt}tomorrow{total energy tax startsAt}}}}}}"}""";

   private const string endpoint = "https://api.tibber.com/v1-beta/gql";

   private bool disposedValue;

   public PriceInfo GetEPEX()
   {
      using var http = new HttpClient();
      http.DefaultRequestHeaders.Accept.Clear();
      http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
      http.DefaultRequestHeaders.UserAgent.Add(userAgent);

      // var request = new GraphQlRequest(query);
      // var json = JsonSerializer.Serialize(request);
      using var content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");

      using var response = http.PostAsync(endpoint, content).Result;
      var payload = response.Content.ReadAsStringAsync().Result;

      return response.IsSuccessStatusCode ? JsonSerializer.Deserialize<GraphQlResponse<ResponseData>>(payload, options).Data.Viewer.Homes[0].CurrentSubscription.PriceInfo : new PriceInfo([], []);
   }

   protected virtual void Dispose(bool disposing)
   {
      if (!disposedValue)
      {
         if (disposing)
         {
            // TODO: dispose managed state (managed objects)
         }

         // TODO: free unmanaged resources (unmanaged objects) and override finalizer
         // TODO: set large fields to null
         disposedValue = true;
      }
   }

   // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
   // ~Tibber()
   // {
   //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
   //     Dispose(disposing: false);
   // }

   void IDisposable.Dispose()
   {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
   }


}
