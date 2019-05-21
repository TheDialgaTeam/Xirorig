using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xiropht_Connector_All.Utils;

namespace Xiropht_Miner
{
    public class ClassMining
    {
        public static long TotalHashrate { get; private set; }

        public static ConcurrentDictionary<int, long> DictionaryMiningThread { get; } = new ConcurrentDictionary<int, long>();

        public static ConcurrentDictionary<string, int> SubmittedShares { get; } = new ConcurrentDictionary<string, int>();

        private static bool ThreadMiningRunning { get; set; }

        private static bool EstimatedCalculationSpeed { get; set; }

        private static Thread[] RandomJobThread { get; set; }

        private static Thread[] AdditionJobThread { get; set; }

        private static Thread[] SubtractionJobThread { get; set; }

        private static Thread[] MultiplicationJobThread { get; set; }

        private static Thread[] DivisionJobThread { get; set; }

        private static Thread[] ModulusJobThread { get; set; }

        private static int AdditionJobIndexOffset { get; set; }

        private static int SubtractionJobIndexOffset { get; set; }

        private static int MultiplicationJobIndexOffset { get; set; }

        private static int DivisionJobIndexOffset { get; set; }

        private static int ModulusJobIndexOffset { get; set; }

        /// <summary>
        /// Proceed mining
        /// </summary>
        public static void StartMining()
        {
            if (ThreadMiningRunning)
                return;

            StopMining();
            ThreadMiningRunning = true;

            StartMiningJob(RandomJobThread, 0, async i => await StartRandomJobAsync(i).ConfigureAwait(false));

            if (ClassMiningConfig.MiningConfigUseIntelligentCalculation)
            {
                StartMiningJob(AdditionJobThread, AdditionJobIndexOffset, async i => await StartAdditionJobAsync(i).ConfigureAwait(false));
                StartMiningJob(SubtractionJobThread, SubtractionJobIndexOffset, async i => await StartSubtractionJobAsync(i).ConfigureAwait(false));
                StartMiningJob(MultiplicationJobThread, MultiplicationJobIndexOffset, async i => await StartMultiplicationJobAsync(i).ConfigureAwait(false));
                StartMiningJob(DivisionJobThread, DivisionJobIndexOffset, async i => await StartDivisionJobAsync(i).ConfigureAwait(false));
                StartMiningJob(ModulusJobThread, ModulusJobIndexOffset, async i => await StartModulusJobAsync(i).ConfigureAwait(false));
            }

            if (EstimatedCalculationSpeed)
                return;

            EstimatedCalculationSpeed = true;
            CalculateCalculationSpeed();
        }

        private static void StartMiningJob(IList<Thread> jobThreads, int offset, Func<int, Task> function)
        {
            for (var i = 0; i < jobThreads.Count; i++)
            {
                var iOffset = i + 1 + offset;

                if (!DictionaryMiningThread.ContainsKey(i + offset))
                    DictionaryMiningThread[i + offset] = 0;

                jobThreads[i] = new Thread(async () => await function(iOffset).ConfigureAwait(false));

                switch (ClassMiningConfig.MiningConfigThreadIntensity)
                {
                    case 0:
                        jobThreads[i].Priority = ThreadPriority.Lowest;
                        break;

                    case 1:
                        jobThreads[i].Priority = ThreadPriority.BelowNormal;
                        break;

                    case 2:
                        jobThreads[i].Priority = ThreadPriority.Normal;
                        break;

                    case 3:
                        jobThreads[i].Priority = ThreadPriority.AboveNormal;
                        break;

                    case 4:
                        jobThreads[i].Priority = ThreadPriority.Highest;
                        break;

                    default:
                        jobThreads[i].Priority = ThreadPriority.Normal;
                        break;
                }

                jobThreads[i].IsBackground = true;
                jobThreads[i].Start();
            }
        }

