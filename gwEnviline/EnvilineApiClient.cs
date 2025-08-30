// Program..: EnvilineApiClient.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 17/10/2024
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Publish  : dotnet publish --runtime linux-arm
// Publish  : dotnet publish --runtime linux-arm64
// Reserved.: Type Class (EnvilineApiClient) -> Interface/API to the Top of the Appication

using gwEnviline.Classes;
using gwEnviline.Classes.JSON;
using System.Net;

namespace gwEnviline;

public enum DHWChargeStatus		// Note: Content is case sensetive
{
	init,
	start,
	stop
}


public enum DHWOperationMode		// Note: Content is case sensetive
{
	low,        // DHW redused 48℃
	Off,        // 0℃
	eco,        // DHW EcoPlus 48℃
	high,       // DHW On+DHW 60℃
	ownprogram, // DHW Own 48℃
	notset
}

public class EnvilineApiClient(IPAddress ip, string unitPassword, string gatewayPassword) : KM200Modul(ip, unitPassword, gatewayPassword)
{
	/* Write */
	public bool DHWTempLevels(float value) => ApiRequestPut<float>("dhwCircuits/dhw1/temperatureLevels", value);
	public bool DHWTempLevelsHigh(float value) => ApiRequestPut<float>("dhwCircuits/dhw1/temperatureLevels/high", value);
	public bool DHWCharge(DHWChargeStatus value) => ApiRequestPut<string>("dhwCircuits/dhw1/charge", value.ToString());              // Allowable val = ["start" , "stop"]  **** Beware uses Electric heatingrod 2300watt ***
	public bool DHWChargeDuration(float value) => ApiRequestPut<float>("dhwCircuits/dhw1/chargeDuration", value);                    // Allowable val = {'id': '/dhwCircuits/dhw1/chargeDuration', 'type': 'floatValue', 'writeable': 1, 'recordable': 0, 'value': 60.0, 'unitOfMeasure': 'mins', 'minValue': 15.0, 'maxValue': 2880.0} 
	public bool IndoorTempSetpoint(float value) => ApiRequestPut<float>("heatingCircuits/hc1/temporaryRoomSetpoint", value);         // Dim jsonText = Me.KM.WebRequestGet("dhwCircuits/dhw1/waterFlow") ' {"id":"/dhwCircuits/dhw1/waterFlow","type":"floatValue","writeable":0,"recordable":0,"value":0.0,"unitOfMeasure":"l/min"}
	public bool DHWTempSetpoint(float value) => ApiRequestPut<float>("dhwCircuits/dhw1/currentSetpoint", value);                     // xxx (Forbidden)
	public DHWOperationMode DHWoperationMode
	{
		get { return (DHWOperationMode)Enum.Parse(typeof(DHWOperationMode), ApiRequestGet<StringJSON>("dhwCircuits/dhw1/operationMode").value); }
		set { ApiRequestPut<string>("dhwCircuits/dhw1/operationMode", value.ToString()); }                                            // Allowable: ["Off", "low" "eco", "high", 'ownprogram']
	}
	public bool DHWSingleChargeSetpoint(float value) => ApiRequestPut<float>("dhwCircuits/dhw1/singleChargeSetpoint", value);        // Allowable: [""]
	public bool DHWWaterflow(float value) => ApiRequestPut<float>("dhwCircuits/dhw1/waterFlow", value);                              // {"id":"/dhwCircuits/dhw1/waterFlow","type":"floatValue","writeable":0,"recordable":0,"value":0.0,"unitOfMeasure":"l/min"}

