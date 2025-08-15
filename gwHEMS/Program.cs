// Program..: Program.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 18/09/2024
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type internal Class (Program)

using GWHEMS.Classes;

namespace GWHEMS;

internal class Program
{
   static void Main(string[] args)
   {
      _ = new Hems();
      Console.WriteLine($"gwHEMS  -> Version: {Util.Version}");
      Console.WriteLine("Running...");
      Console.ReadLine();
   }
}
