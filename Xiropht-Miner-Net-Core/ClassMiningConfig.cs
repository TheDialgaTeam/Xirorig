using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
        public const string MiningConfigUseIntelligentCalculation = "MINING_USE_INTELLIGENT_CALCULATION";
        public const string MiningConfigAdditionJobThread = "MINING_ADDITION_JOB_THREAD";
        public const string MiningConfigSubtractionJobThread = "MINING_SUBTRACTION_JOB_THREAD";
        public const string MiningConfigMultiplicationJobThread = "MINING_MULTIPLICAITON_JOB_THREAD";
        public const string MiningConfigDivisionJobThread = "MINING_DIVISION_JOB_THREAD";
        public const string MiningConfigModulusJobThread = "MINING_MODULUS_JOB_THREAD";
        public const string MiningConfigProcessorAffinity = "MINING_PROCESSOR_AFFINITY";
        public const string MiningConfigDeveloperFee = "MINING_DEV_FEE";
    }

    public class ClassMiningConfig
    {
        private const string MiningConfigFile = "\\config.ini";
        public static string MiningPoolHost;
        public static int MiningPoolPort;
        public static string MiningWalletAddress;
        public static int MiningConfigThread;
        public static int MiningConfigThreadIntensity;
        public static bool MiningConfigUseIntelligentCalculation;
        public static int MiningConfigAdditionJobThread;
        public static int MiningConfigSubtractionJobThread;
        public static int MiningConfigMultiplicationJobThread;
        public static int MiningConfigDivisionJobThread;
        public static int MiningConfigModulusJobThread;
        public static int MiningConfigProcessorAffinity;
        public static int MiningConfigDeveloperFee = 1;

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
                    var numberOfLines = 0;
                    var line = string.Empty;

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
                                                        MiningConfigThreadIntensity = 4;

                                                    if (MiningConfigThreadIntensity < 0)
                                                        MiningConfigThreadIntensity = 0;
                                                    break;
                                                case ClassMiningConfigEnumeration.MiningConfigUseIntelligentCalculation:
                                                    MiningConfigUseIntelligentCalculation = splitLine[1].Equals("Y", StringComparison.OrdinalIgnoreCase);
                                                    break;
                                                case ClassMiningConfigEnumeration.MiningConfigAdditionJobThread:
                                                    MiningConfigAdditionJobThread = int.Parse(splitLine[1]);
                                                    if (MiningConfigAdditionJobThread < 0)
                                                        MiningConfigAdditionJobThread = 0;
                                                    break;
                                                case ClassMiningConfigEnumeration.MiningConfigSubtractionJobThread:
                                                    MiningConfigSubtractionJobThread = int.Parse(splitLine[1]);
                                                    if (MiningConfigSubtractionJobThread < 0)
                                                        MiningConfigSubtractionJobThread = 0;
                                                    break;
                                                case ClassMiningConfigEnumeration.MiningConfigMultiplicationJobThread:
                                                    MiningConfigMultiplicationJobThread = int.Parse(splitLine[1]);
                                                    if (MiningConfigMultiplicationJobThread < 0)
                                                        MiningConfigMultiplicationJobThread = 0;
                                                    break;
                                                case ClassMiningConfigEnumeration.MiningConfigDivisionJobThread:
                                                    MiningConfigDivisionJobThread = int.Parse(splitLine[1]);
                                                    if (MiningConfigDivisionJobThread < 0)
                                                        MiningConfigDivisionJobThread = 0;
                                                    break;
                                                case ClassMiningConfigEnumeration.MiningConfigModulusJobThread:
                                                    MiningConfigModulusJobThread = int.Parse(splitLine[1]);
                                                    if (MiningConfigModulusJobThread < 0)
                                                        MiningConfigModulusJobThread = 0;
                                                    break;
                                                case ClassMiningConfigEnumeration.MiningConfigProcessorAffinity:
                                                    MiningConfigProcessorAffinity = int.Parse(splitLine[1].Replace("0x", ""), NumberStyles.HexNumber);

                                                    var currentProcess = Process.GetCurrentProcess();
                                                    currentProcess.ProcessorAffinity = (IntPtr) MiningConfigProcessorAffinity;
                                                    break;
                                                case ClassMiningConfigEnumeration.MiningConfigDeveloperFee:
                                                    MiningConfigDeveloperFee = int.Parse(splitLine[1]);
                                                    if (MiningConfigDeveloperFee < 1)
                                                        MiningConfigDeveloperFee = 1;
                                                    if (MiningConfigDeveloperFee > 100)
                                                        MiningConfigDeveloperFee = 100;
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
                var tmpwall = Console.ReadLine();

                while (tmpwall.Length > ClassConnectorSetting.MaxWalletAddressSize || tmpwall.Length < ClassConnectorSetting.MinWalletAddressSize)
                {
                    ClassConsole.ConsoleWriteLine("Input wallet address is wrong, Xiropht wallet addresses are between 48 and 96 characters long, please try again: ", ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
                    tmpwall = Console.ReadLine();
                }

                MiningWalletAddress = tmpwall;
                ClassConsole.ConsoleWriteLine("Write the mining pool host: ", ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
                MiningPoolHost = Console.ReadLine();
                ClassConsole.ConsoleWriteLine("Write the mining pool port: ", ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
                var portTmp = 0;
                while (!int.TryParse(Console.ReadLine(), out portTmp))
                    ClassConsole.ConsoleWriteLine("Input port is wrong, please try again: ", ClassConsoleEnumeration.IndexPoolConsoleRedLog);
                MiningPoolPort = portTmp;
                ClassConsole.ConsoleWriteLine("Select the number of thread to use, detected thread " + Environment.ProcessorCount + ": ", ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
                var threadTmp = 0;
                while (!int.TryParse(Console.ReadLine(), out threadTmp))
                    ClassConsole.ConsoleWriteLine("Input number of thread is wrong, please try again: ", ClassConsoleEnumeration.IndexPoolConsoleRedLog);
                MiningConfigThread = threadTmp;

                ClassConsole.ConsoleWriteLine("Select the intensity of thread(s) to use, min 0 | max 4: ", ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
                var threadIntensityTmp = 0;
                while (!int.TryParse(Console.ReadLine(), out threadIntensityTmp))
                    ClassConsole.ConsoleWriteLine("Input intensity of thread(s) is wrong, please try again: ", ClassConsoleEnumeration.IndexPoolConsoleRedLog);
                MiningConfigThreadIntensity = threadIntensityTmp;
                if (MiningConfigThreadIntensity > 4)
                    MiningConfigThreadIntensity = 4;

                if (MiningConfigThreadIntensity < 0)
                    MiningConfigThreadIntensity = 0;

                using (var streamWriterConfigMiner = new StreamWriter(ClassUtility.ConvertPath(Directory.GetCurrentDirectory() + MiningConfigFile)) { AutoFlush = true })
                {
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigWalletAdress + "=" + MiningWalletAddress);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigPoolHost + "=" + MiningPoolHost);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigPoolPort + "=" + MiningPoolPort);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigThread + "=" + MiningConfigThread);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigThreadIntensity + "=" + MiningConfigThreadIntensity);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigUseIntelligentCalculation + "=" + (MiningConfigUseIntelligentCalculation ? "Y" : "N"));
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigAdditionJobThread + "=" + 1);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigSubtractionJobThread + "=" + 1);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigMultiplicationJobThread + "=" + 1);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigDivisionJobThread + "=" + 1);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigModulusJobThread + "=" + 1);
                    streamWriterConfigMiner.WriteLine(ClassMiningConfigEnumeration.MiningConfigDeveloperFee + "=" + 1);
                }

                ClassConsole.ConsoleWriteLine(ClassUtility.ConvertPath(Directory.GetCurrentDirectory() + MiningConfigFile) + " miner config file saved");
            }
        }
    }
}