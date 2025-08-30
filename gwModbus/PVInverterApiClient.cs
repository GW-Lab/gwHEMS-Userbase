// Program..: PVInverterApiClient.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/09/2024 Last revised: 24/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Reserved.: Type Class (PVInverterApiClient)

using gwLogging;
using System.Net;

namespace gwModbus;

public class PVInverterApiClient(IPAddress ipAddress) : ModbusApiClient(ipAddress)
{
   public new int PowerAC => base.PowerAC;
   public new int PowerDC => base.PowerDC;
   public int Temprature => base.InverterTemprature;
   public void ActivePowerACLimit(PowerPercentage power, PVInverterApiClient pv)
   {
      if (base.ActivePowerACLimit(power, pv))                                    // Cancel Zero-Export mode (Energy prices are positive)
         Logging.Log("PV (Inverter)", $"Active Power={power}", "", $"Battery {pv.Battery.Control} controlled");
   }
}