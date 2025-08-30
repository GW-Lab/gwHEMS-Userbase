// Program..: ChargerApiClient.cs
// Author...: G. Wassink
// Design...:
// Date.....: 16/08/2025 Last revised: 23/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Publish  : dotnet publish --runtime linux-arm64
// Reserved.: Type Class (Process)
// SMA Charger 7.4 22kw
// Device Id: a794e163-b4d8-4322-9fab-70b4b9efe9d4

using gwEVCharger.Classes.JSON;
using gwEVCharger.Classes.SMA;

using System.Net;

namespace gwEVCharger;

// ChargerAppLockEnabled  = "1129"
//	ChargerAppLockDisabled = "1130"
//	ChargerManualLockEnabled  = "5171"
//	ChargerManualLockDisabled = "5172"

public enum ChargingMode
{
   Fast = 4718,
   PVSurPlus = 4719,
   Target = 4720,
   Stop = 4721
}

// Parameter.Chrg.ActChaMod
// Value Meaning(example mapping)

public enum ActChargingMode
{
   StandBy = 4715,               //	Standby / Idle
   PVSurPlus = 4716,             // PV-optimized charging
   Fast = 4717,                  // Fast charging
   Stop = 4718,                  // Stop charging / Charging disabled
   Error = 4719,			         // Error / Fault state
}

public enum RotaryKnop
{
   Fast = 4718,
   PVSurPlus = 4950,
}

public enum Health
{
   Warning = 302,
   OK = 307,
   YY = 308
}

public enum Status
{
   Disconnected = 200111,        // Disconnected
   Connected = 200112,           // Connected and not charing
   Charging = 200113,            // Connected and charing
   Completed = 200114,           // Charging completed
   Error = 200115,               // Charing Error
   Locked								// Charger locked
}

public class ChargerApiClient(IPAddress ip, string account = "", string password = "") : EVCharger(ip, account, password)
{
   /* Get measurements*/
   public Health Health => (Health)GetMeasurements<List<MeasurmentsJson>>()[16].values[0].value == Health.OK ? Health.OK : Health.OK;     // Charge Energy kWh:   Measurement.Metering.GridMs.TotWhIn.ChaSta 
   public RotaryKnop RotaryKnopPosition => (RotaryKnop)GetMeasurements<List<MeasurmentsJson>>()[1].values[0].value == RotaryKnop.PVSurPlus ? RotaryKnop.PVSurPlus : RotaryKnop.Fast;  // Charge Switch:	Measurement.Chrg.ModSw => 4718 = Fast Charging, 4950 = PV-Surplus

  // public static Status ChargerStatus => Status.Charging;
   public double WMaxLimNom => GetMeasurements<List<MeasurmentsJson>>()[21].values[0].value; // Measurement.Operation.WMaxLimNom 11000 watt
   public double WMaxLimSrc => GetMeasurements<List<MeasurmentsJson>>()[22].values[0].value; // Measurement.Operation.WMaxLimSrc 11000 watt
   public double WMax => GetMeasurements<List<MeasurmentsJson>>()[22].values[0].value;       // Measurement.Operation.WMaxLimSrc 11000 watt

   /* Get parameters */
   public string Firmware => GetParameters<List<ParametersJson>>()[0].values[21].value;
   public string Model => GetParameters<List<ParametersJson>>()[0].values[25].value;
   public string SerialNr => GetParameters<List<ParametersJson>>()[0].values[35].value;

   /* Set parameters */
   public bool SetInverterACLimit(int value) => SetParameter(value < ChargeLimitMin ? ChargeLimitMin : value > ChargeLimitMax ? ChargeLimitMax : value, "Parameter.Inverter.AcALim"); //Measurement.Operation.WMaxLimSrc 11000 watt
   public bool SetInverterMax(int value) => SetParameter(value < 0 ? 0 : value > 11000 ? 11000 : value, "Parameter.Inverter.WMax");
   public bool SetInverterMaxIn(int value) => SetParameter(value < 0 ? 0 : value > 11000 ? 11000 : value, "Parameter.Inverter.WMaxIn");
   public bool SetChargeMode(ChargingMode value) => SetParameter((int)value, "Parameter.Chrg.ActChaMod");
   //public int GetActiveParameter(string p) => base.GetActiveParmeter(p);
}

// Maximum utilization of solar energy
//(through automatic phase switching & forecast-based operation)
// • Cost-effective charging
//(through intelligent charging modes: charging PV surplus and using time-variable tariffs)
// • Reduced security risk
//(through blackout protection)
// • Fast charging times
//(through boost function and dynamic adaptation to preset limits)
// • Reduction of charging losses
//(compared to charging at the household socket)
// • Everything from one source
//(all components perfectly matched, modularly expandable)
// • Fast, automated service
//(through integrated Service SMA Smart Connected)
// • Monitoring and control of the entire system via app
// • Reduced additional investment
//(integrated DC residual current sensor and charging cable