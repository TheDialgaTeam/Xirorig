using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xiropht_Connector_All.Utils;

namespace Xiropht_Miner
{
    public class ClassMining
    {
        private static Thread[] ThreadMiningArray;
        public static Dictionary<int, long> DictionaryMiningThread = new Dictionary<int, long>(); // Id thread, total share.
        private static bool ThreadMiningRunning;
        private static bool EstimatedCalculationSpeed;
        public static float TotalHashrate;

        /// <summary>
        /// Proceed mining
        /// </summary>
        public static void ProceedMining()
        {
            if (!ThreadMiningRunning)
            {
                StopThreadMining();
                ThreadMiningRunning = true;
                for (int i = 0; i < ThreadMiningArray.Length; i++)
                {
                    if (i < ThreadMiningArray.Length)
                    {
                        int i1 = i + 1;
                        if (!DictionaryMiningThread.ContainsKey(i))
                        {
                            DictionaryMiningThread.Add(i, 0);
                        }
                        ThreadMiningArray[i] = new Thread(async delegate ()
                        {
                            await StartThreadMiningAsync(i1);
                        });
                        switch (ClassMiningConfig.MiningConfigThreadIntensity)
                        {
                            case 0:
                                ThreadMiningArray[i].Priority = ThreadPriority.Lowest;
                                break;
                            case 1:
                                ThreadMiningArray[i].Priority = ThreadPriority.BelowNormal;
                                break;
                            case 2:
                                ThreadMiningArray[i].Priority = ThreadPriority.Normal;
                                break;
                            case 3:
                                ThreadMiningArray[i].Priority = ThreadPriority.AboveNormal;
                                break;
                            case 4:
                                ThreadMiningArray[i].Priority = ThreadPriority.Highest;
                                break;
                        }
                        ThreadMiningArray[i].IsBackground = true;
                        ThreadMiningArray[i].Start();
                    }
                }
                if (!EstimatedCalculationSpeed)
                {
                    EstimatedCalculationSpeed = true;
                    CalculateCalculationSpeed();
                }
            }
        }

