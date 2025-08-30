// Program..: Program.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 18/09/2024
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type internal Class (Program)

using gwHEMS.Classes;

namespace gwHEMS;

internal class Program
{
   static void Main(string[] args)
   {
      _ = new Hems();
      Console.WriteLine($"gwHEMS  -> Version: {Util.Version}");
      Console.WriteLine("Running...");
      Console.ReadLine();
   }
}

// Here’s a consolidated list of the core functions you’ll typically find in a modern Home Energy Management System(HEMS), based on industry guides and technical descriptions.
//Core Monitoring & Control
//Real‑time energy monitoring – Tracks household electricity consumption, generation, and storage in real time.
//Appliance‑level metering – Measures usage per device or circuit for detailed insights.
//Load control & scheduling – Turns devices on/off or shifts operation to cheaper or greener times.
//Remote control – Manage appliances, EV chargers, and heating/cooling from an app or web dashboard.
//🌞 Renewable Integration
//Photovoltaic (PV) management – Optimizes use of rooftop solar, prioritizing self‑consumption.
//Battery storage control – Stores surplus solar power for later use, discharges during peak prices.
//Micro‑generation coordination – Integrates small wind turbines or other on‑site generation.
//🔌 Smart Device & Grid Interaction
//EV charging optimization – Charges electric vehicles when electricity is cheapest or greenest.
//Heat pump & HVAC optimization – Adjusts heating/cooling schedules for efficiency.
//Demand response participation – Reduces or shifts load in response to utility price signals or grid needs.
//Grid import/export management – Controls when to draw from or feed into the grid.
//📊 Data & Analytics
//Usage history & trends – Visualizes past consumption, generation, and costs.
//Forecasting – Predicts solar output, consumption, and costs based on weather and patterns.
//Cost optimization – Suggests or automates actions to minimize bills.
//Carbon footprint tracking – Estimates CO₂ savings from renewable use and efficiency.

//🔐 System Intelligence & Connectivity
//Automation rules – “If‑this‑then‑that” style triggers for devices and loads.
//Interoperability – Works with smart home protocols (KNX, Zigbee, Matter, etc.).
//Cloud or local control – Some systems run locally for privacy; others use cloud for advanced analytics.
//Security & failover – Protects against cyber threats and ensures operation during outages.
//💡 In short: A HEMS acts as the central brain of your home’s energy ecosystem — balancing production, storage, and consumption to cut costs, increase self‑sufficiency, and reduce environmental impact.
//If you want, I can also map these functions into a C# class design so you can model a Home Energy Manager in software — with methods like OptimizeEVCharging() or GetCarbonSavingsReport(). Would you like me to do that?

//yes please
//Got it — let’s turn that list of Home Energy Management System (HEMS) functions into a C# class design you could actually build on. I’ll keep it modular so you can expand it later for real-world integration with IoT devices, APIs, or cloud services.

//🛠 Example C# Class Design for a HEMS
//csharp
//using System;
//using System.Collections.Generic;

//namespace SmartHomeEnergy
//{
//   public class HomeEnergyManager
//   {
//      // --- Properties ---
//      public double CurrentConsumptionKW { get; private set; }
//      public double CurrentGenerationKW { get; private set; }
//      public double BatteryChargeLevelPercent { get; private set; }
//      public double CarbonSavingsKg { get; private set; }
//      public Dictionary<string, double> ApplianceUsageKW { get; private set; }

//      // --- Constructor ---
//      public HomeEnergyManager()
//      {
//         ApplianceUsageKW = new Dictionary<string, double>();
//      }

//      // --- Monitoring ---
//      public void UpdateConsumption(double kw) => CurrentConsumptionKW = kw;
//      public void UpdateGeneration(double kw) => CurrentGenerationKW = kw;
//      public void UpdateBatteryLevel(double percent) => BatteryChargeLevelPercent = percent;
//      public void UpdateApplianceUsage(string appliance, double kw)
//          => ApplianceUsageKW[appliance] = kw;

