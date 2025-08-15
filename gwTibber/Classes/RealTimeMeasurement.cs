// Program..: RealTimeMeasurement.cs
// Author...: G. Wassink
// Design...:
// Date.....: 25/02/2024 Last revised: 28/10/2024
// Notice...: Copyright 2025, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class

using Newtonsoft.Json;

namespace gwTibber.Classes;

public class RealTimeMeasurement
{
	public DateTimeOffset Timestamp;						// { get; set; }			// Timestamp when usage occurred
	public decimal Power;									// { get; set; }			// Consumption at the moment (W)
	public decimal? LastMeterConsumption;				// { get; set; }			// Last meter active import register state (kWh)
	public decimal AccumulatedConsumption;				// { get; set; }			// Energy consumed since the last midnight (kWh)
	public decimal AccumulatedConsumptionLastHour;	// { get; set; }       // Energy consumed since the beginning of the hour (kWh)
	public decimal AccumulatedProduction;				// { get; set; }			// Net energy produced and returned to grid since midnight (kWh)
	public decimal AccumulatedProductionLastHour;	// { get; set; }	      // Net energy produced since the beginning of the hour (kWh)
	public decimal? AccumulatedCost;						// { get; set; }			// Accumulated cost since midnight; requires active Tibber power deal
	public decimal? AccumulatedReward;					// { get; set; }			// Accumulated reward since midnight; requires active Tibber power deal
	public string Currency;									// { get; set; }			// Currency of displayed cost; requires active Tibber power deal
	public decimal MinPower;								// { get; set; }			// Min consumption since midnight (W)
	public decimal AveragePower;							// { get; set; }			// Average consumption since midnight (W)
	public decimal MaxPower;								// { get; set; }			// Peak consumption since midnight (W)
	public decimal? PowerProduction;						// { get; set; }			// Net production (A-) at the moment (Watt)
	public decimal? PowerReactive;						// { get; set; }			// Reactive consumption (Q+) at the moment (kVAr)
	public decimal? PowerProductionReactive;			// { get; set; }			// Net reactive production (Q-) at the moment (kVAr)
	public decimal? MinPowerProduction;					// { get; set; }			// Minimum net production since midnight (W)
	public decimal? MaxPowerProduction;					// { get; set; }			// Maximum net production since midnight (W)
	public decimal? LastMeterProduction;				// { get; set; }			// Last meter active export register state (kWh)
	public decimal? VoltagePhase1;						// { get; set; }			// Voltage (V) on phase 1;
																	// on Kaifa and Aidon meters the value is not part of every HAN data frame therefore the value is null at timestamps with second value other than 0, 10, 20, 30, 40, 50.
																	// There can be other deviations based on concrete meter firmware.
	public decimal? VoltagePhase2;						// { get; set; }			// Voltage (V) on phase 2;
																	//	on Kaifa and Aidon meters the value is not part of every HAN data frame therefore the value is null at timestamps with second value other than 0, 10, 20, 30, 40, 50.
																	//	There can be other deviations based on concrete meter firmware. Value is always null for single phase meters.
	public decimal? VoltagePhase3;						// { get; set; }			// Voltage (V) on phase 3;
																	// on Kaifa and Aidon meters the value is not part of every HAN data frame therefore the value is null at timestamps with second value other than 0, 10, 20, 30, 40, 50.
																	// There can be other deviations based on concrete meter firmware. Value is always null for single phase meters.
	[JsonProperty("CurrentL1")]
	public decimal? CurrentPhase1;						// { get; set; }			// Current (A) on phase 1;
																	// on Kaifa and Aidon meters the value is not part of every HAN data frame therefore the value is null at timestamps with second value other than 0, 10, 20, 30, 40, 50.
																	// There can be other deviations based on concrete meter firmware.
	[JsonProperty("CurrentL2")]
	public decimal? CurrentPhase2;						// { get; set; }			// Current (A) on phase 2;
																	// on Kaifa and Aidon meters the value is not part of every HAN data frame therefore the value is null at timestamps with second value other than 0, 10, 20, 30, 40, 50.
																	// There can be other deviations based on concrete meter firmware. Value is always null for single phase meters.
	[JsonProperty("CurrentL3")]
	public decimal? CurrentPhase3;						// { get; set; }			// Current (A) on phase 3;
																	// on Kaifa and Aidon meters the value is not part of every HAN data frame therefore the value is null at timestamps with second value other than 0, 10, 20, 30, 40, 50.
																	// There can be other deviations based on concrete meter firmware. Value is always null for single phase meters.
	public decimal? PowerFactor;							// { get; set; }			// Power factor (active power / apparent power)
	public int? SignalStrength;							// { get; set; }			// Device signal strength (Pulse - dB; Watty - percent)
}
