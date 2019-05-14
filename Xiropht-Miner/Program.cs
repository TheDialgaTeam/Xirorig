using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xiropht_Connector_All.Setting;

namespace Xiropht_Miner
{
    public class ClassCommandLine
    {
        public const string CommandLineHelp = "h";
        public const string CommandLineStats = "s";
        public const string CommandLineJob = "j";
    }

    public class Program
    {
        private const string UnexpectedExceptionFile = "\\error_miner.txt";
        public static bool Exit;
        private static Thread ThreadCommandLine;

        static void Main(string[] args)
        {
            EnableCatchUnexpectedException();
            Console.CancelKeyPress += Console_CancelKeyPress;

            Thread.CurrentThread.Name = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
            ClassConsole.ConsoleWriteLine(ClassConnectorSetting.CoinName + " Miner - " + Assembly.GetExecutingAssembly().GetName().Version + "R");
            ClassMiningConfig.MiningConfigInitialization();
            StartCommandLine();
            Task.Factory.StartNew(async () =>
            {
                bool connected = await ClassMiningNetwork.StartMiningAsync();
                while (!connected)
                {
                   await Task.Delay(5000);
                   connected = await ClassMiningNetwork.StartMiningAsync();
                }
            }).ConfigureAwait(false);
        }





        #region Events 

        private static void StartCommandLine()
        {
            ThreadCommandLine = new Thread(delegate ()
            {
                ShowCommandLineHelp();
                while(!Exit)
                {
                    StringBuilder input = new StringBuilder();
                    var key = Console.ReadKey(true);
                    input.Append(key.KeyChar);
                    CommandLine(input.ToString().ToLower());
                    input.Clear();
                }
            });
            ThreadCommandLine.Start();
        }

        /// <summary>
        /// Show command lines help.
        /// </summary>
        private static void ShowCommandLineHelp()
        {
            ClassConsole.ConsoleWriteLine(ClassCommandLine.CommandLineHelp + " - show list of command lines.", ClassConsoleEnumeration.IndexPoolConsoleMagentaLog);
            ClassConsole.ConsoleWriteLine(ClassCommandLine.CommandLineStats + " - show current stats of the miner.", ClassConsoleEnumeration.IndexPoolConsoleMagentaLog);
            ClassConsole.ConsoleWriteLine(ClassCommandLine.CommandLineJob + " - show current jobs.", ClassConsoleEnumeration.IndexPoolConsoleMagentaLog);
        }

        /// <summary>
        /// Proceed command line.
        /// </summary>
        /// <param name="command"></param>
        private static void CommandLine(string command)
        {
            switch (command)
            {
                case ClassCommandLine.CommandLineHelp:
                    ShowCommandLineHelp();
                    break;
                case ClassCommandLine.CommandLineJob:
                    ClassConsole.ConsoleWriteLine("Current Mining Difficulty: " + ClassMiningStats.CurrentMiningDifficulty + " | Current Mining Job: " + ClassMiningStats.CurrentJobIndication + " | Block ID: " + ClassMiningStats.CurrentBlockId + " | Block Difficulty: " + ClassMiningStats.CurrentBlockDifficulty, ClassConsoleEnumeration.IndexPoolConsoleMagentaLog);
                    break;
                case ClassCommandLine.CommandLineStats:
                    try
                    {
                        ClassConsole.ConsoleWriteLine("Estimated Hashrate: " + ClassMining.TotalHashrate + " H/s | Good Share: " + ClassMiningStats.TotalGoodShare + " Invalid Share: " + ClassMiningStats.TotalInvalidShare, ClassConsoleEnumeration.IndexPoolConsoleMagentaLog);
                    }
                    catch
                    {

                    }
                    break;
            }
        }

        /// <summary>
        /// Catch unexpected exception and them to a log file.
        /// </summary>
        private static void EnableCatchUnexpectedException()
        {
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args2)
            {
                var filePath = ClassUtility.ConvertPath(Directory.GetCurrentDirectory() + UnexpectedExceptionFile);
                var exception = (Exception)args2.ExceptionObject;
                using (var writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine("Message :" + exception.Message + "<br/>" + Environment.NewLine +
                                     "StackTrace :" +
                                     exception.StackTrace +
                                     "" + Environment.NewLine + "Date :" + DateTime.Now);
                    writer.WriteLine(Environment.NewLine +
                                     "-----------------------------------------------------------------------------" +
                                     Environment.NewLine);
                }

                Trace.TraceError(exception.StackTrace);
                Console.WriteLine("Unexpected error catched, check the error file: " + ClassUtility.ConvertPath(Directory.GetCurrentDirectory() + UnexpectedExceptionFile));
                Environment.Exit(1);

            };
        }

        /// <summary>
        /// Event for detect Cancel Key pressed by the user for close the program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Exit = true;
            e.Cancel = true;
            Console.WriteLine("Close miner tool.");
            Process.GetCurrentProcess().Kill();
        }

        #endregion
    }
}
