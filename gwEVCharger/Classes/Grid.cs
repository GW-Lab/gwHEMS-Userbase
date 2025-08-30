// Program..: Grid.cs
// Author...: G. Wassink
// Design...:
// Date.....: 25/10/2024 Last revised: 03/11/2024
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Publish  : dotnet publish --runtime linux-arm64
// Reserved.: Type Class (Grid)

using gwEVCharger.Classes.SMA;

namespace gwEVCharger.Classes;

public class Grid(EVCharger charger)
{
//	// public double EnergyTotalNew => charger.GetData<List<ChargerJSON>>()[15].values[0].value / 1000;               // ???? Charge Energy kWh:   Measurement.Metering.GridMs.TotWhIn.ChaSta 
//	public double EnergyTotal => charger.GetMeasurements<List<MeasurmentsJson>>()[16].values[0].value / 1000;			// Charge Energy kWh:   Measurement.Metering.GridMs.TotWhIn.ChaSta 

////	public double PowerNew => charger.GetData<List<ChargerJSON>>()[13].values[0].value;								      // ???? Charge Power Watt:	Measurement.Metering.GridMs.TotWIn.ChaSta 
//	public double Power => charger.GetMeasurements<List<MeasurmentsJson>>()[14].values[0].value;							   // Charge Power Watt:	Measurement.Metering.GridMs.TotWIn.ChaSta 
}
