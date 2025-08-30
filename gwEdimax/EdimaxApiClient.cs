// Program..: EdimaxApiClient.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 30/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.: 
// Publish  : dotnet publish --runtime linux-arm
// Publish  : dotnet publish --runtime linux-arm64
// Reserved.: Type Class (EdimaxApiClient)

using System.Net;

namespace gwEdimax;

public class EdimaxApiClient(IPAddress ip, string account, string password, int timeOutMiliseconds = 2000) : SP2101W(ip, account, password, timeOutMiliseconds)
{
}