        /// <summary>
        /// Clean calculation done every one second
        /// </summary>
        private static void CalculateCalculationSpeed()
        {
            new Thread(delegate ()
            {
                while (!Program.Exit)
                {
                    float totalHashrate = 0;
                    for (int i = 0; i < DictionaryMiningThread.Count; i++)
                    {
                        if (i < DictionaryMiningThread.Count)
                        {
                            try
                            {
                                totalHashrate += DictionaryMiningThread[i];
                                DictionaryMiningThread[i] = 0;
                            }
                            catch
                            {

                            }
                        }
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
            }
            else
            {
                for (int i = 0; i < ThreadMiningArray.Length; i++)
                {
                    if (i < ThreadMiningArray.Length)
                    {
                        if (ThreadMiningArray[i] != null && (ThreadMiningArray[i].IsAlive || ThreadMiningArray[i] != null))
                        {
                            DictionaryMiningThread[i] = 0;
                            ThreadMiningArray[i].Abort();
                            GC.SuppressFinalize(ThreadMiningArray[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Start thread mining.
        /// </summary>
        private static async Task StartThreadMiningAsync(int idThread)
        {
            decimal minRange = Math.Round((ClassMiningStats.CurrentMaxRangeJob / ClassMiningConfig.MiningConfigThread) * (idThread - 1), 0);
            if (minRange <= 1)
            {
                minRange = 2;
            }
            decimal maxRange = Math.Round(((ClassMiningStats.CurrentMaxRangeJob / ClassMiningConfig.MiningConfigThread) * idThread), 0);
            ClassConsole.ConsoleWriteLine("Start mining thread id: " + idThread + " on mining job: " + ClassMiningStats.CurrentMiningDifficulty + " with range: " + minRange + "|" + maxRange, ClassConsoleEnumeration.IndexPoolConsoleBlueLog);
            var currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;

            while (ClassMiningNetwork.IsLogged && ThreadMiningRunning)
            {
                if (currentMiningJobIndication != ClassMiningStats.CurrentJobIndication)
                {
                    minRange = Math.Round((ClassMiningStats.CurrentMaxRangeJob / ClassMiningConfig.MiningConfigThread) * (idThread - 1), 0);
                    if (minRange <= 1)
                    {
                        minRange = 2;
                    }
                    maxRange = (Math.Round(((ClassMiningStats.CurrentMaxRangeJob / ClassMiningConfig.MiningConfigThread) * idThread), 0));
                    maxRange = Math.Round(maxRange, 0);
                    currentMiningJobIndication = ClassMiningStats.CurrentJobIndication;
                    ClassConsole.ConsoleWriteLine("Start mining thread id: " + idThread + " on mining job difficulty: " + ClassMiningStats.CurrentMiningDifficulty + " with range: " + minRange + "|" + maxRange, ClassConsoleEnumeration.IndexPoolConsoleBlueLog);
                }
                string firstNumber = ClassUtility.GenerateNumberMathCalculation(minRange, maxRange);
                string secondNumber = ClassUtility.GenerateNumberMathCalculation(minRange, maxRange);



                for (int i = 0; i < ClassUtility.RandomOperatorCalculation.Length; i++)
                {
                    if (i < ClassUtility.RandomOperatorCalculation.Length)
                    {
                        #region Test unreverted calculation
                        string calculation = firstNumber + " " + ClassUtility.RandomOperatorCalculation[i] + " " + secondNumber;
                        decimal calculationResult = ClassUtility.ComputeCalculation(firstNumber, ClassUtility.RandomOperatorCalculation[i], secondNumber);
                        if (calculationResult >= ClassMiningStats.CurrentMinRangeJob && calculationResult <= ClassMiningStats.CurrentMaxRangeJob)
                        {
                            if (calculationResult - Math.Round(calculationResult, 0) == 0) // Check if the result contain decimal places, if yes ignore it. 
                            {

                                string encryptedShare = MakeEncryptedShare(calculation, (idThread - 1));
                                if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                {
                                    // Generate SHA512 hash for block hash indication.
                                    string hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);
                                    if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication || hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                    {
                                        JObject share = new JObject
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

                                #region Test reverted calculation
                                calculation = secondNumber + " " + ClassUtility.RandomOperatorCalculation[i] + " " + firstNumber;
                                calculationResult = ClassUtility.ComputeCalculation(secondNumber, ClassUtility.RandomOperatorCalculation[i], firstNumber);

                                if (calculationResult >= ClassMiningStats.CurrentMinRangeJob && calculationResult <= ClassMiningStats.CurrentMaxRangeJob)
                                {
                                    if (calculationResult - Math.Round(calculationResult, 0) == 0) // Check if the result contain decimal places, if yes ignore it. 
                                    {

                                        string encryptedShare = MakeEncryptedShare(calculation, (idThread - 1));
                                        if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                        {
                                            // Generate SHA512 hash for block hash indication.
                                            string hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                                            if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication || hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                            {

                                                JObject share = new JObject
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
                                #endregion

                            }

                        }
                        else // Test reverted
                        {

                            #region Test reverted calculation
                            calculation = secondNumber + " " + ClassUtility.RandomOperatorCalculation[i] + " " + firstNumber;
                            calculationResult = ClassUtility.ComputeCalculation(secondNumber, ClassUtility.RandomOperatorCalculation[i], firstNumber);
                            if (calculationResult >= ClassMiningStats.CurrentMinRangeJob && calculationResult <= ClassMiningStats.CurrentMaxRangeJob)
                            {
                                if (calculationResult - Math.Round(calculationResult, 0) == 0) // Check if the result contain decimal places, if yes ignore it. 
                                {

                                    string encryptedShare = MakeEncryptedShare(calculation, (idThread - 1));
                                    if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                    {
                                        // Generate SHA512 hash for block hash indication.
                                        string hashEncryptedShare = ClassUtility.GenerateSHA512(encryptedShare);

                                        if (hashEncryptedShare == ClassMiningStats.CurrentJobIndication || hashEncryptedShare == ClassMiningStats.CurrentBlockIndication)
                                        {

                                            JObject share = new JObject
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
                            #endregion

                        }


                        #endregion
                    }
                }

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
                string encryptedShare = ClassUtility.StringToHexString(calculation + ClassMiningStats.CurrentBlockTimestampCreate);

                // Static XOR Encryption -> Key updated from the current mining method.
                encryptedShare = ClassUtility.EncryptXorShare(encryptedShare, ClassMiningStats.CurrentRoundXorKey.ToString());

                // Dynamic AES Encryption -> Size and Key's from the current mining method and the current block key encryption.
                for (int i = 0; i < ClassMiningStats.CurrentRoundAesRound; i++)
                {
                    encryptedShare = ClassUtility.EncryptAesShare(encryptedShare, ClassMiningStats.CurrentBlockKey, Encoding.UTF8.GetBytes(ClassMiningStats.CurrentRoundAesKey), ClassMiningStats.CurrentRoundAesSize);
                }

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