        /// <summary>
        /// Clean calculation done every one second
        /// </summary>
        private static void CalculateCalculationSpeed()
        {
            new Thread(() =>
            {
                while (!Program.Exit)
                {
                    var totalHashrate = 0L;

                    for (var i = 0; i < DictionaryMiningThread.Count; i++)
                    {
                        totalHashrate += DictionaryMiningThread[i];
                        DictionaryMiningThread[i] = 0;
                    }

                    TotalHashrate = totalHashrate;
                    Thread.Sleep(1000);
                }
            }).Start();
        }

        /// <summary>
        /// Stop thread(s) on mining.
        /// </summary>
        private static void StopMining()
        {
            if (RandomJobThread == null)
            {
                RandomJobThread = new Thread[ClassMiningConfig.MiningConfigThread];

                if (!ClassMiningConfig.MiningConfigUseIntelligentCalculation)
                    return;

                AdditionJobIndexOffset = RandomJobThread.Length;
                AdditionJobThread = new Thread[ClassMiningConfig.MiningConfigAdditionJobThread];

                SubtractionJobIndexOffset = AdditionJobIndexOffset + AdditionJobThread.Length;
                SubtractionJobThread = new Thread[ClassMiningConfig.MiningConfigSubtractionJobThread];

                MultiplicationJobIndexOffset = SubtractionJobIndexOffset + SubtractionJobThread.Length;
                MultiplicationJobThread = new Thread[ClassMiningConfig.MiningConfigMultiplicationJobThread];

                DivisionJobIndexOffset = MultiplicationJobIndexOffset + MultiplicationJobThread.Length;
                DivisionJobThread = new Thread[ClassMiningConfig.MiningConfigDivisionJobThread];

                ModulusJobIndexOffset = DivisionJobIndexOffset + DivisionJobThread.Length;
                ModulusJobThread = new Thread[ClassMiningConfig.MiningConfigModulusJobThread];
            }
            else
            {
                StopMiningJob(RandomJobThread, 0);

                if (!ClassMiningConfig.MiningConfigUseIntelligentCalculation)
                    return;

                StopMiningJob(AdditionJobThread, AdditionJobIndexOffset);
                StopMiningJob(SubtractionJobThread, SubtractionJobIndexOffset);
                StopMiningJob(MultiplicationJobThread, MultiplicationJobIndexOffset);
                StopMiningJob(DivisionJobThread, DivisionJobIndexOffset);
                StopMiningJob(ModulusJobThread, ModulusJobIndexOffset);
            }
        }

        private static void StopMiningJob(IReadOnlyList<Thread> jobThreads, int offset)
        {
            for (var i = 0; i < jobThreads.Count; i++)
            {
                if (jobThreads[i] == null || !jobThreads[i].IsAlive)
                    continue;

                DictionaryMiningThread[i + offset] = 0;
                jobThreads[i].Abort();
                GC.SuppressFinalize(jobThreads[i]);
            }
        }

        private static async Task StartAdditionJobAsync(int threadId)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var range = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentBlockDifficulty - 3, ClassMiningConfig.MiningConfigAdditionJobThread, threadId - AdditionJobIndexOffset - 1, 4);
                var startRange = range.Item1;
                var endRange = range.Item2;
                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Addition | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var result = startRange; result <= endRange; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    foreach (var number in ClassMiningUtilities.SumOf(result))
                    {
                        var firstNumber = number.Item1;
                        var secondNumber = number.Item2;

                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (firstNumber < ClassMiningStats.CurrentMinRangeJob || firstNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        if (secondNumber < ClassMiningStats.CurrentMinRangeJob || secondNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        await DoCalculationAsync(firstNumber, secondNumber, "+", "Addition", threadId - 1).ConfigureAwait(false);
                    }
                }

                for (var result = startRange; result <= endRange; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    foreach (var number in ClassMiningUtilities.SumOf(result))
                    {
                        var firstNumber = number.Item1;
                        var secondNumber = number.Item2;

                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (firstNumber >= ClassMiningStats.CurrentMinRangeJob && firstNumber <= ClassMiningStats.CurrentMaxRangeJob &&
                            secondNumber >= ClassMiningStats.CurrentMinRangeJob && secondNumber <= ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        await DoCalculationAsync(firstNumber, secondNumber, "+", "Addition", threadId - 1).ConfigureAwait(false);
                    }
                }

                while (currentMiningJobIndication == ClassMiningStats.CurrentJobIndication)
                    await Task.Delay(1000).ConfigureAwait(false);
            }

            ThreadMiningRunning = false;
        }

