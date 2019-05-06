
namespace Xiropht_Miner
{
    public class ClassMiningRequest
    {
        /// <summary>
        /// Types of request provided by the pool.
        /// </summary>
        public const string TypeSubmit = "submit";
        public const string TypeJob = "job";
        public const string TypeJobDifficulty = "difficulty";
        public const string TypeBlock = "block";
        public const string TypeBlockTimestampCreate = "block_timestamp_create";
        public const string TypeBlockKey = "block_key";
        public const string TypeBlockIndication = "block_indication";
        public const string TypeJobMiningMethodName = "method_name";
        public const string TypeJobMiningMethodAesRound = "aes_round";
        public const string TypeJobMiningMethodAesSize = "aes_size";
        public const string TypeJobMiningMethodAesKey = "aes_key";
        public const string TypeJobMiningMethodXorKey = "xor_key";


        public const string TypeKeepAlive = "keep-alive";

        /// <summary>
        /// Types of result provided by the pool.
        /// </summary>
        public const string TypeResult = "result";
        public const string TypeMinRange = "min_range";
        public const string TypeMaxRange = "max_range";
        public const string TypeShare = "share";
        public const string TypeResultShareOk = "ok";
        public const string TypeResultShareInvalid = "invalid share";
        public const string TypeResultShareDuplicate = "duplicate share";
        public const string TypeResultShareLowDifficulty = "low difficulty share";



        /// <summary>
        /// Submitted data from miner according to login type.
        /// </summary>
        public const string TypeLogin = "login";
        public const string TypeLoginOk = "login-ok";
        public const string TypeLoginWrong = "wrong";
        public const string SubmitWalletAddress = "walletaddress";
        public const string SubmitVersion = "version";

        /// <summary>
        /// Submitted data from miner according to submit share.
        /// </summary>
        public const string SubmitResult = "result";
        public const string SubmitFirstNumber = "firstNumber";
        public const string SubmitSecondNumber = "secondNumber";
        public const string SubmitOperator = "operator";
        public const string SubmitShare = "share";
        public const string SubmitHash = "hash";
    }
}
