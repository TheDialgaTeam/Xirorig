using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xiropht_Connector_All.Seed;
using Xiropht_Connector_All.Setting;

namespace Xiropht_Miner
{
    public class ClassMiningNetwork
    {
        private static TcpClient minerConnection;
        private static bool FirstConnection;
        private static Thread ThreadCheckMiningConnection;
        public static bool IsConnected;
        public static bool IsLogged;
        private static long LastPacketReceived;

        /// <summary>
        /// Start mining.
        /// </summary>
        public static async Task<bool> StartMiningAsync()
        {
            ClassConsole.ConsoleWriteLine("Attempt to connect to pool: " + ClassMiningConfig.MiningPoolHost + ":" + ClassMiningConfig.MiningPoolPort, ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
            if(!await ConnectToMiningPoolAsync())
            {
                ClassConsole.ConsoleWriteLine("Can't connect to pool: " + ClassMiningConfig.MiningPoolHost + ":" + ClassMiningConfig.MiningPoolPort + " retry in 5 seconds.", ClassConsoleEnumeration.IndexPoolConsoleRedLog);
                return false;
            }
            if (IsConnected)
            {
                LastPacketReceived = DateTimeOffset.Now.ToUnixTimeSeconds();
                if (!FirstConnection)
                {
                    FirstConnection = true;
                    ThreadCheckMiningConnection = new Thread(() => CheckMiningPoolConnectionAsync());
                    ThreadCheckMiningConnection.Start();
                }
                await Task.Factory.StartNew(() => ListenMiningPoolAsync(), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Current).ConfigureAwait(false);
            }
            return true;
        }

        /// <summary>
        /// Permit to connect on a pool.
        /// </summary>
        /// <returns></returns>
        private static async Task<bool> ConnectToMiningPoolAsync()
        {
            if (minerConnection == null)
            {
                minerConnection = new TcpClient();
            }
            else
            {
                minerConnection?.Close();
                minerConnection?.Dispose();
                minerConnection = null;
                minerConnection = new TcpClient();
            }

            try
            {
                await minerConnection.ConnectAsync(ClassMiningConfig.MiningPoolHost, ClassMiningConfig.MiningPoolPort);
                IsConnected = true;
                LastPacketReceived = DateTimeOffset.Now.ToUnixTimeSeconds();
                return true;
            }
            catch
            {
                IsConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Check connection status.
        /// </summary>
        private static async void CheckMiningPoolConnectionAsync()
        {
            while(!Program.Exit)
            {
                try
                {
                    if (minerConnection == null)
                    {
                        IsLogged = false;
                        IsConnected = false;
                        ClassConsole.ConsoleWriteLine("Miner is disconnected, retry to connect..", ClassConsoleEnumeration.IndexPoolConsoleRedLog);
                        bool status = await StartMiningAsync();
                        while(!status)
                        {
                            await Task.Delay(5000);
                            status = await StartMiningAsync();
                        }
                    }
                    else
                    {
                        if (!IsConnected || LastPacketReceived + 5 <= DateTimeOffset.Now.ToUnixTimeSeconds())
                        {
                            IsLogged = false;
                            IsConnected = false;
                            ClassConsole.ConsoleWriteLine("Miner is disconnected, retry to connect..", ClassConsoleEnumeration.IndexPoolConsoleRedLog);
                            bool status = await StartMiningAsync();
                            while (!status)
                            {
                                await Task.Delay(5000);
                                status = await StartMiningAsync();
                            }
                        }
                    }
                }
                catch
                {
                    IsConnected = false;
                }
                Thread.Sleep(1000);
            }
        }


        /// <summary>
        /// Listen mining pool packet to receive.
        /// </summary>
        private static async Task ListenMiningPoolAsync()
        { 
            if (!await SendLoginPacketToPoolAsync())
            {
                IsConnected = false;
            }
            while (IsConnected)
            {
                try
                {

                    using (var _connectorStream = new NetworkStream(minerConnection.Client))
                    {
                        byte[] bufferPacket = new byte[ClassConnectorSetting.MaxNetworkPacketSize];
                        using (var bufferedNetworkStream = new BufferedStream(_connectorStream, ClassConnectorSetting.MaxNetworkPacketSize))
                        {
                            int received = await bufferedNetworkStream.ReadAsync(bufferPacket, 0, bufferPacket.Length);

                            if (received > 0)
                            {
                                string packet = Encoding.UTF8.GetString(bufferPacket, 0, received);
                                if (packet.Contains("\n"))
                                {
                                    var splitPacket = packet.Split(new[] { "\n"}, StringSplitOptions.None);
                                    if (splitPacket.Length > 1)
                                    {
                                        foreach (var packetEach in splitPacket)
                                        {
                                            if (packetEach != null)
                                            {
                                                if (!string.IsNullOrEmpty(packetEach))
                                                {
                                                    if (packetEach.Length > 1)
                                                    {
                                                        await Task.Factory.StartNew(() => HandleMiningPoolPacket(packetEach), CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        await Task.Factory.StartNew(() => HandleMiningPoolPacket(packet.Replace("\n", "")), CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);

                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception error)
                {
#if DEBUG
                    Console.WriteLine("Error: " + error.Message);
#endif
                    break;
                }
            }
            IsConnected = false;
        }

        /// <summary>
        /// Disconnect the miner.
        /// </summary>
        public static void DisconnectMiner()
        {
            minerConnection?.Close();
            IsLogged = false;
        }

        /// <summary>
        /// Send login packet to pool
        /// </summary>
        private static async Task<bool> SendLoginPacketToPoolAsync()
        {
            JObject loginPacket = new JObject
            {
                { "type", ClassMiningRequest.TypeLogin },
                { ClassMiningRequest.SubmitWalletAddress, ClassMiningConfig.MiningWalletAddress },
                { ClassMiningRequest.SubmitVersion, Assembly.GetExecutingAssembly().GetName().Version + "R" }
            };

            string loginPacketString = loginPacket.ToString(Formatting.None);
            if (!await SendPacketToPoolAsync(loginPacketString))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Send packet to mining pool
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public static async Task<bool> SendPacketToPoolAsync(string packet)
        {
            try
            {
                using (var _connectorStream = new NetworkStream(minerConnection.Client))
                {
                    using (var bufferedNetworkStream = new BufferedStream(_connectorStream, ClassConnectorSetting.MaxNetworkPacketSize))
                    {
                        var packetByte = Encoding.UTF8.GetBytes(packet + "\n"); 
                        await bufferedNetworkStream.WriteAsync(packetByte, 0, packetByte.Length);
                        await bufferedNetworkStream.FlushAsync();
                    }
                }
            }
            catch
            {
                IsConnected = false;
                minerConnection?.Close();
                return false;
            }

            return true;
        }


        /// <summary>
        /// Handle packets received from pool.
        /// </summary>
        /// <param name="packet"></param>
        private static void HandleMiningPoolPacket(string packet)
        {
            try
            {
                var jsonPacket = JObject.Parse(packet);

                switch (jsonPacket["type"].ToString().ToLower())
                {
                    case ClassMiningRequest.TypeLogin:
                        if (jsonPacket.ContainsKey(ClassMiningRequest.TypeLoginWrong))
                        {
                            ClassConsole.ConsoleWriteLine("Wrong login/wallet address, please check your setting. Disconnect now.", ClassConsoleEnumeration.IndexPoolConsoleRedLog);
                            DisconnectMiner();
                        }
                        else
                        {
                            if (jsonPacket.ContainsKey(ClassMiningRequest.TypeLoginOk))
                            {
                                ClassConsole.ConsoleWriteLine("Login/Wallet Address accepted by the pool. Waiting job.", ClassConsoleEnumeration.IndexPoolConsoleGreenLog);
                            }
                        }
                        break;
                    case ClassMiningRequest.TypeKeepAlive:
                        LastPacketReceived = DateTimeOffset.Now.ToUnixTimeSeconds();
                        break;
                    case ClassMiningRequest.TypeJob:
                        LastPacketReceived = DateTimeOffset.Now.ToUnixTimeSeconds();
                        IsLogged = true;
                        ClassMiningStats.CurrentBlockId = int.Parse(jsonPacket[ClassMiningRequest.TypeBlock].ToString());
                        ClassMiningStats.CurrentBlockTimestampCreate = jsonPacket[ClassMiningRequest.TypeBlockTimestampCreate].ToString();
                        ClassMiningStats.CurrentBlockKey = jsonPacket[ClassMiningRequest.TypeBlockKey].ToString();
                        ClassMiningStats.CurrentBlockIndication = jsonPacket[ClassMiningRequest.TypeBlockIndication].ToString();
                        ClassMiningStats.CurrentBlockDifficulty = decimal.Parse(jsonPacket[ClassMiningRequest.TypeBlockDifficulty].ToString());
                        ClassMiningStats.CurrentJobIndication = jsonPacket[ClassMiningRequest.TypeJobIndication].ToString();
                        ClassMiningStats.CurrentMinRangeJob = decimal.Parse(jsonPacket[ClassMiningRequest.TypeMinRange].ToString());
                        ClassMiningStats.CurrentMaxRangeJob = decimal.Parse(jsonPacket[ClassMiningRequest.TypeMaxRange].ToString());
                        ClassMiningStats.CurrentMethodName = jsonPacket[ClassMiningRequest.TypeJobMiningMethodName].ToString();
                        ClassMiningStats.CurrentRoundAesRound = int.Parse(jsonPacket[ClassMiningRequest.TypeJobMiningMethodAesRound].ToString());
                        ClassMiningStats.CurrentRoundAesSize = int.Parse(jsonPacket[ClassMiningRequest.TypeJobMiningMethodAesSize].ToString());
                        ClassMiningStats.CurrentRoundAesKey = jsonPacket[ClassMiningRequest.TypeJobMiningMethodAesKey].ToString();
                        ClassMiningStats.CurrentRoundXorKey = int.Parse(jsonPacket[ClassMiningRequest.TypeJobMiningMethodXorKey].ToString());
                        if (jsonPacket.ContainsKey(ClassMiningRequest.TypeJobDifficulty))
                        {
                            ClassMiningStats.CurrentMiningDifficulty = decimal.Parse(jsonPacket[ClassMiningRequest.TypeJobDifficulty].ToString());
                        }
                        ClassMining.ProceedMining();

                        ClassConsole.ConsoleWriteLine("New Mining Job: " + ClassMiningStats.CurrentJobIndication + " | Job Difficulty: " + ClassMiningStats.CurrentMiningDifficulty + " | Block ID: " + ClassMiningStats.CurrentBlockId + " | Block Difficulty: " + ClassMiningStats.CurrentBlockDifficulty + " | Block Hash Indication: " + ClassMiningStats.CurrentBlockIndication, ClassConsoleEnumeration.IndexPoolConsoleMagentaLog);
                        ClassConsole.ConsoleWriteLine("Mining Job range received: " + ClassMiningStats.CurrentMinRangeJob+"|"+ClassMiningStats.CurrentMaxRangeJob, ClassConsoleEnumeration.IndexPoolConsoleMagentaLog);

                        break;
                    case ClassMiningRequest.TypeShare:
                        LastPacketReceived = DateTimeOffset.Now.ToUnixTimeSeconds();
                        switch (jsonPacket[ClassMiningRequest.TypeResult].ToString().ToLower())
                        {
                            case ClassMiningRequest.TypeResultShareOk:
                                ClassMiningStats.TotalGoodShare++;
                                ClassConsole.ConsoleWriteLine("Good Share ! [Total = " + ClassMiningStats.TotalGoodShare + "]", ClassConsoleEnumeration.IndexPoolConsoleGreenLog);
                                break;
                            case ClassMiningRequest.TypeResultShareInvalid:
                                ClassMiningStats.TotalInvalidShare++;
                                ClassConsole.ConsoleWriteLine("Invalid Share ! [Total = " + ClassMiningStats.TotalInvalidShare + "]", ClassConsoleEnumeration.IndexPoolConsoleRedLog);
                                break;
                            case ClassMiningRequest.TypeResultShareDuplicate:
                                ClassMiningStats.TotalDuplicateShare++;
                                ClassConsole.ConsoleWriteLine("Duplicate Share ! [Total = " + ClassMiningStats.TotalDuplicateShare + "]", ClassConsoleEnumeration.IndexPoolConsoleYellowLog);
                                break;
                            case ClassMiningRequest.TypeResultShareLowDifficulty:
                                ClassMiningStats.TotalLowDifficultyShare++;
                                ClassConsole.ConsoleWriteLine("Low Difficulty Share ! [Total = " + ClassMiningStats.TotalLowDifficultyShare + "]", ClassConsoleEnumeration.IndexPoolConsoleRedLog);
                                break;
                        }
                        break;
                }
            }
            catch
            {

            }
        }
    }
}
