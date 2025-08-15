// Program..: ChargerApiClient.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/09/2024 Last revised: 19/06/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Publish  : dotnet publish --runtime linux-arm64
// Reserved.: Type Class (Process)
// SMA Charger 22kw
// Device Id: xxxxxxxxx

using System.Net;
using gwCharger.Classes;
using gwCharger.Classes.JSON;

namespace gwCharger;

// ChargerAppLockEnabled  = "1129"
//	ChargerAppLockDisabled = "1130"
//	ChargerManualLockEnabled  = "5171"
//	ChargerManualLockDisabled = "5172"

public enum ChargingModeEnum
{
	Fast = 4718,
	PVSurPlus = 4719,
	Target = 4720,
	Stop = 4721
}

public enum ChargerSwitchEnum
{
	Fast = 4718,
	PVSurPlus = 4950,
}

public enum ChargerHealthEnum
{
	Warning = 302,
	OK = 307,
	YY= 308
}

public enum ChargerStatusEnum
{
	DisConnected = 200111,			// Not connected
	Connected = 200112,				// Connected and not charing
	Charging = 200113,				// Connected and charing
	Locked								// Charger locked
}

public class ChargerApiClient(IPAddress ip, string account = "", string password = "") : SMAEVCharger22(ip, account, password)
{	
	/* Get measurements*/
	public ChargerHealthEnum  Health => (ChargerHealthEnum)GetMeasurements<List<MeasurmentsJson>>()[16].values[0].value ==ChargerHealthEnum.OK ? ChargerHealthEnum.OK : ChargerHealthEnum.OK;     // Charge Energy kWh:   Measurement.Metering.GridMs.TotWhIn.ChaSta 
	public ChargerSwitchEnum RotarySwitchChargeMode => (ChargerSwitchEnum)GetMeasurements<List<MeasurmentsJson>>()[1].values[0].value == ChargerSwitchEnum.PVSurPlus ? ChargerSwitchEnum.PVSurPlus : ChargerSwitchEnum.Fast;  // Charge Switch:	Measurement.Chrg.ModSw => 4718 = Fast Charging, 4950 = PV-Surplus

	public ChargerStatusEnum ChargerStatus => ChargerStatusEnum.Charging; 
   public double WMaxLimNom => GetMeasurements<List<MeasurmentsJson>>()[21].values[0].value;	// Measurement.Operation.WMaxLimNom 11000 watt
	public double WMaxLimSrc => GetMeasurements<List<MeasurmentsJson>>()[22].values[0].value;	// Measurement.Operation.WMaxLimSrc 11000 watt
	public double WMax => GetMeasurements<List<MeasurmentsJson>>()[22].values[0].value;			// Measurement.Operation.WMaxLimSrc 11000 watt

	/* Get parameters */
	public string Firmware => GetParameters<List<ParametersJson>>()[0].values[21].value;
	public string Model => GetParameters<List<ParametersJson>>()[0].values[25].value;
	public string SerialNr => GetParameters<List<ParametersJson>>()[0].values[35].value;

	/* Set parameters */
	public bool SetInverterACLimit(int value) => SetParameter(value < ChargeLimitMin ? ChargeLimitMin : (value > ChargeLimitMax ? ChargeLimitMax : value), "Parameter.Inverter.AcALim"); //Measurement.Operation.WMaxLimSrc 11000 watt
	public bool SetInverterMax(int value) => SetParameter(value < 0 ? 0 : (value > 11000 ? 11000 : value), "Parameter.Inverter.WMax");
	public bool SetInverterMaxIn(int value) => SetParameter(value < 0 ? 0 : (value > 11000 ? 11000 : value), "Parameter.Inverter.WMaxIn");
	public bool SetChargeMode(ChargingModeEnum value) => SetParameter((int)value, "Parameter.Chrg.ActChaMod");
}


