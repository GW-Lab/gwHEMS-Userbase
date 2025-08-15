// Program..: Tibber.cs
// Author...: G. Wassink
// Design...:
// Date.....: 25/02/2024 Last revised: 03/04/2024
// Notice...: Copyright 2025, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class(Tibber)

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace gwTibber.Classes;

public partial class Tibber : IDisposable
{
	public static HttpHeaderValueCollection<ProductInfoHeaderValue> UserAgent { get; private set; }

	private static readonly ProductInfoHeaderValue TibberSdkUserAgent = new("gwTibber-SDK.NET", "1.0");
	private static readonly SemaphoreSlim Semaphore = new(1);
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(59);

	internal static readonly JsonSerializerSettings JsonSerializerSettings =
		  new()
		  {
			  ContractResolver = new CamelCasePropertyNamesContractResolver(),
			  Converters = { new StringEnumConverter() },
			  DateParseHandling = DateParseHandling.DateTimeOffset
		  };

	private static readonly JsonSerializer Serializer = JsonSerializer.Create(JsonSerializerSettings);
	private RealTimeMeasurementListener _realTimeMeasurementListener;
	private readonly HttpClient _httpClient;
	private readonly string _accessToken;
	private readonly string _baseUrl;

	public Tibber(string accessToken, ProductInfoHeaderValue userAgent, HttpMessageHandler messageHandler = null, TimeSpan? timeout = null, string baseUrl = null)
	{
		_accessToken = accessToken;
		_baseUrl = baseUrl ?? "https://api.tibber.com/v1-beta/gql";

		messageHandler ??= new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };

		_httpClient = new HttpClient(messageHandler)
								{
									BaseAddress = new Uri(_baseUrl),
									Timeout = timeout ?? DefaultTimeout,
									DefaultRequestHeaders =
											 {
													Authorization = new AuthenticationHeaderValue("Bearer", _accessToken),
													AcceptEncoding = { new StringWithQualityHeaderValue("gzip") }
											 }
								};

