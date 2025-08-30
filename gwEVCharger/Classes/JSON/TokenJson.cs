// Program..: TokenJSON.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/09/2024 Last revised: 25/10/2024
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Publish  : dotnet publish --runtime linux-arm64
// Reserved.: Type Class (Process)

namespace gwEVCharger.Classes.JSON;

internal class TokenJson
{
	public string Access_token  = "";
	public int Expires_in  = 0;
	public string Refresh_token  = "";
	public string Token_type = "";
	public string UiIdleTime = "";
}
