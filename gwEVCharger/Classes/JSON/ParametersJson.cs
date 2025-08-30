// Program..: ParametersJson.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/09/2024 Last revised: 25/10/2024
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Publish  : 
// Reserved.: Type Class (Json serializer support)

namespace gwEVCharger.Classes.JSON;

public class ParametersJson
{
	public string channelId = "";
	public string componentId = "";
	public List<Value> values = [];

	public class Value
	{
		public DateTime time = DateTime.Now;
		public string value = "";
	}
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<List<SMACharger>>(myJsonResponse);
// VolgNr
// 00 [{"channelId":"Measurement.ChaSess.WhIn","componentId":"IGULD:SELF","values":[{"time":"2024-11-25T06:58:51.266Z","value":0}]},
// 01 {"channelId":"Measurement.Chrg.ModSw","componentId":"IGULD:SELF","values":[{"time":"2024-11-23T15:06:50.438Z","value":4950}]},
// 02 {"channelId":"Measurement.GridMs.A.phsA","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:59:00.726Z","value":0}]},
// 03 {"channelId":"Measurement.GridMs.A.phsB","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:59:00.725Z","value":0}]},
// 04 {"channelId":"Measurement.GridMs.A.phsC","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:59:00.723Z","value":0}]},
// 05 {"channelId":"Measurement.GridMs.Hz","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:58:58.251Z","value":50.02}]},
// 06 {"channelId":"Measurement.GridMs.PhV.phsA","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:58:58.296Z","value":225.20000000000002}]},
// 06 {"channelId":"Measurement.GridMs.PhV.phsB","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:58:58.295Z","value":226.4}]},
// 07 {"channelId":"Measurement.GridMs.PhV.phsC","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:58:58.271Z","value":226.1}]},
// 08 {"channelId":"Measurement.GridMs.TotPF","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:58:58.252Z","value":1}]},
// 09 {"channelId":"Measurement.GridMs.TotVA","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:58:58.251Z","value":0}]},
// 10 {"channelId":"Measurement.GridMs.TotVAr","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:58:58.251Z","value":0}]},
// 11 {"channelId":"Measurement.InOut.GI1","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:59:00.066Z","value":0}]},
// 12 {"channelId":"Measurement.Metering.GridMs.TotWIn","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:58:58.251Z","value":0}]},
// 13 {"channelId":"Measurement.Metering.GridMs.TotWIn.ChaSta","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:58:58.251Z","value":0}]},
// 14 {"channelId":"Measurement.Metering.GridMs.TotWhIn","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:58:58.216Z","value":822435}]},
// 15 {"channelId":"Measurement.Metering.GridMs.TotWhIn.ChaSta","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:58:58.216Z","value":822435}]},
// 16 {"channelId":"Measurement.Operation.EVeh.ChaStt","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:59:00.281Z","value":200111}]},
// 17 {"channelId":"Measurement.Operation.EVeh.Health","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:59:00.722Z","value":307}]},
// 18 {"channelId":"Measurement.Operation.Evt.Msg","componentId":"IGULD:SELF","values":[{"time":"2024-11-24T06:03:48.337Z","value":302}]},
// 19 {"channelId":"Measurement.Operation.Health","componentId":"IGULD:SELF","values":[{"time":"2024-11-24T06:03:48.338Z","value":307}]},
// 20 {"channelId":"Measurement.Operation.WMaxLimNom","componentId":"IGULD:SELF","values":[{"time":"1970-01-01T00:00:00.973Z"}]},
// 21 {"channelId":"Measurement.Operation.WMaxLimSrc","componentId":"IGULD:SELF","values":[{"time":"2024-10-31T09:28:48.966Z","value":2608}]},
// 22 {"channelId":"Measurement.Wl.ConnStt","componentId":"IGULD:SELF","values":[{"time":"2024-10-31T09:28:56.540Z","value":303}]},
// 23 {"channelId":"Measurement.Wl.SigPwr","componentId":"IGULD:SELF","values":[{"time":"2024-10-31T09:28:56.541Z","value":0}]},
// 24 {"channelId":"Setpoint.PlantControl.PCC.ChrgActCnt","componentId":"IGULD:SELF","values":[{"time":"2024-11-27T08:58:50.126Z","value":0}]}]