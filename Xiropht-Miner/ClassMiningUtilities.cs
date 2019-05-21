using System;
using System.Collections.Generic;
using System.Linq;

namespace Xiropht_Miner
{
    public static class ClassMiningUtilities
    {
        public static IEnumerable<decimal> DivideEvenly(decimal totalPossibilities, int totalThread)
        {
            var div = Math.Truncate(totalPossibilities / totalThread);
            var remainder = totalPossibilities % totalThread;

            for (var i = 0; i < totalThread; i++)
                yield return i < remainder ? div + 1 : div;
        }

        public static decimal SquareRoot(decimal square)
        {
            if (square < 0)
                return 0;

            var root = square / 3;

            for (var i = 0; i < 32; i++)
                root = (root + square / root) / 2;

            return root;
        }

        public static bool IsPrimeNumber(decimal number)
        {
            if (number <= 1)
                return false;

            if (number == 2)
                return true;

            if (number % 2 == 0)
                return false;

            var boundary = Math.Floor(SquareRoot(number));

            for (var i = 3m; i <= boundary; i += 2)
            {
                if (number % i == 0)
                    return false;
            }

            return true;
        }

        public static (decimal start, decimal end) GetJob(decimal totalPossibilities, int totalThread, int currentThreadIndex, int offset)
        {
            var startRange = DivideEvenly(totalPossibilities, totalThread).Take(currentThreadIndex + 1).Sum() - DivideEvenly(totalPossibilities, totalThread).ElementAt(currentThreadIndex) + offset;
            var endRange = DivideEvenly(totalPossibilities, totalThread).Take(currentThreadIndex + 1).Sum() + offset - 1;

            return (startRange, endRange);
        }

        public static IEnumerable<(decimal first, decimal second)> FactorOf(decimal result)
        {
            var meanAverage = Math.Ceiling(SquareRoot(result));

            for (var i = ClassMiningStats.CurrentMinRangeJob; i <= meanAverage; i++)
            {
                if (result % i != 0)
                    continue;

                var number = result / i;

                yield return (i, number);

                if (i != number)
                    yield return (number, i);
            }
        }

        public static IEnumerable<(decimal first, decimal second)> DivisorOf(decimal result)
        {
            for (var i = ClassMiningStats.CurrentMaxRangeJob; i >= result; i--)
            {
                if (i % result != 0)
                    continue;

                yield return (i, i / result);
            }
        }

        public static IEnumerable<(decimal first, decimal second)> SumOf(decimal result)
        {
            if (result == 2 || result == 3)
                yield break;

            for (var i = ClassMiningStats.CurrentMinRangeJob; i < result - 1; i++)
            {
                var number = result - i;

                yield return (i, result - i);

                if (i != number)
                    yield return (result - i, i);
            }
        }

        public static IEnumerable<(decimal first, decimal second)> SubtractOf(decimal result)
        {
            for (var i = ClassMiningStats.CurrentMaxRangeJob; i > result + 1; i--)
                yield return (i, i - result);
        }
    }
}