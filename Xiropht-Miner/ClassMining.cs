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
        public static long TotalHashrate;

        private static bool ThreadMiningRunning;

        private static bool EstimatedCalculationSpeed;

        public static ConcurrentDictionary<int, long> DictionaryMiningThread { get; } = new ConcurrentDictionary<int, long>();

        public static ConcurrentDictionary<string, int> SubmittedShares { get; } = new ConcurrentDictionary<string, int>();

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
                var (startRange, endRange) = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentBlockDifficulty - 3, ClassMiningConfig.MiningConfigAdditionJobThread, threadId - AdditionJobIndexOffset - 1, 4);

                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Addition | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var result = startRange; result <= endRange; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    foreach (var (firstNumber, secondNumber) in ClassMiningUtilities.SumOf(result))
                    {
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

                    foreach (var (firstNumber, secondNumber) in ClassMiningUtilities.SumOf(result))
                    {
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
        }

        private static async Task StartSubtractionJobAsync(int threadId)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var (startRange, endRange) = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentBlockDifficulty - 1, ClassMiningConfig.MiningConfigSubtractionJobThread, threadId - SubtractionJobIndexOffset - 1, 2);

                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Subtraction | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var result = startRange; result <= endRange; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    foreach (var (firstNumber, secondNumber) in ClassMiningUtilities.SubtractOf(result))
                    {
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

                    foreach (var (firstNumber, secondNumber) in ClassMiningUtilities.SubtractOf(result))
                    {
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
        }

        private static async Task StartMultiplicationJobAsync(int threadId)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var (startRange, endRange) = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentBlockDifficulty - 3, ClassMiningConfig.MiningConfigMultiplicationJobThread, threadId - MultiplicationJobIndexOffset - 1, 4);

                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Multiplication | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var result = startRange; result <= endRange; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    if (ClassMiningUtilities.IsPrimeNumber(result))
                        continue;

                    foreach (var (firstNumber, secondNumber) in ClassMiningUtilities.FactorOf(result))
                    {
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

                    foreach (var (firstNumber, secondNumber) in ClassMiningUtilities.FactorOf(result))
                    {
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
        }

        private static async Task StartDivisionJobAsync(int threadId)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var (startRange, endRange) = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentBlockDifficulty - 1, ClassMiningConfig.MiningConfigDivisionJobThread, threadId - DivisionJobIndexOffset - 1, 2);

                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Division | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var result = ClassMiningStats.CurrentMinRangeJob; result <= ClassMiningStats.CurrentMaxRangeJob; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    if (ClassMiningUtilities.IsPrimeNumber(result))
                        continue;

                    foreach (var (firstNumber, secondNumber) in ClassMiningUtilities.DivisorOf(result))
                    {
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

                    foreach (var (firstNumber, secondNumber) in ClassMiningUtilities.DivisorOf(result))
                    {
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
        }

        private static async Task StartModulusJobAsync(int threadId)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var (startRange, endRange) = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentBlockDifficulty - 1, ClassMiningConfig.MiningConfigModulusJobThread, threadId - ModulusJobIndexOffset - 1, 2);

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

                while (currentMiningJobIndication == ClassMiningStats.CurrentJobIndication)
                    await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        private static async Task StartRandomJobAsync(int threadId)
        {
            var minRange = Math.Round(ClassMiningStats.CurrentMaxRangeJob / ClassMiningConfig.MiningConfigThread * (threadId - 1), 0);

            if (minRange <= 1)
                minRange = 2;

            var maxRange = Math.Round(ClassMiningStats.CurrentMaxRangeJob / ClassMiningConfig.MiningConfigThread * threadId, 0);
            ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Random | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {minRange}-{maxRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);
            var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;

            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                {
                    minRange = Math.Round(ClassMiningStats.CurrentMaxRangeJob / ClassMiningConfig.MiningConfigThread * (threadId - 1), 0);
                    if (minRange <= 1)
                        minRange = 2;
                    maxRange = Math.Round(ClassMiningStats.CurrentMaxRangeJob / ClassMiningConfig.MiningConfigThread * threadId, 0);
                    currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                    ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Random | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {minRange}-{maxRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);
                }

                var firstNumber = ClassUtility.GenerateNumberMathCalculation(minRange, maxRange);
                var secondNumber = ClassUtility.GenerateNumberMathCalculation(minRange, maxRange);

                for (var i = 0; i < ClassUtility.RandomOperatorCalculation.Length; i++)
                {
                    var calculation = firstNumber + " " + ClassUtility.RandomOperatorCalculation[i] + " " + secondNumber;
                    var calculationResult = ClassUtility.ComputeCalculation(firstNumber, ClassUtility.RandomOperatorCalculation[i], secondNumber);

                    if (calculationResult >= ClassMiningStats.CurrentMinRangeJob && calculationResult <= ClassMiningStats.CurrentBlockDifficulty)
                    {
                        if (calculationResult - Math.Round(calculationResult, 0) == 0) // Check if the result contain decimal places, if yes ignore it. 
                        {
                            var encryptedShare = MakeEncryptedShare(calculation, threadId - 1);

                            if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                            {
                                // Generate SHA512 hash for block hash indication.
                                var hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                                if (ClassMiningStats.CurrentJobIndication.Contains(hashEncryptedShare) || hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                {
                                    if (SubmittedShares.ContainsKey(calculation))
                                        continue;

                                    if (!SubmittedShares.TryAdd(calculation, 1))
                                        continue;

                                    if (ClassMiningStats.CurrentJobIndication.Contains(hashEncryptedShare))
                                        ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Random | Job found: {firstNumber} {ClassUtility.RandomOperatorCalculation[i]} {secondNumber} = {calculationResult}");

                                    if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                        ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Random | Block found: {firstNumber} {ClassUtility.RandomOperatorCalculation[i]} {secondNumber} = {calculationResult}");

                                    var share = new JObject
                                    {
                                        { "type", ClassMiningRequest.TypeSubmit },
                                        { ClassMiningRequest.SubmitResult, calculationResult },
                                        { ClassMiningRequest.SubmitFirstNumber, firstNumber },
                                        { ClassMiningRequest.SubmitSecondNumber, secondNumber },
                                        { ClassMiningRequest.SubmitOperator, ClassUtility.RandomOperatorCalculation[i] },
                                        { ClassMiningRequest.SubmitShare, encryptedShare },
                                        { ClassMiningRequest.SubmitHash, hashEncryptedShare }
                                    };

                                    await ClassMiningNetwork.SendPacketToPoolAsync(share.ToString(Formatting.None)).ConfigureAwait(false);
                                }
                            }
                        }
                        else // Test reverted
                        {
                            calculation = secondNumber + " " + ClassUtility.RandomOperatorCalculation[i] + " " + firstNumber;
                            calculationResult = ClassUtility.ComputeCalculation(secondNumber, ClassUtility.RandomOperatorCalculation[i], firstNumber);

                            if (calculationResult - Math.Round(calculationResult, 0) == 0) // Check if the result contain decimal places, if yes ignore it. 
                            {
                                if (calculationResult >= ClassMiningStats.CurrentMinRangeJob && calculationResult <= ClassMiningStats.CurrentBlockDifficulty)
                                {
                                    var encryptedShare = MakeEncryptedShare(calculation, threadId - 1);

                                    if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                    {
                                        // Generate SHA512 hash for block hash indication.
                                        var hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                                        if (ClassMiningStats.CurrentJobIndication.Contains(hashEncryptedShare) || hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                        {
                                            if (SubmittedShares.ContainsKey(calculation))
                                                continue;

                                            if (!SubmittedShares.TryAdd(calculation, 1))
                                                continue;

                                            if (ClassMiningStats.CurrentJobIndication.Contains(hashEncryptedShare))
                                                ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Random | Job found: {secondNumber} {ClassUtility.RandomOperatorCalculation[i]} {firstNumber} = {calculationResult}");

                                            if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                                ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Random | Block found: {secondNumber} {ClassUtility.RandomOperatorCalculation[i]} {firstNumber} = {calculationResult}");

                                            var share = new JObject
                                            {
                                                { "type", ClassMiningRequest.TypeSubmit },
                                                { ClassMiningRequest.SubmitResult, calculationResult },
                                                { ClassMiningRequest.SubmitFirstNumber, secondNumber },
                                                { ClassMiningRequest.SubmitSecondNumber, firstNumber },
                                                { ClassMiningRequest.SubmitOperator, ClassUtility.RandomOperatorCalculation[i] },
                                                { ClassMiningRequest.SubmitShare, encryptedShare },
                                                { ClassMiningRequest.SubmitHash, hashEncryptedShare }
                                            };

                                            await ClassMiningNetwork.SendPacketToPoolAsync(share.ToString(Formatting.None)).ConfigureAwait(false);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else // Test reverted
                    {
                        calculation = secondNumber + " " + ClassUtility.RandomOperatorCalculation[i] + " " + firstNumber;
                        calculationResult = ClassUtility.ComputeCalculation(secondNumber, ClassUtility.RandomOperatorCalculation[i], firstNumber);

                        if (calculationResult - Math.Round(calculationResult, 0) == 0) // Check if the result contain decimal places, if yes ignore it. 
                        {
                            if (calculationResult >= ClassMiningStats.CurrentMinRangeJob && calculationResult <= ClassMiningStats.CurrentBlockDifficulty)
                            {
                                var encryptedShare = MakeEncryptedShare(calculation, threadId - 1);

                                if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                {
                                    // Generate SHA512 hash for block hash indication.
                                    var hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                                    if (ClassMiningStats.CurrentJobIndication.Contains(hashEncryptedShare) || hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                    {
                                        if (SubmittedShares.ContainsKey(calculation))
                                            continue;

                                        if (!SubmittedShares.TryAdd(calculation, 1))
                                            continue;

                                        if (ClassMiningStats.CurrentJobIndication.Contains(hashEncryptedShare))
                                            ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Random | Job found: {secondNumber} {ClassUtility.RandomOperatorCalculation[i]} {firstNumber} = {calculationResult}");

                                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                            ClassConsole.ConsoleWriteLine($"Thread: {threadId} | Job Type: Random | Block found: {secondNumber} {ClassUtility.RandomOperatorCalculation[i]} {firstNumber} = {calculationResult}");

                                        var share = new JObject
                                        {
                                            { "type", ClassMiningRequest.TypeSubmit },
                                            { ClassMiningRequest.SubmitResult, calculationResult },
                                            { ClassMiningRequest.SubmitFirstNumber, secondNumber },
                                            { ClassMiningRequest.SubmitSecondNumber, firstNumber },
                                            { ClassMiningRequest.SubmitOperator, ClassUtility.RandomOperatorCalculation[i] },
                                            { ClassMiningRequest.SubmitShare, encryptedShare },
                                            { ClassMiningRequest.SubmitHash, hashEncryptedShare }
                                        };

                                        await ClassMiningNetwork.SendPacketToPoolAsync(share.ToString(Formatting.None)).ConfigureAwait(false);
                                    }
                                }
                            }
                        }
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