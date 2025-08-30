// Program..: Logging.cs
// Author...: G. Wassink
// Design...:
// Date.....: 05/08/2025 Last revised: 05/08/2025
// Notice...: Copyright 2025, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class

namespace gwLogging;

public static class Logging
{
   public static void Log(string header, string col1, string col2 = "", string col3 ="")
   {
      if (col2 != "")
         Console.WriteLine($"{header,-13} -> {col1,-23} : {col2} {col3}" );
      else
         Console.WriteLine($"{header,-13} -> {col1,-23} : {DateTime.Now:HH:mm} {col3}");
   }
}
