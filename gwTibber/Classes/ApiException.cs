// Program..: ApiException.cs
// Author...: G. Wassink
// Design...:
// Date.....: 25/02/2024 Last revised: 28/10/2024
// Notice...: Copyright 2025, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class

namespace gwTibber.Classes;

    public class ApiException : Exception
    {
        public ApiException(string message) : base(message)
        { }

        public ApiException(string message, Exception innerException) : base(message, innerException)
        { }
    }
