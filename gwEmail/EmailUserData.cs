// Program..: EMailUserData.cs
// Author...: G. Wassink
// Design...:
// Date.....: 15/06/2019 Last revised: 08/10/2024
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class (EMailUserData)

namespace gwEmail;

public class EmailUserData(string userName, string password, string mXRecord, string from, string[] to, string[] cc, bool ssl)
{
	public readonly string userName = userName;
	public readonly string password = password;
	public readonly string MXRecord = mXRecord;
	public readonly string From = from;
	public readonly string[] To = to;
	public readonly string[] CC = cc;
	public readonly bool Ssl = ssl;
}
