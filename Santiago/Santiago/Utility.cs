using System;
using System.Collections.Generic;
using System.Text;

namespace Santiago
{
    class Utility
    {
        public static bool isDebug = true;

        public static void Debug(string logMessage)
        {
            if (isDebug)
            {
                Console.WriteLine($"DEBUG - {DateTime.Now:T}: {logMessage}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public static void Log(string logMessage)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"LOG - {DateTime.Now:T}: {logMessage}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Alert(string logMessage)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"ALERT - {DateTime.Now:T}: {logMessage}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Error(string logMessage)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR - {DateTime.Now:T}: {logMessage}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void PrintCardCall(CardCall cc)
        {
            var strResult = "";
            if (cc.Result == CallResult.Unknown) strResult = "Unknown...";
            else strResult = cc.Result == CallResult.Hit ? "Hit!" : "Miss!";

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"{cc.SenderName} called {cc.CardRequested} from {cc.TargetName} and it was a {strResult}");
            Console.ForegroundColor = ConsoleColor.White;
        }

    }
}
