// Program..: Util.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 23/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: Extention class -> (ModbusApiClient) 
// Publish  : dotnet publish --runtime linux-arm64
// Reserved.: Type Class (Util)

namespace gwModbus.Classes;

public static class Util
{
   internal static void ActivePowerACLimitApi(this ModbusApiClient target, short value)
   {
      if (value >= 0 && value <= 100)
         target.WriteSingleRegister(2, 40016, value);                                                                                                                // Normalized Active Power Limition by PV system ctrl -> Range 0% to 100% -> default 100% (write only)
   }

   public static T ReadInputRegister<T>(this ModbusApiClient target, int unitIdentifier, int startingAddress) where T : unmanaged
   {
      return target.ReadInputRegisters<T>(unitIdentifier, startingAddress, 1)[0];
   }

   public static async Task<Memory<T>> ReadInputRegisterAsync<T>(this ModbusApiClient target, int unitIdentifier, int startingAddress) where T : unmanaged
   {
      return await target.ReadInputRegistersAsync<T>(unitIdentifier, startingAddress, 1);
   }

   internal static void BatteryPowerApi(this ModbusApiClient target, int power = 0)                                                                                  // Power > 0 is discharged battery, Power < 0 is charge battery 
   {
      target.WriteMultipleRegisters(3, 40149, [power]);                                                                                                              // Power (Watt)
   }

   internal static void BatteryControlApi(this ModbusApiClient target, BatteryControl control = BatteryControl.Inverter)
   {
      target.WriteMultipleRegisters<uint>(3, 40151, control == BatteryControl.Remote ? [802] : [803]);                                                               // 802 -> set control to remote : 803 -> set control to inverter
   }

   internal static void RebootApi(this ModbusApiClient target) => target.WriteMultipleRegisters<uint>(3, 40077, [1146]);

   internal static async void RebootApiAsyncApi(this ModbusApiClient target) => await target.WriteMultipleRegistersAsync<uint>(3, 40077, [1146]);

   /* ------- Battery Charge/Discharge Power (Watt) */
   internal static uint BatteryChargeApi(this ModbusApiClient target) => target.ReadInputRegister<uint>(3, 31393);
   public static async Task<Memory<uint>> BatteryChargeAsyncApi(this ModbusApiClient target) => await target.ReadInputRegisterAsync<uint>(3, 31393);                 // Battery Charge (Watt)
   internal static int BatteryDischargeApi(this ModbusApiClient target) => target.ReadInputRegister<int>(3, 31395);
   public static async Task<Memory<uint>> BatteryDischargeAsyncApi(this ModbusApiClient target) => await target.ReadInputRegisterAsync<uint>(3, 31395);              //  Battery Discharge (Watt)

   /* -------- Battery Charged/Discharged Energy (Wh)	*/                                                                                                                                                                                                                                                                                                       // Battery Energy Wh
   internal static ulong BatteryChargedEnergyApi(this ModbusApiClient target) => target.ReadInputRegister<ulong>(3, 31397);
   internal static async Task<Memory<ulong>> BatteryChargedEnergyAsyncApi(this ModbusApiClient target) => await target.ReadInputRegisterAsync<ulong>(3, 31397);      // Battery Charge (Wh)
   internal static ulong BatteryDischargedEnergyApi(this ModbusApiClient target) => target.ReadInputRegister<ulong>(3, 31401);
   internal static async Task<Memory<ulong>> BatteryDischargedEnergyAsyncApi(this ModbusApiClient target) => await target.ReadInputRegisterAsync<ulong>(3, 31401);   // Battery Discharge (Wh)
   internal static int PowerACApi(this ModbusApiClient target) => target.ReadInputRegister<int>(3, 30775);                                                           // Current AC power (Watt) 
   internal static int PowerDCApi(this ModbusApiClient target) => target.ReadInputRegister<int>(3, 30773);                                                           // Current DC power (Watt) 
   internal static uint BatterySOCApi(this ModbusApiClient target) => target.ReadInputRegister<uint>(3, 30845);                                                      // State Of Charge (%)
   internal static int ExportToGridApi(this ModbusApiClient target) => target.ReadInputRegister<int>(3, 30867);                                                      // Export to the Grid
   internal static int ImportFromGridApi(this ModbusApiClient target) => target.ReadInputRegister<int>(3, 30865);                                                    // Import from the grid
   internal static uint BatteryStatusApi(this ModbusApiClient target) => target.ReadInputRegister<uint>(3, 31391);
   internal static int BatteryTempratureApi(this ModbusApiClient target) => target.ReadInputRegister<int>(3, 30849);
   internal static int TempratureApi(this ModbusApiClient target) => target.ReadInputRegister<int>(3, 34113);
}
