// Program..: EMail.cs
// Author...: G. Wassink
// Design...:
// Date.....: 15/06/2019 Last revised: 08/10/2024
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class (EMail)

using System.Net.Mail;

namespace gwEmail.Classes;

public class EmailConfig
{
	public string[] TOs = [];
	public string[]? CCs = [];
	public string From = "";
	public string FromDisplayName = "";
	public string Subject = "";
	public string ClientCredentialUserName = "";
	public string ClientCredentialPassword = "";
	public MailPriority Priority;
	public string Host = "";
	public bool EnableSsl;
}
