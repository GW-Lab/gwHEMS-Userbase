// Program..: EMailContent.cs
// Author...: G. Wassink
// Design...:
// Date.....: 15/06/2019 Last revised: 08/10/2024
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class (EMailContent)

namespace gwEmail.Classes;

public class EmailContent
{
	public bool IsHtml = true;
	public string body = "";
	public string AttachFile = "";
}