        private static async Task StartSubtractionJobAsync(int threadId)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var range = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentBlockDifficulty - 1, ClassMiningConfig.MiningConfigSubtractionJobThread, threadId - SubtractionJobIndexOffset - 1, 2);
                var startRange = range.Item1;
                var endRange = range.Item2;
                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Subtraction | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var result = startRange; result <= endRange; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    foreach (var number in ClassMiningUtilities.SubtractOf(result))
                    {
                        var firstNumber = number.Item1;
                        var secondNumber = number.Item2;

                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (firstNumber < ClassMiningStats.CurrentMinRangeJob || firstNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        if (secondNumber < ClassMiningStats.CurrentMinRangeJob || secondNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        await DoCalculationAsync(firstNumber, secondNumber, "-", "Subtraction", threadId - 1).ConfigureAwait(false);
                    }
                }

                for (var result = startRange; result <= endRange; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    foreach (var number in ClassMiningUtilities.SubtractOf(result))
                    {
                        var firstNumber = number.Item1;
                        var secondNumber = number.Item2;

                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (firstNumber >= ClassMiningStats.CurrentMinRangeJob && firstNumber <= ClassMiningStats.CurrentMaxRangeJob &&
                            secondNumber >= ClassMiningStats.CurrentMinRangeJob && secondNumber <= ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        await DoCalculationAsync(firstNumber, secondNumber, "-", "Subtraction", threadId - 1).ConfigureAwait(false);
                    }
                }

                while (currentMiningJobIndication == ClassMiningStats.CurrentJobIndication)
                    await Task.Delay(1000).ConfigureAwait(false);
            }

            ThreadMiningRunning = false;
        }

        private static async Task StartMultiplicationJobAsync(int threadId)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var range = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentBlockDifficulty - 3, ClassMiningConfig.MiningConfigMultiplicationJobThread, threadId - MultiplicationJobIndexOffset - 1, 4);
                var startRange = range.Item1;
                var endRange = range.Item2;
                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Multiplication | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var result = startRange; result <= endRange; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    if (ClassMiningUtilities.IsPrimeNumber(result))
                        continue;

                    foreach (var number in ClassMiningUtilities.FactorOf(result))
                    {
                        var firstNumber = number.Item1;
                        var secondNumber = number.Item2;

                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (firstNumber < ClassMiningStats.CurrentMinRangeJob || firstNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        if (secondNumber < ClassMiningStats.CurrentMinRangeJob || secondNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        await DoCalculationAsync(firstNumber, secondNumber, "*", "Multiplication", threadId - 1).ConfigureAwait(false);
                    }
                }

                for (var result = startRange; result <= endRange; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    if (ClassMiningUtilities.IsPrimeNumber(result))
                        continue;

                    foreach (var number in ClassMiningUtilities.FactorOf(result))
                    {
                        var firstNumber = number.Item1;
                        var secondNumber = number.Item2;

                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (firstNumber >= ClassMiningStats.CurrentMinRangeJob && firstNumber <= ClassMiningStats.CurrentMaxRangeJob &&
                            secondNumber >= ClassMiningStats.CurrentMinRangeJob && secondNumber <= ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        await DoCalculationAsync(firstNumber, secondNumber, "*", "Multiplication", threadId - 1).ConfigureAwait(false);
                    }
                }

                while (currentMiningJobIndication == ClassMiningStats.CurrentJobIndication)
                    await Task.Delay(1000).ConfigureAwait(false);
            }

            ThreadMiningRunning = false;
        }

        private static async Task StartDivisionJobAsync(int threadId)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var range = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentBlockDifficulty - 1, ClassMiningConfig.MiningConfigDivisionJobThread, threadId - DivisionJobIndexOffset - 1, 2);
                var startRange = range.Item1;
                var endRange = range.Item2;
                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Division | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var result = ClassMiningStats.CurrentMinRangeJob; result <= ClassMiningStats.CurrentMaxRangeJob; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    if (ClassMiningUtilities.IsPrimeNumber(result))
                        continue;

                    foreach (var number in ClassMiningUtilities.DivisorOf(result))
                    {
                        var firstNumber = number.Item1;
                        var secondNumber = number.Item2;

                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (firstNumber < ClassMiningStats.CurrentMinRangeJob || firstNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        if (secondNumber < ClassMiningStats.CurrentMinRangeJob || secondNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        await DoCalculationAsync(firstNumber, secondNumber, "/", "Division", threadId - 1).ConfigureAwait(false);
                    }
                }

                for (var result = ClassMiningStats.CurrentMinRangeJob; result <= ClassMiningStats.CurrentMaxRangeJob; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    if (ClassMiningUtilities.IsPrimeNumber(result))
                        continue;

                    foreach (var number in ClassMiningUtilities.DivisorOf(result))
                    {
                        var firstNumber = number.Item1;
                        var secondNumber = number.Item2;

                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (firstNumber >= ClassMiningStats.CurrentMinRangeJob && firstNumber <= ClassMiningStats.CurrentMaxRangeJob &&
                            secondNumber >= ClassMiningStats.CurrentMinRangeJob && secondNumber <= ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        await DoCalculationAsync(firstNumber, secondNumber, "/", "Division", threadId - 1).ConfigureAwait(false);
                    }
                }

                while (currentMiningJobIndication == ClassMiningStats.CurrentJobIndication)
                    await Task.Delay(1000).ConfigureAwait(false);
            }

            ThreadMiningRunning = false;
        }

        private static async Task StartModulusJobAsync(int threadId)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var range = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentBlockDifficulty - 1, ClassMiningConfig.MiningConfigModulusJobThread, threadId - ModulusJobIndexOffset - 1, 2);
                var startRange = range.Item1;
                var endRange = range.Item2;
                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Modulus | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var firstNumber = startRange; firstNumber <= endRange; firstNumber++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    for (var secondNumber = firstNumber + 1; secondNumber <= ClassMiningStats.CurrentMaxRangeJob; secondNumber++)
                    {
                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        await DoCalculationAsync(firstNumber, secondNumber, "%", "Modulus", threadId - 1).ConfigureAwait(false);
                    }
                }

                for (var firstNumber = startRange; firstNumber <= endRange; firstNumber++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    for (var secondNumber = ClassMiningStats.CurrentMaxRangeJob + 1; secondNumber <= ClassMiningStats.CurrentBlockDifficulty; secondNumber++)
                    {
                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        await DoCalculationAsync(firstNumber, secondNumber, "%", "Modulus", threadId - 1).ConfigureAwait(false);
                    }
                }

                while (currentMiningJobIndication == ClassMiningStats.CurrentJobIndication)
                    await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        private static async Task StartRandomJobAsync(int threadId)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var range = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentBlockDifficulty - 1, ClassMiningConfig.MiningConfigThread, threadId - 1, 2);
                var startRange = range.Item1;
                var endRange = range.Item2;
                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Random | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                while (currentMiningJobIndication == ClassMiningStats.CurrentJobIndication)
                {
                    var firstNumber = ClassUtility.GenerateNumberMathCalculation(startRange, endRange);
                    var secondNumber = ClassUtility.GenerateNumberMathCalculation(startRange, endRange);

                    foreach (var randomOperator in ClassUtility.RandomOperatorCalculation)
                    {
                        await DoCalculationAsync(decimal.Parse(firstNumber), decimal.Parse(secondNumber), randomOperator, "Random", threadId - 1).ConfigureAwait(false);
                        await DoCalculationAsync(decimal.Parse(secondNumber), decimal.Parse(firstNumber), randomOperator, "Random", threadId - 1).ConfigureAwait(false);
                    }
                }
            }

            ThreadMiningRunning = false;
        }

        /// <summary>
        /// Encrypt math calculation with the current mining method
        /// </summary>
        private static string MakeEncryptedShare(string calculation, int idThread)
        {
            try
            {
                var encryptedShare = ClassUtility.StringToHexString(calculation + ClassMiningStats.CurrentBlockTimestampCreate);

                // Static XOR Encryption -> Key updated from the current mining method.
                encryptedShare = ClassUtility.EncryptXorShare(encryptedShare, ClassMiningStats.CurrentRoundXorKey.ToString());

                // Dynamic AES Encryption -> Size and Key's from the current mining method and the current block key encryption.
                for (var i = 0; i < ClassMiningStats.CurrentRoundAesRound; i++)
                    encryptedShare = ClassUtility.EncryptAesShare(encryptedShare, ClassMiningStats.CurrentBlockKey, Encoding.UTF8.GetBytes(ClassMiningStats.CurrentRoundAesKey), ClassMiningStats.CurrentRoundAesSize);

                // Static XOR Encryption -> Key from the current mining method
                encryptedShare = ClassUtility.EncryptXorShare(encryptedShare, ClassMiningStats.CurrentRoundXorKey.ToString());

                // Static AES Encryption -> Size and Key's from the current mining method.
                encryptedShare = ClassUtility.EncryptAesShare(encryptedShare, ClassMiningStats.CurrentBlockKey, Encoding.UTF8.GetBytes(ClassMiningStats.CurrentRoundAesKey), ClassMiningStats.CurrentRoundAesSize);

                // Generate SHA512 HASH for the share.
                encryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                DictionaryMiningThread[idThread]++; // Calculated only from real activity.

                return encryptedShare;
            }
            catch
            {
                return ClassAlgoErrorEnumeration.AlgoError;
            }
        }

        private static async Task DoCalculationAsync(decimal firstNumber, decimal secondNumber, string operatorSymbol, string jobType, int threadIndex)
        {
            if (firstNumber < 2 || firstNumber > ClassMiningStats.CurrentBlockDifficulty)
                return;

            if (secondNumber < 2 || secondNumber > ClassMiningStats.CurrentBlockDifficulty)
                return;

            var calculation = $"{firstNumber} {operatorSymbol} {secondNumber}";

            if (SubmittedShares.ContainsKey(calculation))
                return;

            var result = ClassUtility.ComputeCalculation(firstNumber.ToString("F0"), operatorSymbol, secondNumber.ToString("F0"));

            if (result - Math.Round(result, 0) != 0)
                return;

            if (result < 2 || result > ClassMiningStats.CurrentBlockDifficulty)
                return;

            var encryptedShare = MakeEncryptedShare(calculation, threadIndex);

            if (encryptedShare == ClassAlgoErrorEnumeration.AlgoError)
                return;

            var hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

            if (!ClassMiningStats.CurrentJobIndication.Contains(hashEncryptedShare) && hashEncryptedShare != ClassMiningStats.CurrentBlockIndication)
                return;

            if (!SubmittedShares.TryAdd(calculation, 1))
                return;

            if (ClassMiningStats.CurrentJobIndication.Contains(hashEncryptedShare))
                ClassConsole.ConsoleWriteLine($"Thread: {threadIndex + 1} | Job Type: {jobType} | Job found: {firstNumber} {operatorSymbol} {secondNumber} = {result}");

            if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                ClassConsole.ConsoleWriteLine($"Thread: {threadIndex + 1} | Job Type: {jobType} | Block found: {firstNumber} {operatorSymbol} {secondNumber} = {result}");

            var share = new JObject
            {
                { "type", ClassMiningRequest.TypeSubmit },
                { ClassMiningRequest.SubmitResult, result },
                { ClassMiningRequest.SubmitFirstNumber, firstNumber },
                { ClassMiningRequest.SubmitSecondNumber, secondNumber },
                { ClassMiningRequest.SubmitOperator, operatorSymbol },
                { ClassMiningRequest.SubmitShare, encryptedShare },
                { ClassMiningRequest.SubmitHash, hashEncryptedShare }
            };

            await ClassMiningNetwork.SendPacketToPoolAsync(share.ToString(Formatting.None)).ConfigureAwait(false);
        }
    }
}