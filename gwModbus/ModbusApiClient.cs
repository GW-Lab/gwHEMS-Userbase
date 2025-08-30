// Program..: ModbusApiApiClient.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/09/2024 Last revised: 24/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Reserved.: Type Class (ModbusApiApiClient)
// 
// Used to manage ModBus devices (Sunny Home Manager, Inverters etc ...) 
// Inloggen ip een SMA Inverter: Kabel in inverter en laptop. Met je laptop surfen naar http://169.254.12.3, opgelost.

using FluentModbus;
using gwModbus.Classes;
using System.Net;

namespace gwModbus;

public enum InverterStatus : int
{
   Fault = 35,
   Off = 303,
   Ok = 307,
   Warning = 455
}

public enum PowerPercentage : short
{
   None = -1,
   ZeroExport = 0,
   P25 = 25,
   P50 = 50,
   P75 = 75,
   Maximum = 100
}

public class ModbusApiClient : ModbusTcpClient
{
   private readonly IPAddress ip;
   private PowerPercentage activePowerOld = PowerPercentage.None;

   public Battery Battery;

   public ModbusApiClient(IPAddress ipAddress)
   {
      this.ip = ipAddress;
      this.Battery = new(ipAddress, this);
   }

   protected bool ActivePowerACLimit(PowerPercentage power, ModbusApiClient pv)        // Zero-Export=0%, Normal=100% (SHM = plant scope, Inverter = Inverter scope)
   {
      if (power != activePowerOld)
      {
         Connect(ip, ModbusEndianness.BigEndian);

         if (IsConnected)
         {
            this.ActivePowerACLimitApi(value: (short)power);                           // Set HM automaticaly to Inverter controlled

            Disconnect();
         }

         this.activePowerOld = power;

         return true;
      }else{
         return false;
      }
   }

   protected int PowerAC // Applicable -> PVInverter
   {
      get
      {
         int power = 0;

         Connect(ip, ModbusEndianness.BigEndian);

         if (IsConnected)
         {
            power = this.PowerACApi();

            Disconnect();
         }

         return power;
      }
   }

   protected int PowerDC
   {
      get
      {
         int power = 0;

         Connect(ip, ModbusEndianness.BigEndian);

         if (IsConnected)
         {
            power = this.PowerDCApi();

            Disconnect();
         }

         return power;
      }
   }

   public int ImportFromGrid  // Applicable -> HomeManager & PVInverter
   {
      get
      {
         int power = 0;

         Connect(ip, ModbusEndianness.BigEndian);

         if (IsConnected)
         {
            power = this.ImportFromGridApi();

            Disconnect();
         }

         return power;
      }
   }

   public int ExportToGrid // Applicable -> HomeManager & PVInverter
   {
      get
      {
         int power = 0;

         Connect(ip, ModbusEndianness.BigEndian);

         if (IsConnected)
         {
            power = this.ExportToGridApi();

            Disconnect();
         }

         return power;
      }
   }

   public InverterStatus Status // Applicable HomeManager + PVInverter
   {
      get
      {
         InverterStatus status = InverterStatus.Fault;

         Connect(ip, ModbusEndianness.BigEndian);

         if (IsConnected)
         {
            status = (InverterStatus)ReadInputRegisters<int>(3, 30201, 1)[0];

            Disconnect();
         }

         return status;
      }
   }
   
   protected int InverterTemprature // Applicable -> PVInverter
   {
      get
      {
         int temp = 0;

         Connect(ip, ModbusEndianness.BigEndian);

         if (IsConnected)
         {
            temp = this.TempratureApi() / 10;   // Inverter temprature (Celcius)

            Disconnect();
         }

         return temp;
      }
   }
}
