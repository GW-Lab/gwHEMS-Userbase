// Program..: ModbusApiApiClient.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/09/2024 Last revised: 29/06/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Publish  : dotnet publish --runtime linux-arm
// Publish	: dotnet publish --runtime linux-arm64
// Reserved.: Type Class (ModbusApiApiClient)

using FluentModbus;
using gwLogging;
using gwModbus.Classes;
using System.Net;

namespace gwModbus;

public enum BatteryStatus : uint
{
   Fault = 35,
   Off = 303,
   Ok  = 307,
   Warning = 455,
   InfoNotAvailable = 16777213
}

public enum InverterStatus : int
{
   Fault = 35,
   Off = 303,
   Ok = 307,
   Warning = 455
}

public enum BatteryControlEnum
{
   Init,
   Inverter,
   Remote,
}

public class ModbusApiClient : ModbusTcpClient
{
   private readonly IPAddress ip;
   public BatteryIntern Battery;

   public ModbusApiClient(IPAddress ipAddress)
   {
      this.ip = ipAddress;
      this.Battery = new(ipAddress, this);
   }
   public short ActivePowerACLimit
   {
      set
      {
         Connect(ip, ModbusEndianness.BigEndian);

         if (IsConnected)
         {

            this.ActivePowerACLimitApi(percent: value);

            Disconnect();
         }
      }
   }
   public int PowerAC
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

   public int PowerDC
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

   public int ImportFromGrid
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

   public int ExportToGrid
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

   public InverterStatus Status
   {
      get
      {
         InverterStatus status;

         Connect(ip, ModbusEndianness.BigEndian);

         if (IsConnected)
         {
            status = (InverterStatus)ReadInputRegisters<int>(3, 30201, 1)[0];

            Disconnect();
         }
         else
         {
            status = InverterStatus.Fault;                                                                  // Status 
         }

         return status;
      }
   }

   public int InverterTemprature
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


   public class BatteryIntern(IPAddress ip, ModbusApiClient mbc)
   {
      public BatteryControlEnum ControlledBy = BatteryControlEnum.Init;

      public BatteryControlEnum Control(BatteryControlEnum control)
      {
         mbc.Connect(ip, ModbusEndianness.BigEndian);

         if (mbc.IsConnected)
         {
            if (ControlledBy != control)
            {
               this.ControlledBy = control;
               mbc.BatteryControlApi(control);

               Logging.Log("Battery", $"{control} controlled");
            }

            mbc.Disconnect();
         }

         return control;
      }

      public BatteryControlEnum ControllForce(BatteryControlEnum control)
      {
         mbc.Connect(ip, ModbusEndianness.BigEndian);

         if (mbc.IsConnected)
         {
            this.ControlledBy = control;
            mbc.BatteryControlApi(control);

            mbc.Disconnect();

            Logging.Log("Battery", $"{control} controlled:" ,"","force");
         }

         return control;
      }

      public int PowerAC
      {
         get
         {
            mbc.Connect(ip, ModbusEndianness.BigEndian);

            var power = 0;

            if (mbc.IsConnected)
            {
               power = (int)mbc.BatteryChargeApi();

               power = power > 0 ? -power : (int)mbc.BatteryDischargeApi();

               mbc.Disconnect();
            }

            return power;
         }
         set
         {
            {
               mbc.Connect(ip, ModbusEndianness.BigEndian);

               if (mbc.IsConnected)
               {
                  if (ControlledBy != BatteryControlEnum.Remote)
                  {
                     this.ControlledBy = BatteryControlEnum.Remote;                                                       // Discharge battery (5000 watt) stops after 1/2 hour 
                     mbc.BatteryControlApi(this.ControlledBy);                                                            // Set remote control
                     mbc.BatteryPowerApi(value);
                     
                     if (value > 0)
                        Logging.Log("Battery", $"Discharge {Math.Abs(value)}W");
                     else
                        Logging.Log("Battery", $"Charge {Math.Abs(value)}W");
                  }
               }

               mbc.Disconnect();
            }
         }
      }

      public int PowerACForced
      {
         get
         {
            mbc.Connect(ip, ModbusEndianness.BigEndian);

            var power = 0;

            if (mbc.IsConnected)
            {
               power = (int)mbc.BatteryChargeApi();

               power = power > 0 ? -power : (int)mbc.BatteryDischargeApi();

               mbc.Disconnect();
            }

            return power;
         }
         set
         {
            {
               mbc.Connect(ip, ModbusEndianness.BigEndian);

               if (mbc.IsConnected)
               {
                  this.ControlledBy = BatteryControlEnum.Remote;                                                          // Discharge battery (5000 watt) stops after 1/2 hour 
                  mbc.BatteryControlApi(this.ControlledBy);                                                               // Set remote control
                  mbc.BatteryPowerApi(value);
                  
                  if (value > 0)
                     Logging.Log("Battery", $"Discharge {Math.Abs(value)}W","","forced");
                  else
                     Logging.Log("Battery", $"Charge {Math.Abs(value)}W","", "forced"); 
               }

               mbc.Disconnect();
            }
         }
      }
      public uint SOC
      {
         get
         {
            uint soc = 0;

            mbc.Connect(ip, ModbusEndianness.BigEndian);

            if (mbc.IsConnected)
            {
               soc = mbc.BatterySOCApi();

               mbc.Disconnect();
            }

            return soc;
         }
      }

      public BatteryStatus Status
      {
         get
         {
            BatteryStatus status = BatteryStatus.Off;

            mbc.Connect(ip, ModbusEndianness.BigEndian);

            if (mbc.IsConnected)
            {
               status = (BatteryStatus)mbc.BatteryStatusApi();   // BatteryStatus

               mbc.Disconnect();
            }

            return status;
         }
      }

      public int Temprature
      {
         get
         {
            int temp = 0;

            mbc.Connect(ip, ModbusEndianness.BigEndian);

            if (mbc.IsConnected)
            {
               temp = mbc.BatteryTempratureApi() / 10;   // Battery temprature (Celcius)

               mbc.Disconnect();
            }

            return temp;
         }
      }
   }
}