//      // --- Control & Scheduling ---
//      public void ScheduleAppliance(string appliance, DateTime startTime)
//      {
//         Console.WriteLine($"Scheduled {appliance} to start at {startTime}");
//      }

//      public void TurnOffAppliance(string appliance)
//      {
//         Console.WriteLine($"{appliance} turned off to save energy.");
//      }

//      // --- Optimization ---
//      public void OptimizeEVCharging(DateTime preferredCompletionTime)
//      {
//         Console.WriteLine($"Optimizing EV charging to finish by {preferredCompletionTime}");
//      }

//      public void OptimizeHeatingCooling(double targetTemp)
//      {
//         Console.WriteLine($"Adjusting HVAC to maintain {targetTemp}°C efficiently.");
//      }

//      public void ManageBatteryStorage()
//      {
//         if (CurrentGenerationKW > CurrentConsumptionKW)
//            Console.WriteLine("Charging battery with surplus solar.");
//         else
//            Console.WriteLine("Discharging battery to meet demand.");
//      }

//      // --- Analytics ---
//      public double CalculateDailyCost(double pricePerKWh)
//      {
//         double totalKWh = CurrentConsumptionKW * 24; // Simplified
//         return totalKWh * pricePerKWh;
//      }

//      public void ForecastUsage()
//      {
//         Console.WriteLine("Forecasting tomorrow's energy usage based on historical data...");
//      }

//      public void CalculateCarbonSavings(double gridEmissionFactor)
//      {
//         CarbonSavingsKg = CurrentGenerationKW * gridEmissionFactor;
//      }
//   }
//}
//📌 How This Maps to HEMS Functions
//HEMS Function  C# Method / Property
//Real-time monitoring UpdateConsumption(), UpdateGeneration()
//Appliance-level metering   UpdateApplianceUsage()
//Load control & scheduling ScheduleAppliance(), TurnOffAppliance()
//EV charging optimization OptimizeEVCharging()
//HVAC optimization OptimizeHeatingCooling()
//Battery storage control ManageBatteryStorage()
//Cost optimization CalculateDailyCost()
//Forecasting ForecastUsage()
//Carbon tracking   CalculateCarbonSavings()
//If you want, I can extend this into a multi-class architecture with separate modules for SolarManager, BatteryManager, EVManager, and AnalyticsEngine, so it’s closer to how a real IoT-based HEMS would be structured.

//Do you want me to build that full modular version next? It would make the system far more scalable.

//ok
//Perfect — let’s scale up that single-class design into a modular, multi-class architecture so your Home Energy Management System is clean, extensible, and closer to how a real IoT-based HEMS would be built.

//🏗 Modular C# Architecture for a HEMS
//We’ll break it into managers for each subsystem, plus a central HomeEnergyManager that coordinates them.

//csharp
//using System;
//using System.Collections.Generic;