		UserAgent = _httpClient.DefaultRequestHeaders.UserAgent;
		UserAgent.Add(userAgent);
		UserAgent.Add(TibberSdkUserAgent);
	}

	public void Dispose()
	{
		_realTimeMeasurementListener?.Dispose();
		_httpClient.Dispose();
	}

	//public async Task<TibberApiQueryResponse> GetBasicDataAsync(CancellationToken cancellationToken = default)
	//{
	//	var result = await QueryAsync(new TibberQueryBuilder().WithHomesAndSubscriptions().Build(), cancellationToken);
	//	ValidateResult(result);

	//	return result;
	//}

	public async Task<TibberApiQueryResponse> GetHomes(CancellationToken cancellationToken = default)
	{
		var result = await QueryAsync(new TibberQueryBuilder().WithHomes().Build(), cancellationToken);
		ValidateResult(result);

		return result;
	}

	//public async Task<TibberApiQueryResponse> GetHomeById(Guid homeId, CancellationToken cancellationToken = default)
	//{
	//	var result = await QueryAsync(new TibberQueryBuilder().WithHomeById(homeId).Build(), cancellationToken);
	//	ValidateResult(result);

	//	return result;
	//}

	//public async Task<ICollection<ConsumptionEntry>> GetHomeConsumption(Guid homeId, EnergyResolution resolution, int? lastEntries = null, CancellationToken cancellationToken = default)
	//{
	//	var result = await QueryAsync(new TibberQueryBuilder().WithHomeConsumption(homeId, resolution, lastEntries).Build(), cancellationToken);
	//	ValidateResult(result);

	//	return result.Data?.Viewer?.Home?.Consumption?.Nodes;
	//}

	//public async Task<ICollection<ProductionEntry>> GetHomeProduction(Guid homeId, EnergyResolution resolution, int? lastEntries = null, CancellationToken cancellationToken = default)
	//{
	//	var result = await QueryAsync(new TibberQueryBuilder().WithHomeProduction(homeId, resolution, lastEntries).Build(), cancellationToken);
	//	ValidateResult(result);

	//	return result.Data?.Viewer?.Home?.Production?.Nodes;
	//}

	public Task<TibberApiQueryResponse> Query(string query, CancellationToken cancellationToken = default) => Request<TibberApiQueryResponse>(query, cancellationToken);

	public Task<TibberApiQueryResponse> QueryAsync(string query, CancellationToken cancellationToken = default) => Request<TibberApiQueryResponse>(query, cancellationToken);

	//public Task<TibberApiMutationResponse> Mutation(string mutation, CancellationToken cancellationToken = default) => Request<TibberApiMutationResponse>(mutation, cancellationToken);


	public async Task<TibberApiQueryResponse> ValidateRealtimeDevice(CancellationToken cancellationToken = default)
	{
		var homes = await GetHomes(cancellationToken);

		if (!(homes?.Data?.Viewer?.Homes?.Any() ?? false))
			throw new ApplicationException("No homes found");

		if (!(homes.Data?.Viewer?.Homes?.Any(h => h.Features?.RealTimeConsumptionEnabled ?? false) ?? false))
			throw new ApplicationException("No homes with real time consumption devices found");

		var websocketSubscriptionUrl = homes.Data?.Viewer?.WebsocketSubscriptionUrl;

		return websocketSubscriptionUrl is null ? throw new ApplicationException("Unable to retrieve web socket subscription url") : homes;
	}

	//public async Task<IObservable<RealTimeMeasurement>> StartRealTimeMeasurementListener(Guid homeId, CancellationToken cancellationToken = default)
	//{
	//	var homes = await ValidateRealtimeDevice(cancellationToken);
	//	var websocketSubscriptionUrl = homes.Data.Viewer.WebsocketSubscriptionUrl;

	//	await Semaphore.WaitAsync(cancellationToken);

	//	_realTimeMeasurementListener ??=
	//		  new RealTimeMeasurementListener(this, new Uri(websocketSubscriptionUrl), _accessToken);

	//	try
	//	{
	//		return await _realTimeMeasurementListener.SubscribeHome(homeId, cancellationToken);
	//	}
	//	finally
	//	{
	//		Semaphore.Release();
	//	}
	//}

	//public async Task StopRealTimeMeasurementListener(Guid homeId)
	//{
	//	await Semaphore.WaitAsync();

	//	try
	//	{
	//		await _realTimeMeasurementListener.UnsubscribeHome(homeId, CancellationToken.None);
	//	}
	//	finally
	//	{
	//		Semaphore.Release();
	//	}
	//}

	private async Task<TResult> Request<TResult>(string query, CancellationToken cancellationToken)
	{
		var requestStart = DateTimeOffset.UtcNow;

		HttpResponseMessage response;

		try
		{
			response = await _httpClient.PostAsync(string.Empty, JsonContent(new { query }), cancellationToken);
		}
		catch (Exception exception)
		{
			throw new ApiHttpException(_httpClient.BaseAddress, HttpMethod.Post, DateTimeOffset.Now - requestStart, exception.Message, exception);
		}

		if (!response.IsSuccessStatusCode)
			throw await ApiHttpException.Create(new Uri(_baseUrl), HttpMethod.Post, response, DateTimeOffset.Now - requestStart).ConfigureAwait(false);

		using var stream = await response.Content.ReadAsStreamAsync();
		using var streamReader = new StreamReader(stream);
		using var jsonReader = new JsonTextReader(streamReader);

		return Serializer.Deserialize<TResult>(jsonReader);
	}

	private static HttpContent JsonContent(object data) =>
		  new StringContent(JsonConvert.SerializeObject(data, JsonSerializerSettings), Encoding.UTF8, "application/json");

	private static void ValidateResult(TibberApiQueryResponse response)
	{
		if (response.Errors is not null && response.Errors.Count != 0)
			throw new ApiException($"Query execution failed:{Environment.NewLine}{string.Join(Environment.NewLine, response.Errors.Select(e => $"{e.Message} (locations: {string.Join(";", e.Locations.Select(l => $"line: {l.Line}, column: {l.Column}"))})"))}");
	}
}

public class TibberApiQueryResponse : GraphQlResponse<QueryData>
{ }

//public class TibberApiMutationResponse : GraphQlResponse<TibberMutation>
//{ }

public class QueryData
{
	public Viewer Viewer { get; set; }
}

//public class QueryError
//{
//	public string Message { get; set; }
//	public ICollection<ErrorLocation> Locations { get; set; }
//}

//public class ErrorLocation
//{
//	public int? Line { get; set; }
//	public int? Column { get; set; }
//}
