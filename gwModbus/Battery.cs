// Program..: HomeStorage.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/09/2024 Last revised: 29/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Reserved.: Type Class (HomeStorage)

using FluentModbus;
using gwLogging;
using gwModbus.Classes;
using System.Net;

namespace gwModbus;

public enum BatteryStatus : uint
{
   Fault = 35,
   Off = 303,
   Ok = 307,
   Warning = 455,
   InfoNotAvailable = 16777213
}

public enum BatteryControl
{
   Init,
   Inverter,
   Remote,
}

public class Battery(IPAddress ip, ModbusApiClient mbc)
{
   private BatteryControl controlledBy = BatteryControl.Init;

   public enum BatteryPower : int
   {
      ChargeMax = -6250,                                 // Charge power			(Watt)
      ChargeMidium= -3000,
      ChargeMin = -1000,                                 // Charge power			(Watt)
      DischargeMax = 6250,                               // Discharge power		(Watt) 
      DischargeMidium = 3000,
      DischargeMin = 1000,                               // Discharge power		(Watt) 
      Blocked = 0                                        // Battery blocked => not Charging or Discharging
   }

   public BatteryControl Control                         //Control(BatteryControl control)
   {
      get { return controlledBy; }
      set
      {
         mbc.Connect(ip, ModbusEndianness.BigEndian);

         if (mbc.IsConnected)
         {
            if (controlledBy != value)
            {
               controlledBy = value;
               mbc.BatteryControlApi(value);

               Logging.Log("Battery", $"{value} controlled", "", $"SOC {SOC}%");
            }

            mbc.Disconnect();
         }
      }
   }

   public BatteryControl ControllRepeat
   {
      set
      {
         mbc.Connect(ip, ModbusEndianness.BigEndian);

         if (mbc.IsConnected)
         {
            controlledBy = value;
            mbc.BatteryControlApi(value);

            mbc.Disconnect();

            Logging.Log("Battery", $"{value} controlled:", "", "repeat");
         }

      }
   }

   public int Power
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
         mbc.Connect(ip, ModbusEndianness.BigEndian);

         if (mbc.IsConnected)
         {
            if (controlledBy != BatteryControl.Remote)
            {
               controlledBy = BatteryControl.Remote;
               mbc.BatteryControlApi(controlledBy);                              // Set remote control
               mbc.BatteryPowerApi(value);                                       // Charge or Discharge battery (5000 watt) stops after 1/2 hour 

               if (value > 0)
                  Logging.Log("Battery", $"Discharge {Math.Abs(value)}W");
               else
                  Logging.Log("Battery", $"Charge {Math.Abs(value)}W", "", $"(Bat. {controlledBy} ctrl)");
            }
         }

         mbc.Disconnect();
      }
   }

   public int PowerRepeat
   {
      set
      {
         mbc.Connect(ip, ModbusEndianness.BigEndian);

         if (mbc.IsConnected)
         {
            controlledBy = BatteryControl.Remote;
            mbc.BatteryControlApi(controlledBy);                               // Set remote control
            mbc.BatteryPowerApi(value);                                        // Discharge battery (5000 watt) stops after 1/2 hour so forced repeat the command 

            if (value > 0)
               Logging.Log("Battery", $"Discharge {Math.Abs(value)}W", "", "repeat");
            else
               Logging.Log("Battery", $"Charge {Math.Abs(value)}W", "", "repeat");
         }

         mbc.Disconnect();
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
            status = (BatteryStatus)mbc.BatteryStatusApi(); // BatteryStatus

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
            temp = mbc.BatteryTempratureApi() / 10;         // Battery temprature (Celcius)

            mbc.Disconnect();
         }

         return temp;
      }
   }
}