	/* Read */
	public float DHWCurrentSetpoint => ApiRequestGet<FloatJSON>("dhwCircuits/dhw1/currentSetpoint").value;
	// public float DHWTemperatureLevelOff => ApiRequestGet<FloatJSON>("dhwCircuits/dhw1/temperatureLevels/off").value;
	public float ActualModulation => ApiRequestGet<FloatJSON>("heatSources/actualModulation").value;
	public float CVReturnTemp => ApiRequestGet<FloatJSON>("heatSources/returnTemperature").value;                                    //	Dim jsonText = ApiRequest("heatSources/numberOfStarts") ' {"id""/heatSources/actualModulation","type":"floatValue","writeable":0,"recordable":0,"value":0.0,"unitOfMeasure":"%"}
	public float CVSupplyTemp => ApiRequestGet<FloatJSON>("heatSources/applianceSupplyTemperature").value;                           // Dim jsonText = ApiRequest("heatSources/returnTemperature") '{"id""/heatSources/returnTemperature","type":"floatValue","writeable":0,"recordable":0,"value":26.2,"unitOfMeasure":"C","state":[{"open":-3276.8},{ "short":3276.7}]}
	public float DHWActualPower => ApiRequestGet<FloatJSON>("heatSources/nominalDHWPower").value;                                    //	Dim jsonText = ApiRequest("heatSources/numberOfStarts") ' {"id":"/heatSources/nominalDHWPower","type":"floatValue","writeable":0,"recordable":0,"value":24.0,"unitOfMeasure":"kW"}
	public string DHWStatus => ApiRequestGet<StringJSON>("dhwCircuits/dhw1/status").value;                                           // Dim jsonText = ApiRequest("heatSources/workingTime/totalSystem") ' {"id""/heatSources/workingTime/totalSystem","type":"floatValue","writeable":0,"recordable":0,"value":77600,"unitOfMeasure":"s"}
	public float DHWTemp => ApiRequestGet<FloatJSON>("dhwCircuits/dhw1/actualTemp").value;                                           // var dhw = ApiRequestGet("dhwCircuits/dhw1/actualTemp");
	public string DHWWaterflow() => ApiRequestGet<StringJSON>("dhwCircuits/dhw1/waterFlow").value;                                   //	Dim jsonText = Me.KM.WebRequestGet("dhwCircuits/dhw1/waterFlow") ' {"id""/dhwCircuits/dhw1/status","type":"stringValue","writeable":0,"recordable":0,"value":"ACTIVE","allowedValues":["INACTIVE", "ACTIVE"]}
	public string FirmwareVersion => ApiRequestGet<StringJSON>("gateway/versionFirmware").value;
	public float IndoorTemp => ApiRequestGet<FloatJSON>("heatingCircuits/hc1/roomtemperature").value;                                // {"id":"/heatSources/total/energyMonitoring/outputProduced","uri":"http://192.168.2.106/heatSources/total/energyMonitoring/outputProduced"},
	public float HC1PumpModulation => ApiRequestGet<FloatJSON>("heatingCircuits/hc1/pumpModulation").value;                          // Dim jsonText = Me.KM.WebRequestGet("heatingCircuits/hc1/pumpModulation") ' { "id""/dhwCircuits/dhw1/status","type":"stringValue","writeable":0,"recordable":0,"value":"ACTIVE","allowedValues":["INACTIVE", "ACTIVE"]}
	public string HealthStatus => ApiRequestGet<StringJSON>("system/healthStatus").value;                                            //	Dim jsonText = ApiRequest("gateway/versionFirmware")
	public float Info => ApiRequestGet<FloatJSON>("dhwCircuits/dhw1/workingTime").value;
	public float MinOutdoorTemp => ApiRequestGet<FloatJSON>("system/minOutdoorTemp").value;                                          // Dim jsonText = Me.KM.WebRequestGet("dhwCircuits/dhw1/operationMode") ' { "id""/dhwCircuits/dhw1/status","type":"stringValue","writeable":0,"recordable":0,"value":"ACTIVE","allowedValues":["INACTIVE", "ACTIVE"]}
	public float NumberOfStarts => ApiRequestGet<FloatJSON>("heatSources/numberOfStarts").value;                                     // Dim jsonText = Me.KM.WebRequestGet("system/healthStatus")
	public float OutputProduced => ApiRequestGet<FloatJSON>("heatSources/total/energyMonitoring/outputProduced").value;
	public float OutdoorTemp => ApiRequestGet<FloatJSON>("system/sensors/temperatures/outdoor_t1").value;                            // Dim jsonText = ApiRequest("system/sensors/temperatures/outdoor_t1")
	public float UpTime => ApiRequestGet<FloatJSON>("heatSources/workingTime/totalSystem").value / 3600;                             // Dim jsonText = ApiRequest("heatSources/applianceSupplyTemperature") '' {"id":"/heatSources/applianceSupplyTemperature","type":"floatValue","writeable":0,"recordable":0,"value":26.6,"unitOfMeasure":"C","state":[{"open":-3276.8},{"short":3276.7}]}
}

