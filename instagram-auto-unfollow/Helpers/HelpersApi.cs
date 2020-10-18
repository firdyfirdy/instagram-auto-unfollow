﻿using InstagramApiSharp.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace instagram_auto_unfollow.Helpers
{
  public static class HelpersApi
  {
    public static IInstaApi InstaApi { get; set; }
    
    public static void WriteLine(string value, ConsoleColor color = ConsoleColor.DarkGreen)
    {
      //
      // This method writes an entire line to the console with the string.
      //
      Console.ForegroundColor = color;
      Console.Write(value);
      Console.ResetColor();
      Console.WriteLine();
    }
    public static long UnixTimeNow()
    {
      var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
      return (long)timeSpan.TotalSeconds;
    }
  }
}