//namespace SmartHomeEnergy
//{
//   // --- Solar Management ---
//   public class SolarManager
//   {
//      public double CurrentGenerationKW { get; private set; }
//      public void UpdateGeneration(double kw) => CurrentGenerationKW = kw;
//      public void ForecastGeneration()
//      {
//         Console.WriteLine("Forecasting solar output based on weather data...");
//      }
//   }
//   // --- Battery Management ---
//   public class BatteryManager
//   {
//      public double ChargeLevelPercent { get; private set; }
//      public void UpdateChargeLevel(double percent) => ChargeLevelPercent = percent;
//      public void Charge(double kw)
//      {
//         Console.WriteLine($"Charging battery with {kw} kW.");
//      }
//      public void Discharge(double kw)
//      {
//         Console.WriteLine($"Discharging battery with {kw} kW.");
//      }
//   }
//   // --- EV Charging Management ---
//   public class EVManager
//   {
//      public void OptimizeCharging(DateTime completionTime)
//      {
//         Console.WriteLine($"Optimizing EV charging to complete by {completionTime}.");
//      }
//   }
//   // --- HVAC Management ---
//   public class HVACManager
//   {
//      public void OptimizeTemperature(double targetTemp)
//      {
//         Console.WriteLine($"Adjusting HVAC to maintain {targetTemp}°C efficiently.");
//      }
//   }
//   // --- Appliance Management ---
//   public class ApplianceManager
//   {
//      public Dictionary<string, double> ApplianceUsageKW { get; private set; } = new();
//      public void UpdateUsage(string appliance, double kw)
//      {
//         ApplianceUsageKW[appliance] = kw;
//      }
//      public void Schedule(string appliance, DateTime startTime)
//      {
//         Console.WriteLine($"Scheduled {appliance} to start at {startTime}.");
//      }
//      public void TurnOff(string appliance)
//      {
//         Console.WriteLine($"{appliance} turned off.");
//      }
//   }
//   // --- Analytics & Reporting ---
//   public class AnalyticsEngine
//   {
//      public void CalculateCarbonSavings(double generationKW, double gridEmissionFactor)
//      {
//         double savings = generationKW * gridEmissionFactor;
//         Console.WriteLine($"Carbon savings: {savings} kg CO₂.");
//      }
//      public void ForecastUsage()
//      {
//         Console.WriteLine("Forecasting household energy usage...");
//      }
//      public double CalculateDailyCost(double consumptionKW, double pricePerKWh)
//      {
//         double totalKWh = consumptionKW * 24; // Simplified
//         return totalKWh * pricePerKWh;
//      }
//   }
//   // --- Central Coordinator ---
//   public class HomeEnergyManager
//   {
//      public SolarManager Solar { get; } = new();
//      public BatteryManager Battery { get; } = new();
//      public EVManager EV { get; } = new();
//      public HVACManager HVAC { get; } = new();
//      public ApplianceManager Appliances { get; } = new();
//      public AnalyticsEngine Analytics { get; } = new();
//      public void OptimizeEnergyFlow()
//      {
//         if (Solar.CurrentGenerationKW > 0 && Battery.ChargeLevelPercent < 100)
//         {
//            Battery.Charge(Solar.CurrentGenerationKW);
//         }
//         else
//         {
//            Console.WriteLine("No surplus solar to store.");
//         }
//      }
//   }
//}
//🔍 Why This Structure Works
//Separation of Concerns – Each subsystem(solar, battery, EV, HVAC, appliances, analytics) has its own class.
//Scalability – You can add new modules(e.g., wind turbine manager, water heater manager) without touching the others.
//Testability – Each module can be unit-tested independently.
//Realism – Mirrors how actual HEMS software is layered: device drivers → subsystem managers → central coordinator.
//If you want, I can add an event-driven layer so that, for example, when solar output spikes, the system automatically triggers battery charging or appliance scheduling without manual calls. That would make it behave more like a real-time smart home brain.
//Do you want me to wire in that automation logic next? That’s where it starts to feel alive.

//ok
//Alright — let’s wire in that event‑driven automation layer so your modular Home Energy Management System can react to changes in real time, just like a real smart‑home brain.
//We’ll use C# events and delegates so that when one subsystem changes state (e.g., solar output spikes), other subsystems can respond automatically without you manually calling methods.
//⚡ Event‑Driven HEMS Architecture
//csharp
//using System;

