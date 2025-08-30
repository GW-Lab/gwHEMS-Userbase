// Program..: KM200Modul.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 17/10/2024
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Publish  : dotnet publish --runtime linux-arm
// Publish  : dotnet publish --runtime linux-arm64
// Reserved.: Type Class (KM200Modul) -> Interface/API to the Buderus KM2300 Tcp/ip interface

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// AES-Key - Generator für das KM200 Web Gateway
// https://km200.andreashahn.info/

namespace gwEnviline.Classes;
public class KM200Modul
{
	private readonly byte[] Accesskey;
	private readonly byte[] Salt = [0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0];
	private readonly IPAddress ip;

   private static readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true, IncludeFields = true };

	public KM200Modul(IPAddress ip, string unitPassword, string gatewayPassword)
	{
		this.ip = ip;
		Accesskey = Key(unitPassword, gatewayPassword);
	}

	protected T ApiRequestGet<T>(string query) where T : new()
	{
		using HttpClient client = new();
		client.DefaultRequestHeaders.Add("User-Agent", "TeleHeater/2.2.3");

		var response = client.GetAsync(new Uri($"Http://{ip}/{query}")).Result;

		if (response.IsSuccessStatusCode)
		{
			var json = Decrypt(response.Content.ReadAsStringAsync().Result);
			var result = JsonSerializer.Deserialize<T>(Decrypt(response.Content.ReadAsStringAsync().Result), jsonOptions);

			return result ?? new T();
		}
		else
		{
			return new T();
		}
	}

	protected bool ApiRequestPut<T>(string query, T val)
	{
		using HttpClient client = new();
		client.BaseAddress = new Uri($"http://{ip}");
		client.DefaultRequestHeaders.Add("User-Agent", "TeleHeater/2.2.3");

		/*var payload = new { value = newRoomTemp };   */

		var payload = new { value = val };   // Ensure this matches the API's expected payload structure
		var content = new StringContent(Encrypt(JsonSerializer.Serialize(payload)), Encoding.UTF8, "application/json");

		HttpResponseMessage response = client.PutAsync(query, content).Result;

		return response.IsSuccessStatusCode;
	}

	//protected bool ApiRequestPut(string query,float value)
	//{
	//	using HttpClient client = new();
	//	client.BaseAddress = new Uri($"http://{ip}");
	//	client.DefaultRequestHeaders.Add("User-Agent", "TeleHeater/2.2.3");

	//	/*var payload = new { value = newRoomTemp };   */

	//	var payload = new { value };   // Ensure this matches the API's expected payload structure
	//	var content = new StringContent(Encrypt(JsonSerializer.Serialize(payload)), Encoding.UTF8, "application/json");

	//	HttpResponseMessage response = client.PutAsync(query, content).Result;

	//	return response.IsSuccessStatusCode;
	//}

	private byte[] Key(string unitPassword, string gatewayPassword)
	{
		var unitPasswordList = new List<byte>();
		unitPasswordList.AddRange(Encoding.UTF8.GetBytes(unitPassword));
		unitPasswordList.AddRange(this.Salt);

		var gateWayPasswordList = new List<byte>();
		gateWayPasswordList.AddRange(this.Salt);
		gateWayPasswordList.AddRange(Encoding.UTF8.GetBytes(gatewayPassword));

		var returnKey = new List<byte>();

		//using (var Md5 = new  MD5CryptoServiceProvider())     .Net 8.0 Obsolate                      // hex: 05b7aa6d275fee477c0caff040989bd4 key1.ComputeHash(l1.ToArray) ' byte():	5 183 170 109 39 95 238 71 124 12 175 240 64 152 155 212
		//{
		//	returnKey.AddRange(Md5.ComputeHash(unitPasswordList.ToArray()));
		//	returnKey.AddRange(Md5.ComputeHash(gateWayPasswordList.ToArray()));
		//}

		returnKey.AddRange(MD5.HashData([.. unitPasswordList]));
		returnKey.AddRange(MD5.HashData([.. gateWayPasswordList]));

		return [.. returnKey];
	}

	private string Decrypt(string text)                                                                // Helper -> Decript the Json text data
	{
		using var encryptor = Aes.Create();
		encryptor.Padding = PaddingMode.Zeros;
		encryptor.Mode = CipherMode.ECB;

		var aesKey = new byte[32];

		Array.Copy(this.Accesskey, 0, aesKey, 0, 32);
		encryptor.Key = aesKey;
		encryptor.IV = new byte[16];

		using var memoryStream = new MemoryStream();
		using var cryptoStream = new CryptoStream(memoryStream, encryptor.CreateDecryptor(), CryptoStreamMode.Write);
		var cipherBytes = Convert.FromBase64String(text);

		cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
		cryptoStream.FlushFinalBlock();

		var plainBytes = memoryStream.ToArray();

		return Encoding.UTF8.GetString(plainBytes, 0, plainBytes.Length).Trim('\0');                    // Remove \0 terminators from String end
	}

	private String Encrypt(string text)                                                                // Helper -> Encript the Json text data
	{
		using var encryptor = Aes.Create();
		encryptor.Mode = CipherMode.ECB;
		encryptor.Padding = PaddingMode.Zeros;

		var aesKey = new byte[32];
		Array.Copy(this.Accesskey, 0, aesKey, 0, 32);

		encryptor.Key = aesKey;
		encryptor.IV = new byte[16];

		using var memoryStream = new MemoryStream();
		using var cryptoStream = new CryptoStream(memoryStream, encryptor.CreateEncryptor(), CryptoStreamMode.Write);
		var blockSize = 16;
		var @char = blockSize - (text.Length % blockSize);

		var plainBytes = Encoding.UTF8.GetBytes(text + new string(Convert.ToChar(@char), @char));

		cryptoStream.Write(plainBytes, 0, plainBytes.Length);
		cryptoStream.FlushFinalBlock();

		var cipherBytes = memoryStream.ToArray();

		return Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);
	}
}
