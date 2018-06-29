using System;
using System.Collections.Generic;
using System.Text;

namespace Santiago
{
    class Utility
    {
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
           
    }
}
