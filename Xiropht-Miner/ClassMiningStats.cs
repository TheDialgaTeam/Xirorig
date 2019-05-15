namespace Xiropht_Miner
{
    public class ClassMiningStats
    {
        /// <summary>
        /// Miner stats.
        /// </summary>
        public static int TotalGoodShare;

        public static int TotalInvalidShare;
        public static int TotalDuplicateShare;
        public static int TotalLowDifficultyShare;
        public static int CurrentBlockId;
        public static string CurrentBlockTimestampCreate;
        public static string CurrentBlockKey;
        public static string CurrentBlockIndication;
        public static decimal CurrentBlockDifficulty;
        public static string CurrentJobIndication;
        public static decimal CurrentMiningDifficulty;
        public static decimal CurrentMinRangeJob;
        public static decimal CurrentMaxRangeJob;

        /// <summary>
        /// Mining method informations.
        /// </summary>
        public static string CurrentMethodName;

        public static int CurrentRoundAesRound;
        public static int CurrentRoundAesSize;
        public static string CurrentRoundAesKey;
        public static int CurrentRoundXorKey;
    }
}