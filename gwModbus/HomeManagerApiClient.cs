// Program..: HomeManagerApiClient.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/09/2024 Last revised: 24/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Reserved.: Type Class (HomeManagerApiClient)

using gwLogging;
using System.Net;

namespace gwModbus;

public class HomeManagerApiClient(IPAddress ipAddress) : ModbusApiClient(ipAddress)
{
   public void PVPlantActivePowerACLimit(PowerPercentage power, PVInverterApiClient pv)
   {
      if (base.ActivePowerACLimit(power, pv))                                           // Cancel zero export mode (Energy prices are positive)
         Logging.Log("PV (plant)", $"Active Power={power}", "", $"Battery {pv.Battery.Control} controlled");
   }
}
