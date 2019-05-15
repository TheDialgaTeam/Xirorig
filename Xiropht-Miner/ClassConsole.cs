using System;

namespace Xiropht_Miner
{
    public class ClassConsoleEnumeration
    {
        public const int IndexPoolConsoleGreenLog = 0;
        public const int IndexPoolConsoleYellowLog = 1;
        public const int IndexPoolConsoleRedLog = 2;
        public const int IndexPoolConsoleWhiteLog = 3;
        public const int IndexPoolConsoleBlueLog = 4;
        public const int IndexPoolConsoleMagentaLog = 5;
    }

    public class ClassConsole
    {
        /// <summary>
        /// Log on the console.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="logId"></param>
        /// <param name="logLevel"></param>
        /// <param name="writeLog"></param>
        public static void ConsoleWriteLine(string text, int colorId = 0)
        {
            switch (colorId)
            {
                case ClassConsoleEnumeration.IndexPoolConsoleGreenLog:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;

                case ClassConsoleEnumeration.IndexPoolConsoleYellowLog:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case ClassConsoleEnumeration.IndexPoolConsoleRedLog:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                case ClassConsoleEnumeration.IndexPoolConsoleBlueLog:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;

                case ClassConsoleEnumeration.IndexPoolConsoleMagentaLog:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.WriteLine(DateTime.Now + " - " + text);
        }
    }
}