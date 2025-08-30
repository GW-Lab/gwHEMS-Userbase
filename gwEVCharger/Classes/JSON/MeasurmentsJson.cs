// Program..: GWChargerJSON.cs
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

public class MeasurmentsJson
{
   public string channelId = "";
   public string componentId = "";
	public List<Value> values = [];

	public class Value
	{
		public DateTime time = DateTime.Now;
		public double value = 0;
	}
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<List<SMACharger>>(myJsonResponse);
// VolgNr
// 00 [{"channelId":"Measurement.ChaSess.WhIn","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:56:26.535Z","value":14}]},
// 01 {"channelId":"Measurement.Chrg.ModSw","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:47:22.580Z","value":4718}]},
// 02 {"channelId":"Measurement.GridMs.A.phsA","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:08.100Z","value":0}]},
// 03 {"channelId":"Measurement.GridMs.A.phsB","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:08.099Z","value":0}]},
// 04 {"channelId":"Measurement.GridMs.A.phsC","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:08.108Z","value":0}]},
// 05 {"channelId":"Measurement.GridMs.Hz","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:06.641Z","value":50.04}]},
// 06 {"channelId":"Measurement.GridMs.PhV.phsA","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:06.649Z","value":229.3}]},
// 07 {"channelId":"Measurement.GridMs.PhV.phsB","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:06.647Z","value":227.9}]},
// 08 {"channelId":"Measurement.GridMs.PhV.phsC","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:06.651Z","value":225.1}]},
// 09 {"channelId":"Measurement.GridMs.TotPF","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:06.641Z","value":1}]},
// 10 {"channelId":"Measurement.GridMs.TotVA","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:06.641Z","value":0}]},
// 11 {"channelId":"Measurement.GridMs.TotVAr","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:06.641Z","value":0}]},
// 12 {"channelId":"Measurement.InOut.GI1","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:08.080Z","value":0}]},
// 13 {"channelId":"Measurement.Metering.GridMs.TotWIn","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:06.641Z","value":0}]},
// 14 {"channelId":"Measurement.Metering.GridMs.TotWIn.ChaSta","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:06.641Z","value":0}]},
// 15 {"channelId":"Measurement.Metering.GridMs.TotWhIn","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:06.600Z","value":671018}]},
// 16 {"channelId":"Measurement.Metering.GridMs.TotWhIn.ChaSta","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:06.600Z","value":671018}]},
// 17 {"channelId":"Measurement.Operation.EVeh.ChaStt","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:07.651Z","value":200112}]},
// 18 {"channelId":"Measurement.Operation.EVeh.Health","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:59:08.098Z","value":307}]},
// 19 {"channelId":"Measurement.Operation.Evt.Msg","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:56:26.821Z","value":302}]},
// 20 {"channelId":"Measurement.Operation.Health","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:56:26.821Z","value":307}]},
// 21 {"channelId":"Measurement.Operation.WMaxLimNom","componentId":"IGULD:SELF","values":[{"time":"1970-01-01T00:00:00.973Z"}]},
// 22 {"channelId":"Measurement.Operation.WMaxLimSrc","componentId":"IGULD:SELF","values":[{"time":"2024-09-07T12:10:02.648Z","value":2608}]},
// 23 {"channelId":"Measurement.Wl.ConnStt","componentId":"IGULD:SELF","values":[{"time":"2024-09-07T12:10:10.243Z","value":303}]},
// 24 {"channelId":"Measurement.Wl.SigPwr","componentId":"IGULD:SELF","values":[{"time":"2024-09-07T12:10:10.244Z","value":0}]},
// 25 {"channelId":"Measurement.Wl.SoftAcsConnStt","componentId":"IGULD:SELF","values":[{"time":"2024-09-24T08:43:16.833Z","value":303}]},
// 26 {"channelId":"Setpoint.PlantControl.PCC.ChrgActCnt","componentId":"IGULD:SELF","values":[{"time":"2024-09-25T21:58:29.069Z","value":1}]}]

