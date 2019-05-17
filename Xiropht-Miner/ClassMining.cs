using System;
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
        public static Dictionary<int, long> DictionaryMiningThread = new Dictionary<int, long>();
        public static long TotalHashrate;

        private static Thread[] ThreadMiningArray;
        private static Thread[] ThreadMiningAdditionArray;
        private static Thread[] ThreadMiningSubtractionArray;
        private static Thread[] ThreadMiningMultiplicationArray;
        private static Thread[] ThreadMiningDivisionArray;
        private static Thread[] ThreadMiningModulusArray;

        private static bool ThreadMiningRunning;
        private static bool EstimatedCalculationSpeed;
        private static bool JobCompleted;

        private static int ThreadMiningAdditionIndexOffset;
        private static int ThreadMiningSubtractionIndexOffset;
        private static int ThreadMiningMultiplicationIndexOffset;
        private static int ThreadMiningDivisionIndexOffset;
        private static int ThreadMiningModulusIndexOffset;

        /// <summary>
        /// Proceed mining
        /// </summary>
        public static void ProceedMining()
        {
            void AdjustThreadPriority(Thread thread)
            {
                switch (ClassMiningConfig.MiningConfigThreadIntensity)
                {
                    case 0:
                        thread.Priority = ThreadPriority.Lowest;
                        break;

                    case 1:
                        thread.Priority = ThreadPriority.BelowNormal;
                        break;

                    case 2:
                        thread.Priority = ThreadPriority.Normal;
                        break;

                    case 3:
                        thread.Priority = ThreadPriority.AboveNormal;
                        break;

                    case 4:
                        thread.Priority = ThreadPriority.Highest;
                        break;

                    default:
                        thread.Priority = ThreadPriority.Normal;
                        break;
                }
            }

            if (ThreadMiningRunning)
                return;

            StopThreadMining();
            ThreadMiningRunning = true;

            for (var i = 0; i < ThreadMiningArray.Length; i++)
            {
                var i1 = i + 1;

                if (!DictionaryMiningThread.ContainsKey(i))
                    DictionaryMiningThread.Add(i, 0);

                ThreadMiningArray[i] = new Thread(async () => { await StartThreadMiningAsync(i1); });
                AdjustThreadPriority(ThreadMiningArray[i]);
                ThreadMiningArray[i].IsBackground = true;
                ThreadMiningArray[i].Start();
            }

            if (ClassMiningConfig.MiningConfigUseIntelligentCalculation)
            {
                for (var i = 0; i < ThreadMiningAdditionArray.Length; i++)
                {
                    var i1 = i + ThreadMiningAdditionIndexOffset + 1;

                    if (!DictionaryMiningThread.ContainsKey(i + ThreadMiningAdditionIndexOffset))
                        DictionaryMiningThread.Add(i + ThreadMiningAdditionIndexOffset, 0);

                    ThreadMiningAdditionArray[i] = new Thread(async () => { await StartThreadMiningAdditionJobAsync(i1); });
                    AdjustThreadPriority(ThreadMiningAdditionArray[i]);
                    ThreadMiningAdditionArray[i].IsBackground = true;
                    ThreadMiningAdditionArray[i].Start();
                }

                for (var i = 0; i < ThreadMiningSubtractionArray.Length; i++)
                {
                    var i1 = i + ThreadMiningSubtractionIndexOffset + 1;

                    if (!DictionaryMiningThread.ContainsKey(i + ThreadMiningSubtractionIndexOffset))
                        DictionaryMiningThread.Add(i + ThreadMiningSubtractionIndexOffset, 0);

                    ThreadMiningSubtractionArray[i] = new Thread(async () => { await StartThreadMiningSubtractionJobAsync(i1); });
                    AdjustThreadPriority(ThreadMiningSubtractionArray[i]);
                    ThreadMiningSubtractionArray[i].IsBackground = true;
                    ThreadMiningSubtractionArray[i].Start();
                }

                for (var i = 0; i < ThreadMiningMultiplicationArray.Length; i++)
                {
                    var i1 = i + ThreadMiningMultiplicationIndexOffset + 1;

                    if (!DictionaryMiningThread.ContainsKey(i + ThreadMiningMultiplicationIndexOffset))
                        DictionaryMiningThread.Add(i + ThreadMiningMultiplicationIndexOffset, 0);

                    ThreadMiningMultiplicationArray[i] = new Thread(async () => { await StartThreadMiningMultiplicationJobAsync(i1); });
                    AdjustThreadPriority(ThreadMiningMultiplicationArray[i]);
                    ThreadMiningMultiplicationArray[i].IsBackground = true;
                    ThreadMiningMultiplicationArray[i].Start();
                }

                for (var i = 0; i < ThreadMiningDivisionArray.Length; i++)
                {
                    var i1 = i + ThreadMiningDivisionIndexOffset + 1;

                    if (!DictionaryMiningThread.ContainsKey(i + ThreadMiningDivisionIndexOffset))
                        DictionaryMiningThread.Add(i + ThreadMiningDivisionIndexOffset, 0);

                    ThreadMiningDivisionArray[i] = new Thread(async () => { await StartThreadMiningDivisionJobAsync(i1); });
                    AdjustThreadPriority(ThreadMiningDivisionArray[i]);
                    ThreadMiningDivisionArray[i].IsBackground = true;
                    ThreadMiningDivisionArray[i].Start();
                }

                for (var i = 0; i < ThreadMiningModulusArray.Length; i++)
                {
                    var i1 = i + ThreadMiningModulusIndexOffset + 1;

                    if (!DictionaryMiningThread.ContainsKey(i + ThreadMiningModulusIndexOffset))
                        DictionaryMiningThread.Add(i + ThreadMiningModulusIndexOffset, 0);

                    ThreadMiningModulusArray[i] = new Thread(async () => { await StartThreadMiningModulusJobAsync(i1); });
                    AdjustThreadPriority(ThreadMiningModulusArray[i]);
                    ThreadMiningModulusArray[i].IsBackground = true;
                    ThreadMiningModulusArray[i].Start();
                }
            }

            if (EstimatedCalculationSpeed)
                return;

            EstimatedCalculationSpeed = true;
            CalculateCalculationSpeed();
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
        private static void StopThreadMining()
        {
            if (ThreadMiningArray == null)
            {
                ThreadMiningArray = new Thread[ClassMiningConfig.MiningConfigThread];

                if (!ClassMiningConfig.MiningConfigUseIntelligentCalculation)
                    return;

                ThreadMiningAdditionIndexOffset = ThreadMiningArray.Length;
                ThreadMiningAdditionArray = new Thread[ClassMiningConfig.MiningConfigAdditionJobThread];

                ThreadMiningSubtractionIndexOffset = ThreadMiningAdditionIndexOffset + ThreadMiningAdditionArray.Length;
                ThreadMiningSubtractionArray = new Thread[ClassMiningConfig.MiningConfigSubtractionJobThread];

                ThreadMiningMultiplicationIndexOffset = ThreadMiningSubtractionIndexOffset + ThreadMiningSubtractionArray.Length;
                ThreadMiningMultiplicationArray = new Thread[ClassMiningConfig.MiningConfigMultiplicationJobThread];

                ThreadMiningDivisionIndexOffset = ThreadMiningMultiplicationIndexOffset + ThreadMiningMultiplicationArray.Length;
                ThreadMiningDivisionArray = new Thread[ClassMiningConfig.MiningConfigDivisionJobThread];

                ThreadMiningModulusIndexOffset = ThreadMiningDivisionIndexOffset + ThreadMiningDivisionArray.Length;
                ThreadMiningModulusArray = new Thread[ClassMiningConfig.MiningConfigModulusJobThread];
            }
            else
            {
                for (var i = 0; i < ThreadMiningArray.Length; i++)
                {
                    if (ThreadMiningArray[i] == null || !ThreadMiningArray[i].IsAlive && ThreadMiningArray[i] == null)
                        continue;

                    DictionaryMiningThread[i] = 0;
                    ThreadMiningArray[i].Abort();
                    GC.SuppressFinalize(ThreadMiningArray[i]);
                }

                if (!ClassMiningConfig.MiningConfigUseIntelligentCalculation)
                    return;

                for (var i = 0; i < ThreadMiningAdditionArray.Length; i++)
                {
                    if (ThreadMiningAdditionArray[i] == null || !ThreadMiningAdditionArray[i].IsAlive && ThreadMiningAdditionArray[i] == null)
                        continue;

                    DictionaryMiningThread[i + ThreadMiningAdditionIndexOffset] = 0;
                    ThreadMiningAdditionArray[i].Abort();
                    GC.SuppressFinalize(ThreadMiningAdditionArray[i]);
                }

                for (var i = 0; i < ThreadMiningSubtractionArray.Length; i++)
                {
                    if (ThreadMiningSubtractionArray[i] == null || !ThreadMiningSubtractionArray[i].IsAlive && ThreadMiningSubtractionArray[i] == null)
                        continue;

                    DictionaryMiningThread[i + ThreadMiningSubtractionIndexOffset] = 0;
                    ThreadMiningSubtractionArray[i].Abort();
                    GC.SuppressFinalize(ThreadMiningSubtractionArray[i]);
                }

                for (var i = 0; i < ThreadMiningMultiplicationArray.Length; i++)
                {
                    if (ThreadMiningMultiplicationArray[i] == null || !ThreadMiningMultiplicationArray[i].IsAlive && ThreadMiningMultiplicationArray[i] == null)
                        continue;

                    DictionaryMiningThread[i + ThreadMiningMultiplicationIndexOffset] = 0;
                    ThreadMiningMultiplicationArray[i].Abort();
                    GC.SuppressFinalize(ThreadMiningMultiplicationArray[i]);
                }

                for (var i = 0; i < ThreadMiningDivisionArray.Length; i++)
                {
                    if (ThreadMiningDivisionArray[i] == null || !ThreadMiningDivisionArray[i].IsAlive && ThreadMiningDivisionArray[i] == null)
                        continue;

                    DictionaryMiningThread[i + ThreadMiningDivisionIndexOffset] = 0;
                    ThreadMiningDivisionArray[i].Abort();
                    GC.SuppressFinalize(ThreadMiningDivisionArray[i]);
                }

                for (var i = 0; i < ThreadMiningModulusArray.Length; i++)
                {
                    if (ThreadMiningModulusArray[i] == null || !ThreadMiningModulusArray[i].IsAlive && ThreadMiningModulusArray[i] == null)
                        continue;

                    DictionaryMiningThread[i + ThreadMiningModulusIndexOffset] = 0;
                    ThreadMiningModulusArray[i].Abort();
                    GC.SuppressFinalize(ThreadMiningModulusArray[i]);
                }
            }
        }

        private static async Task StartThreadMiningAdditionJobAsync(int idThread)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var (startRange, endRange) = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentMaxRangeJob - ClassMiningStats.CurrentMinRangeJob + 1, ClassMiningConfig.MiningConfigAdditionJobThread, idThread - ThreadMiningAdditionIndexOffset - 1);

                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Addition | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var result = startRange; result <= endRange; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    if (JobCompleted)
                        break;

                    foreach (var (firstNumber, secondNumber) in ClassMiningUtilities.SumOf(result))
                    {
                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (JobCompleted)
                            break;

                        if (firstNumber < ClassMiningStats.CurrentMinRangeJob || firstNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        if (secondNumber < ClassMiningStats.CurrentMinRangeJob || secondNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        var calculation = $"{firstNumber} + {secondNumber}";

                        var encryptedShare = MakeEncryptedShare(calculation, idThread - 1);

                        if (encryptedShare == ClassAlgoErrorEnumeration.AlgoError)
                            continue;

                        var hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                        if (hashEncryptedShare != ClassMiningStats.CurrentJobIndication && hashEncryptedShare != ClassMiningStats.CurrentBlockIndication)
                            continue;

                        if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication)
                            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Addition | Job found: {firstNumber} + {secondNumber} = {result}");

                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Addition | Block found: {firstNumber} + {secondNumber} = {result}");

                        var share = new JObject
                        {
                            { "type", ClassMiningRequest.TypeSubmit },
                            { ClassMiningRequest.SubmitResult, result },
                            { ClassMiningRequest.SubmitFirstNumber, firstNumber },
                            { ClassMiningRequest.SubmitSecondNumber, secondNumber },
                            { ClassMiningRequest.SubmitOperator, "+" },
                            { ClassMiningRequest.SubmitShare, encryptedShare },
                            { ClassMiningRequest.SubmitHash, hashEncryptedShare }
                        };

                        JobCompleted = await ClassMiningNetwork.SendPacketToPoolAsync(share.ToString(Formatting.None)).ConfigureAwait(false);

                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                        {
                            JobCompleted = false;
                            continue;
                        }

                        break;
                    }

                    if (JobCompleted)
                        break;
                }

                await Task.Run(async () =>
                {
                    while (currentMiningJobIndication == ClassMiningStats.CurrentJobIndication)
                        await Task.Delay(1000).ConfigureAwait(false);

                    JobCompleted = false;
                }).ConfigureAwait(false);
            }
        }

        private static async Task StartThreadMiningSubtractionJobAsync(int idThread)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var (startRange, endRange) = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentMaxRangeJob - ClassMiningStats.CurrentMinRangeJob + 1, ClassMiningConfig.MiningConfigSubtractionJobThread, idThread - ThreadMiningSubtractionIndexOffset - 1);

                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Subtraction | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var result = startRange; result <= endRange; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    if (JobCompleted)
                        break;

                    foreach (var (firstNumber, secondNumber) in ClassMiningUtilities.SubtractOf(result))
                    {
                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (JobCompleted)
                            break;

                        if (firstNumber < ClassMiningStats.CurrentMinRangeJob || firstNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        if (secondNumber < ClassMiningStats.CurrentMinRangeJob || secondNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        var calculation = $"{firstNumber} - {secondNumber}";

                        var encryptedShare = MakeEncryptedShare(calculation, idThread - 1);

                        if (encryptedShare == ClassAlgoErrorEnumeration.AlgoError)
                            continue;

                        var hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                        if (hashEncryptedShare != ClassMiningStats.CurrentJobIndication && hashEncryptedShare != ClassMiningStats.CurrentBlockIndication)
                            continue;

                        if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication)
                            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Subtraction | Job found: {firstNumber} - {secondNumber} = {result}");

                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Subtraction | Block found: {firstNumber} - {secondNumber} = {result}");

                        var share = new JObject
                        {
                            { "type", ClassMiningRequest.TypeSubmit },
                            { ClassMiningRequest.SubmitResult, result },
                            { ClassMiningRequest.SubmitFirstNumber, firstNumber },
                            { ClassMiningRequest.SubmitSecondNumber, secondNumber },
                            { ClassMiningRequest.SubmitOperator, "-" },
                            { ClassMiningRequest.SubmitShare, encryptedShare },
                            { ClassMiningRequest.SubmitHash, hashEncryptedShare }
                        };

                        JobCompleted = await ClassMiningNetwork.SendPacketToPoolAsync(share.ToString(Formatting.None)).ConfigureAwait(false);

                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                        {
                            JobCompleted = false;
                            continue;
                        }

                        break;
                    }

                    if (JobCompleted)
                        break;
                }

                await Task.Run(async () =>
                {
                    while (currentMiningJobIndication == ClassMiningStats.CurrentJobIndication)
                        await Task.Delay(1000).ConfigureAwait(false);

                    JobCompleted = false;
                }).ConfigureAwait(false);
            }
        }

        private static async Task StartThreadMiningMultiplicationJobAsync(int idThread)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var (startRange, endRange) = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentMaxRangeJob - ClassMiningStats.CurrentMinRangeJob + 1, ClassMiningConfig.MiningConfigMultiplicationJobThread, idThread - ThreadMiningMultiplicationIndexOffset - 1);

                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Multiplication | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var result = startRange; result <= endRange; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    if (JobCompleted)
                        break;

                    if (ClassMiningUtilities.IsPrimeNumber(result))
                        continue;

                    foreach (var (firstNumber, secondNumber) in ClassMiningUtilities.FactorOf(result))
                    {
                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (JobCompleted)
                            break;

                        if (firstNumber < ClassMiningStats.CurrentMinRangeJob || firstNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        if (secondNumber < ClassMiningStats.CurrentMinRangeJob || secondNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        var calculation = $"{firstNumber} * {secondNumber}";

                        var encryptedShare = MakeEncryptedShare(calculation, idThread - 1);

                        if (encryptedShare == ClassAlgoErrorEnumeration.AlgoError)
                            continue;

                        var hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                        if (hashEncryptedShare != ClassMiningStats.CurrentJobIndication && hashEncryptedShare != ClassMiningStats.CurrentBlockIndication)
                            continue;

                        if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication)
                            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Multiplication | Job found: {firstNumber} * {secondNumber} = {result}");

                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Multiplication | Block found: {firstNumber} * {secondNumber} = {result}");

                        var share = new JObject
                        {
                            { "type", ClassMiningRequest.TypeSubmit },
                            { ClassMiningRequest.SubmitResult, result },
                            { ClassMiningRequest.SubmitFirstNumber, firstNumber },
                            { ClassMiningRequest.SubmitSecondNumber, secondNumber },
                            { ClassMiningRequest.SubmitOperator, "*" },
                            { ClassMiningRequest.SubmitShare, encryptedShare },
                            { ClassMiningRequest.SubmitHash, hashEncryptedShare }
                        };

                        JobCompleted = await ClassMiningNetwork.SendPacketToPoolAsync(share.ToString(Formatting.None)).ConfigureAwait(false);

                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                        {
                            JobCompleted = false;
                            continue;
                        }

                        break;
                    }

                    if (JobCompleted)
                        break;
                }

                await Task.Run(async () =>
                {
                    while (currentMiningJobIndication == ClassMiningStats.CurrentJobIndication)
                        await Task.Delay(1000).ConfigureAwait(false);

                    JobCompleted = false;
                }).ConfigureAwait(false);
            }
        }

        private static async Task StartThreadMiningDivisionJobAsync(int idThread)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var (startRange, endRange) = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentMaxRangeJob - ClassMiningStats.CurrentMinRangeJob + 1, ClassMiningConfig.MiningConfigDivisionJobThread, idThread - ThreadMiningDivisionIndexOffset - 1);

                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Division | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var result = ClassMiningStats.CurrentMinRangeJob; result <= ClassMiningStats.CurrentMaxRangeJob; result++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    if (JobCompleted)
                        break;

                    if (ClassMiningUtilities.IsPrimeNumber(result))
                        continue;

                    foreach (var (firstNumber, secondNumber) in ClassMiningUtilities.DivisorOf(result))
                    {
                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (JobCompleted)
                            break;

                        if (firstNumber < ClassMiningStats.CurrentMinRangeJob || firstNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        if (secondNumber < ClassMiningStats.CurrentMinRangeJob || secondNumber > ClassMiningStats.CurrentMaxRangeJob)
                            continue;

                        var calculation = $"{firstNumber} / {secondNumber}";

                        var encryptedShare = MakeEncryptedShare(calculation, idThread - 1);

                        if (encryptedShare == ClassAlgoErrorEnumeration.AlgoError)
                            continue;

                        var hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                        if (hashEncryptedShare != ClassMiningStats.CurrentJobIndication && hashEncryptedShare != ClassMiningStats.CurrentBlockIndication)
                            continue;

                        if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication)
                            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Division | Job found: {firstNumber} / {secondNumber} = {ClassMiningStats.CurrentMiningDifficulty}");

                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Division | Block found: {firstNumber} / {secondNumber} = {ClassMiningStats.CurrentMiningDifficulty}");

                        var share = new JObject
                        {
                            { "type", ClassMiningRequest.TypeSubmit },
                            { ClassMiningRequest.SubmitResult, result },
                            { ClassMiningRequest.SubmitFirstNumber, firstNumber },
                            { ClassMiningRequest.SubmitSecondNumber, secondNumber },
                            { ClassMiningRequest.SubmitOperator, "/" },
                            { ClassMiningRequest.SubmitShare, encryptedShare },
                            { ClassMiningRequest.SubmitHash, hashEncryptedShare }
                        };

                        JobCompleted = await ClassMiningNetwork.SendPacketToPoolAsync(share.ToString(Formatting.None)).ConfigureAwait(false);

                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                        {
                            JobCompleted = false;
                            continue;
                        }

                        break;
                    }

                    if (JobCompleted)
                        break;
                }

                await Task.Run(async () =>
                {
                    while (currentMiningJobIndication == ClassMiningStats.CurrentJobIndication)
                        await Task.Delay(1000).ConfigureAwait(false);

                    JobCompleted = false;
                }).ConfigureAwait(false);
            }
        }

        private static async Task StartThreadMiningModulusJobAsync(int idThread)
        {
            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                var (startRange, endRange) = ClassMiningUtilities.GetJob(ClassMiningStats.CurrentMaxRangeJob - ClassMiningStats.CurrentMinRangeJob + 1, ClassMiningConfig.MiningConfigModulusJobThread, idThread - ThreadMiningModulusIndexOffset - 1);

                var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Modulus | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {startRange}-{endRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);

                for (var firstNumber = startRange; firstNumber <= endRange; firstNumber++)
                {
                    if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                        break;

                    if (JobCompleted)
                        break;

                    for (var secondNumber = firstNumber + 1; secondNumber <= ClassMiningStats.CurrentMaxRangeJob; secondNumber++)
                    {
                        if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                            break;

                        if (JobCompleted)
                            break;

                        var calculation = $"{firstNumber} % {secondNumber}";

                        var encryptedShare = MakeEncryptedShare(calculation, idThread - 1);

                        if (encryptedShare == ClassAlgoErrorEnumeration.AlgoError)
                            continue;

                        var hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                        if (hashEncryptedShare != ClassMiningStats.CurrentJobIndication && hashEncryptedShare != ClassMiningStats.CurrentBlockIndication)
                            continue;

                        if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication)
                            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Modulus | Job found: {firstNumber} % {secondNumber} = {firstNumber}");

                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Modulus | Block found: {firstNumber} % {secondNumber} = {firstNumber}");

                        var share = new JObject
                        {
                            { "type", ClassMiningRequest.TypeSubmit },
                            { ClassMiningRequest.SubmitResult, firstNumber },
                            { ClassMiningRequest.SubmitFirstNumber, firstNumber },
                            { ClassMiningRequest.SubmitSecondNumber, secondNumber },
                            { ClassMiningRequest.SubmitOperator, "%" },
                            { ClassMiningRequest.SubmitShare, encryptedShare },
                            { ClassMiningRequest.SubmitHash, hashEncryptedShare }
                        };

                        JobCompleted = await ClassMiningNetwork.SendPacketToPoolAsync(share.ToString(Formatting.None)).ConfigureAwait(false);

                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                        {
                            JobCompleted = false;
                            continue;
                        }

                        break;
                    }

                    if (JobCompleted)
                        break;
                }

                await Task.Run(async () =>
                {
                    while (currentMiningJobIndication == ClassMiningStats.CurrentJobIndication)
                        await Task.Delay(1000).ConfigureAwait(false);

                    JobCompleted = false;
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Start thread mining.
        /// </summary>
        private static async Task StartThreadMiningAsync(int idThread)
        {
            var minRange = Math.Round(ClassMiningStats.CurrentMaxRangeJob / ClassMiningConfig.MiningConfigThread * (idThread - 1), 0);
            if (minRange <= 1)
                minRange = 2;
            var maxRange = Math.Round(ClassMiningStats.CurrentMaxRangeJob / ClassMiningConfig.MiningConfigThread * idThread, 0);
            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Any | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {minRange}-{maxRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);
            var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;

            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                {
                    minRange = Math.Round(ClassMiningStats.CurrentMaxRangeJob / ClassMiningConfig.MiningConfigThread * (idThread - 1), 0);
                    if (minRange <= 1)
                        minRange = 2;
                    maxRange = Math.Round(ClassMiningStats.CurrentMaxRangeJob / ClassMiningConfig.MiningConfigThread * idThread, 0);
                    currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                    ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Any | Job Difficulty: {ClassMiningStats.CurrentMiningDifficulty} | Job Range: {minRange}-{maxRange}", ClassConsoleEnumeration.IndexPoolConsoleBlueLog);
                }

                var firstNumber = ClassUtility.GenerateNumberMathCalculation(minRange, maxRange);
                var secondNumber = ClassUtility.GenerateNumberMathCalculation(minRange, maxRange);

                for (var i = 0; i < ClassUtility.RandomOperatorCalculation.Length; i++)
                {
                    if (ClassMiningConfig.MiningConfigUseIntelligentCalculation)
                    {
                        if (i == 1 || i == 4)
                            continue;
                    }

                    if (i < ClassUtility.RandomOperatorCalculation.Length)
                    {
                        var calculation = firstNumber + " " + ClassUtility.RandomOperatorCalculation[i] + " " + secondNumber;
                        var calculationResult = ClassUtility.ComputeCalculation(firstNumber, ClassUtility.RandomOperatorCalculation[i], secondNumber);

                        if (calculationResult >= ClassMiningStats.CurrentMinRangeJob && calculationResult <= ClassMiningStats.CurrentMaxRangeJob)
                        {
                            if (calculationResult - Math.Round(calculationResult, 0) == 0) // Check if the result contain decimal places, if yes ignore it. 
                            {
                                var encryptedShare = MakeEncryptedShare(calculation, idThread - 1);

                                if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                {
                                    // Generate SHA512 hash for block hash indication.
                                    var hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                                    if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication || hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                    {
                                        if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication)
                                            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Any | Job found: {firstNumber} {ClassUtility.RandomOperatorCalculation[i]} {secondNumber} = {calculationResult}");

                                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                            ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Any | Block found: {firstNumber} {ClassUtility.RandomOperatorCalculation[i]} {secondNumber} = {calculationResult}");

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
                                        
                                        JobCompleted = await ClassMiningNetwork.SendPacketToPoolAsync(share.ToString(Formatting.None)).ConfigureAwait(false);

                                        if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                            JobCompleted = false;
                                    }
                                }
                            }
                            else // Test reverted
                            {
                                calculation = secondNumber + " " + ClassUtility.RandomOperatorCalculation[i] + " " + firstNumber;
                                calculationResult = ClassUtility.ComputeCalculation(secondNumber, ClassUtility.RandomOperatorCalculation[i], firstNumber);

                                if (calculationResult - Math.Round(calculationResult, 0) == 0) // Check if the result contain decimal places, if yes ignore it. 
                                {
                                    if (calculationResult >= ClassMiningStats.CurrentMinRangeJob && calculationResult <= ClassMiningStats.CurrentMaxRangeJob)
                                    {
                                        var encryptedShare = MakeEncryptedShare(calculation, idThread - 1);

                                        if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                        {
                                            // Generate SHA512 hash for block hash indication.
                                            var hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                                            if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication || hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                            {
                                                if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication)
                                                    ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Any | Job found: {secondNumber} {ClassUtility.RandomOperatorCalculation[i]} {firstNumber} = {calculationResult}");

                                                if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                                    ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Any | Block found: {secondNumber} {ClassUtility.RandomOperatorCalculation[i]} {firstNumber} = {calculationResult}");

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

                                                JobCompleted = await ClassMiningNetwork.SendPacketToPoolAsync(share.ToString(Formatting.None)).ConfigureAwait(false);

                                                if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                                    JobCompleted = false;
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
                                if (calculationResult >= ClassMiningStats.CurrentMinRangeJob && calculationResult <= ClassMiningStats.CurrentMaxRangeJob)
                                {
                                    var encryptedShare = MakeEncryptedShare(calculation, idThread - 1);

                                    if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                    {
                                        // Generate SHA512 hash for block hash indication.
                                        var hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                                        if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication || hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                        {
                                            if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication)
                                                ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Any | Job found: {secondNumber} {ClassUtility.RandomOperatorCalculation[i]} {firstNumber} = {calculationResult}");

                                            if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                                ClassConsole.ConsoleWriteLine($"Thread: {idThread} | Job Type: Any | Block found: {secondNumber} {ClassUtility.RandomOperatorCalculation[i]} {firstNumber} = {calculationResult}");

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

                                            JobCompleted = await ClassMiningNetwork.SendPacketToPoolAsync(share.ToString(Formatting.None)).ConfigureAwait(false);

                                            if (hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                                JobCompleted = false;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (JobCompleted)
                        break;
                }

                if (!JobCompleted)
                    continue;
                
                var indication = currentMiningJobIndication;

                await Task.Run(async () =>
                {
                    while (indication == ClassMiningStats.CurrentJobIndication)
                        await Task.Delay(1000).ConfigureAwait(false);

                    JobCompleted = false;
                }).ConfigureAwait(false);
            }

            ThreadMiningRunning = false;
        }

        /// <summary>
        /// Encrypt math calculation with the current mining method
        /// </summary>
        /// <param name="calculation"></param>
        /// <returns></returns>
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
    }
}