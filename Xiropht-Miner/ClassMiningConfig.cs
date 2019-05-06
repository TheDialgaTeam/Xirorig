using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xiropht_Connector_All.Setting;

namespace Xiropht_Miner
{
    public class ClassMiningConfigEnumeration
    {
        public const string MiningConfigPoolHost = "MINING_POOL_HOST";
        public const string MiningConfigPoolPort = "MINING_POOL_PORT";
        public const string MiningConfigWalletAdress = "MINING_WALLET_ADDRESS";
        public const string MiningConfigThread = "MINING_THREAD";
        public const string MiningConfigThreadIntensity = "MINING_THREAD_INTENSITY";
    }

    public class ClassMiningConfig
    {
        private const string MiningConfigFile = "\\config.ini";
        public static string MiningPoolHost;
        public static int MiningPoolPort;
        public static string MiningWalletAddress;
        public static int MiningConfigThread;
        public static int MiningConfigThreadIntensity;

        /// <summary>
        /// Initilize mining configuration.
        /// </summary>
        /// <returns></returns>
        public static void MiningConfigInitialization()
        {
            if (File.Exists(ClassUtility.ConvertPath(Directory.GetCurrentDirectory() + MiningConfigFile)))
            {
                using (var streamReaderConfigPool = new StreamReader(ClassUtility.ConvertPath(Directory.GetCurrentDirectory() + MiningConfigFile)))
                {
                    int numberOfLines = 0;
                    string line = string.Empty;
                    while ((line = streamReaderConfigPool.ReadLine()) != null)
                    {
                        numberOfLines++;
                        if (!string.IsNullOrEmpty(line))
                        {
                            if (!line.StartsWith("/"))
                            {
                                if (line.Contains("="))
                                {
                                    var splitLine = line.Split(new[] { "=" }, StringSplitOptions.None);
                                    if (splitLine.Length > 1)
                                    {
                                        try
                                        {
#if DEBUG
                                            Debug.WriteLine("Config line read: " + splitLine[0] + " argument read: " + splitLine[1]);
#endif
                                            switch (splitLine[0])
                                            {
                                                case ClassMiningConfigEnumeration.MiningConfigPoolHost:
                                                    MiningPoolHost = splitLine[1];
                                                    break;
                                                case ClassMiningConfigEnumeration.MiningConfigPoolPort:
                                                    MiningPoolPort = int.Parse(splitLine[1]);
                                                    break;
                                                case ClassMiningConfigEnumeration.MiningConfigWalletAdress:
                                                    MiningWalletAddress = splitLine[1];
                                                    break;
                                                case ClassMiningConfigEnumeration.MiningConfigThread:
                                                    MiningConfigThread = int.Parse(splitLine[1]);
                                                    break;
                                                case ClassMiningConfigEnumeration.MiningConfigThreadIntensity:
                                                    MiningConfigThreadIntensity = int.Parse(splitLine[1]);
                                                    if (MiningConfigThreadIntensity > 4)
                                                    {
                                                        MiningConfigThreadIntensity = 4;
                                                    }
                                                    if (MiningConfigThreadIntensity < 0)
                                                    {
                                                        MiningConfigThreadIntensity = 0;
                                                    }
                                                    break;

                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Error on line:" + numberOfLines);
                                        }
                                    }
#if DEBUG
                                    else
                                    {
                                        Debug.WriteLine("Error on config line: " + splitLine[0] + " on line:" + numberOfLines);
                                    }
#endif
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                File.Create(ClassUtility.ConvertPath(Directory.GetCurrentDirectory() + MiningConfigFile)).Close();
                ClassConsole.ConsoleWriteLine("Write your wallet address: ", ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
                string tmpwall = Console.ReadLine();
                while (tmpwall.Length > ClassConnectorSetting.MaxWalletAddressSize || tmpwall.Length < ClassConnectorSetting.MinWalletAddressSize)
                {
                    ClassConsole.ConsoleWriteLine("Input wallet address is wrong, Xiropht wallet addresses are between 48 and 96 characters long, please try again: ", ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
                    tmpwall = Console.ReadLine();
                }

                MiningWalletAddress = tmpwall;
                ClassConsole.ConsoleWriteLine("Write the mining pool host: ", ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
                MiningPoolHost = Console.ReadLine();
                ClassConsole.ConsoleWriteLine("Write the mining pool port: ", ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
                int portTmp = 0;
                while (!int.TryParse(Console.ReadLine(), out portTmp))
                {
                    ClassConsole.ConsoleWriteLine("Input port is wrong, please try again: ", ClassConsoleEnumeration.IndexPoolConsoleRedLog);
                }
                MiningPoolPort = portTmp;
                ClassConsole.ConsoleWriteLine("Select the number of thread to use, detected thread " + Environment.ProcessorCount + ": ", ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
                int threadTmp = 0;
                while (!int.TryParse(Console.ReadLine(), out threadTmp))
                {
                    ClassConsole.ConsoleWriteLine("Input number of thread is wrong, please try again: ", ClassConsoleEnumeration.IndexPoolConsoleRedLog);
                }
                MiningConfigThread = threadTmp;

                ClassConsole.ConsoleWriteLine("Select the intensity of thread(s) to use, min 0 | max 4: ", ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
                int threadIntensityTmp = 0;
                while (!int.TryParse(Console.ReadLine(), out threadIntensityTmp))
                {
                    ClassConsole.ConsoleWriteLine("Input intensity of thread(s) is wrong, please try again: ", ClassConsoleEnumeration.IndexPoolConsoleRedLog);
                }
                MiningConfigThreadIntensity = threadIntensityTmp;
                if (MiningConfigThreadIntensity > 4)
                {
                    MiningConfigThreadIntensity = 4;
                }
                if (MiningConfigThreadIntensity < 0)
                {
                    MiningConfigThreadIntensity = 0;
                }

                using (var streamWriterConfigMiner = new StreamWriter(ClassUtility.ConvertPath(Directory.GetCurrentDirectory() + MiningConfigFile)) { AutoFlush = true })
                {
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigWalletAdress + "=" + MiningWalletAddress);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigPoolHost + "=" + MiningPoolHost);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigPoolPort + "=" + MiningPoolPort);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigThread + "=" + MiningConfigThread);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigThreadIntensity + "=" + MiningConfigThreadIntensity);
                }
                ClassConsole.ConsoleWriteLine(ClassUtility.ConvertPath(Directory.GetCurrentDirectory() + MiningConfigFile) + " miner config file saved", ClassConsoleEnumeration.IndexPoolConsoleGreenLog);
            }
        }
    }
}