//namespace SmartHomeEnergy
//{
//   // --- Event argument classes ---
//   public class EnergyChangedEventArgs : EventArgs
//   {
//      public double ValueKW { get; }
//      public EnergyChangedEventArgs(double valueKW) => ValueKW = valueKW;
//   }
//   public class BatteryLevelChangedEventArgs : EventArgs
//   {
//      public double LevelPercent { get; }
//      public BatteryLevelChangedEventArgs(double levelPercent) => LevelPercent = levelPercent;
//   }
//   // --- Solar Manager ---
//   public class SolarManager
//   {
//      public event EventHandler<EnergyChangedEventArgs> GenerationChanged;
//      private double _currentGenerationKW;
//      public double CurrentGenerationKW
//      {
//         get => _currentGenerationKW;
//         set
//         {
//            _currentGenerationKW = value;
//            GenerationChanged?.Invoke(this, new EnergyChangedEventArgs(value));
//         }
//      }
//   }
//   // --- Battery Manager ---
//   public class BatteryManager
//   {
//      public event EventHandler<BatteryLevelChangedEventArgs> LevelChanged;
//      private double _chargeLevelPercent;
//      public double ChargeLevelPercent
//      {
//         get => _chargeLevelPercent;
//         set
//         {
//            _chargeLevelPercent = value;
//            LevelChanged?.Invoke(this, new BatteryLevelChangedEventArgs(value));
//         }
//      }
//      public void Charge(double kw) => Console.WriteLine($"Charging battery with {kw} kW.");
//      public void Discharge(double kw) => Console.WriteLine($"Discharging battery with {kw} kW.");
//   }
//   // --- Appliance Manager ---
//   public class ApplianceManager
//   {
//      public void RunHighLoadAppliance(string appliance)
//      {
//         Console.WriteLine($"Running {appliance} while solar surplus is available.");
//      }
//   }
//   // --- Central Coordinator ---
//   public class HomeEnergyManager
//   {
//      public SolarManager Solar { get; } = new();
//      public BatteryManager Battery { get; } = new();
//      public ApplianceManager Appliances { get; } = new();
//      public HomeEnergyManager()
//      {
//         // Subscribe to solar changes
//         Solar.GenerationChanged += OnSolarGenerationChanged;
//         // Subscribe to battery level changes
//         Battery.LevelChanged += OnBatteryLevelChanged;
//      }
//      private void OnSolarGenerationChanged(object sender, EnergyChangedEventArgs e)
//      {
//         Console.WriteLine($"[Event] Solar generation changed: {e.ValueKW} kW");
//         if (e.ValueKW > 2.0 && Battery.ChargeLevelPercent < 100)
//         {
//            Battery.Charge(e.ValueKW);
//         }
//         else if (e.ValueKW > 2.0 && Battery.ChargeLevelPercent >= 100)
//         {
//            Appliances.RunHighLoadAppliance("Dishwasher");
//         }
//      }
//      private void OnBatteryLevelChanged(object sender, BatteryLevelChangedEventArgs e)
//      {
//         Console.WriteLine($"[Event] Battery level changed: {e.LevelPercent}%");
//         if (e.LevelPercent < 20)
//         {
//            Console.WriteLine("Battery low — consider reducing non‑essential loads.");
//         }
//      }
//   }
//}
//🔹 How it works
//Events:
//SolarManager.GenerationChanged fires whenever solar output changes.
//BatteryManager.LevelChanged fires whenever battery charge changes.
//Subscriptions:
//HomeEnergyManager listens to these events and decides what to do.
//Automation logic:
//If solar > 2 kW and battery not full → charge battery.
//If solar > 2 kW and battery full → run a high‑load appliance.
//If battery < 20% → warn user to reduce loads.

//Example usage
//csharp
//var hem = new SmartHomeEnergy.HomeEnergyManager();
//// Simulate solar production spike
//hem.Solar.CurrentGenerationKW = 3.5;
//// Simulate battery filling up
//hem.Battery.ChargeLevelPercent = 100;
//// Simulate another solar spike
//hem.Solar.CurrentGenerationKW = 4.0;
//// Simulate battery dropping low
//hem.Battery.ChargeLevelPercent = 15;
//💡 This pattern means you can keep adding new triggers — like price changes from EPEX Spot or weather forecasts — and have the system react without rewriting core logic.
//If you want, I can extend this further so it also reacts to dynamic electricity prices from EPEX Spot, automatically shifting loads to the cheapest hours.That would make it a fully price‑aware HEMS. Would you like me to add that?
