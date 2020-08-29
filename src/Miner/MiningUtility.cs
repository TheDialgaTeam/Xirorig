﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TheDialgaTeam.Xiropht.Xirorig.Miner
{
    public static class MiningUtility
    {
        private static readonly char[] Base10CharRepresentation = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        private static readonly char[] Base16CharRepresentation = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private static readonly byte[] Base16ByteRepresentation = { (byte) '0', (byte) '1', (byte) '2', (byte) '3', (byte) '4', (byte) '5', (byte) '6', (byte) '7', (byte) '8', (byte) '9', (byte) 'A', (byte) 'B', (byte) 'C', (byte) 'D', (byte) 'E', (byte) 'F' };

        public static unsafe string MakeEncryptedShare(string value, string xorKey, int round, ICryptoTransform aesCryptoTransform, SHA512 sha512)
        {
            var valueLength = value.Length;
            var xorKeyLength = xorKey.Length;

            var outputLength = valueLength * 2;
            var output = ArrayPool<byte>.Shared.Rent(outputLength);

            fixed (char* base16CharRepresentationPtr = Base16CharRepresentation, xorKeyPtr = xorKey)
            fixed (byte* base16ByteRepresentationPtr = Base16ByteRepresentation)
            {
                var xorKeyIndex = 0;

                // First encryption phase convert to hex and xor each result.
                fixed (byte* outputPtr = output)
                fixed (char* valuePtr = value)
                {
                    var outputBytePtr = outputPtr;
                    var valueCharPtr = valuePtr;

                    for (var i = valueLength - 1; i >= 0; i--)
                    {
                        *outputBytePtr = (byte) (*(base16ByteRepresentationPtr + (*valueCharPtr >> 4)) ^ *(xorKeyPtr + xorKeyIndex));
                        outputBytePtr++;
                        xorKeyIndex++;

                        if (xorKeyIndex == xorKeyLength)
                        {
                            xorKeyIndex = 0;
                        }

                        *outputBytePtr = (byte) (*(base16ByteRepresentationPtr + (*valueCharPtr & 15)) ^ *(xorKeyPtr + xorKeyIndex));
                        outputBytePtr++;
                        xorKeyIndex++;

                        if (xorKeyIndex == xorKeyLength)
                        {
                            xorKeyIndex = 0;
                        }

                        valueCharPtr++;
                    }
                }

                // Second encryption phase: run through aes per round and apply xor at the final round.
                const byte dash = (byte) '-';

                for (var i = round; i >= 0; i--)
                {
                    var aesOutput = aesCryptoTransform.TransformFinalBlock(output, 0, outputLength);
                    var aesOutputLength = aesOutput.Length;

                    ArrayPool<byte>.Shared.Return(output);

                    outputLength = aesOutputLength * 2 + aesOutputLength - 1;
                    output = ArrayPool<byte>.Shared.Rent(outputLength);

                    fixed (byte* outputPtr = output, aesOutputPtr = aesOutput)
                    {
                        var outputBytePtr = outputPtr;
                        var aesOutputBytePtr = aesOutputPtr;

                        if (i == 1)
                        {
                            xorKeyIndex = 0;

                            for (var j = aesOutputLength - 1; j >= 0; j--)
                            {
                                *outputBytePtr = (byte) (*(base16ByteRepresentationPtr + (*aesOutputBytePtr >> 4)) ^ *(xorKeyPtr + xorKeyIndex));
                                outputBytePtr++;
                                xorKeyIndex++;

                                if (xorKeyIndex == xorKeyLength)
                                {
                                    xorKeyIndex = 0;
                                }

                                *outputBytePtr = (byte) (*(base16ByteRepresentationPtr + (*aesOutputBytePtr & 15)) ^ *(xorKeyPtr + xorKeyIndex));
                                outputBytePtr++;
                                xorKeyIndex++;

                                if (xorKeyIndex == xorKeyLength)
                                {
                                    xorKeyIndex = 0;
                                }

                                if (j == 0) break;

                                *outputBytePtr = (byte) (dash ^ *(xorKeyPtr + xorKeyIndex));
                                outputBytePtr++;
                                xorKeyIndex++;

                                if (xorKeyIndex == xorKeyLength)
                                {
                                    xorKeyIndex = 0;
                                }

                                aesOutputBytePtr++;
                            }
                        }
                        else
                        {
                            for (var j = aesOutputLength - 1; j >= 0; j--)
                            {
                                *outputBytePtr = *(base16ByteRepresentationPtr + (*aesOutputBytePtr >> 4));
                                outputBytePtr++;

                                *outputBytePtr = *(base16ByteRepresentationPtr + (*aesOutputBytePtr & 15));
                                outputBytePtr++;

                                if (j == 0) break;

                                *outputBytePtr = dash;
                                outputBytePtr++;
                                aesOutputBytePtr++;
                            }
                        }
                    }
                }

                // Third encryption phase: compute hash
                var hashOutput = sha512.ComputeHash(output, 0, outputLength);
                var hashOutputLength = hashOutput.Length;

                ArrayPool<byte>.Shared.Return(output);

                var result = new string('\0', hashOutputLength * 2);

                fixed (byte* hashOutputPtr = hashOutput)
                fixed (char* resultPtr = result)
                {
                    var hashOutputBytePtr = hashOutputPtr;
                    var resultCharPtr = resultPtr;

                    for (var i = hashOutputLength - 1; i >= 0; i--)
                    {
                        *resultCharPtr = *(base16CharRepresentationPtr + (*hashOutputBytePtr >> 4));
                        resultCharPtr++;

                        *resultCharPtr = *(base16CharRepresentationPtr + (*hashOutputBytePtr & 15));
                        resultCharPtr++;

                        hashOutputBytePtr++;
                    }
                }

                return result;
            }
        }

        public static unsafe string ComputeHash(SHA512 hashAlgorithm, string value)
        {
            var output = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
            var outputLength = output.Length;
            var result = new string('\0', outputLength * 2);

            fixed (byte* outputPtr = output)
            fixed (char* base16CharRepresentationPtr = Base16CharRepresentation, resultPtr = result)
            {
                var outputBytePtr = outputPtr;
                var resultCharPtr = resultPtr;

                for (var i = outputLength - 1; i >= 0; i--)
                {
                    *resultCharPtr = *(base16CharRepresentationPtr + (*outputBytePtr >> 4));
                    resultCharPtr++;

                    *resultCharPtr = *(base16CharRepresentationPtr + (*outputBytePtr & 15));
                    resultCharPtr++;

                    outputBytePtr++;
                }
            }

            return result;
        }

        public static unsafe decimal GenerateNumberMathCalculation(RNGCryptoServiceProvider rngCryptoServiceProvider, byte[] randomNumber, decimal minRange, decimal maxRange)
        {
            decimal resultDecimal;

            do
            {
                var randomSize = GetRandomBetween(rngCryptoServiceProvider, randomNumber, 1, GetRandomBetweenJob(rngCryptoServiceProvider, randomNumber, minRange, maxRange).ToString("F0").Length);
                var resultString = new string('\0', randomSize);

                fixed (char* base10CharRepresentationPtr = Base10CharRepresentation, resultPtr = resultString)
                {
                    var resultCharPtr = resultPtr;

                    for (var i = 0; i < randomSize; i++)
                    {
                        if (randomSize == 1)
                        {
                            *resultCharPtr = *(base10CharRepresentationPtr + GetRandomBetween(rngCryptoServiceProvider, randomNumber, 2, 9));
                        }
                        else
                        {
                            *resultCharPtr = *(base10CharRepresentationPtr + GetRandomBetween(rngCryptoServiceProvider, randomNumber, i == 0 ? 1 : 0, 9));
                        }

                        resultCharPtr++;
                    }
                }

                resultDecimal = Convert.ToDecimal(resultString);
            } while (resultDecimal < minRange || resultDecimal > maxRange);

            return resultDecimal;
        }

        public static (decimal, decimal) GetJobRange(decimal totalPossibilities, int totalThread, int threadIndex, decimal offset)
        {
            var startRange = DivideEvenly(totalPossibilities, totalThread).Take(threadIndex + 1).Sum() - DivideEvenly(totalPossibilities, totalThread).ElementAt(threadIndex) + offset;
            var endRange = DivideEvenly(totalPossibilities, totalThread).Take(threadIndex + 1).Sum() + offset - 1;

            return (startRange, endRange);
        }

        public static (decimal, decimal) GetJobRangeByPercentage(decimal minRange, decimal maxRange, int minRangePercentage, int maxRangePercentage)
        {
            var startRange = Math.Floor(maxRange * minRangePercentage * 0.01m) + minRange;
            var endRange = Math.Min(maxRange, Math.Floor(maxRange * maxRangePercentage * 0.01m) + 1);

            return (startRange, endRange);
        }

        public static bool IsPrimeNumber(decimal number)
        {
            if (number <= 1)
            {
                return false;
            }

            if (number == 2)
            {
                return true;
            }

            if (number % 2 == 0)
            {
                return false;
            }

            var boundary = Math.Floor(SquareRoot(number));

            for (var i = 3m; i <= boundary; i += 2)
            {
                if (number % i == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static IEnumerable<(decimal, decimal)> SumOf(decimal result, decimal minRange)
        {
            if (result == 2 || result == 3)
            {
                yield break;
            }

            for (var i = minRange; i < result - 1; i++)
            {
                var number = result - i;

                yield return (i, result - i);

                if (i != number)
                {
                    yield return (result - i, i);
                }
            }
        }

        public static IEnumerable<Tuple<decimal, decimal>> SubtractOf(decimal result, decimal maxRange)
        {
            for (var i = maxRange; i > result + 1; i--)
            {
                yield return new Tuple<decimal, decimal>(i, i - result);
            }
        }

        public static IEnumerable<(decimal, decimal)> FactorOf(decimal result, decimal minRange)
        {
            var meanAverage = Math.Ceiling(SquareRoot(result));

            for (var i = minRange; i <= meanAverage; i++)
            {
                if (result % i != 0)
                {
                    continue;
                }

                var number = result / i;

                yield return (i, number);

                if (i != number)
                {
                    yield return (number, i);
                }
            }
        }

        public static IEnumerable<Tuple<decimal, decimal>> DivisorOf(decimal result, decimal maxRange)
        {
            for (var i = maxRange; i >= result; i--)
            {
                if (i % result != 0)
                {
                    continue;
                }

                yield return new Tuple<decimal, decimal>(i, i / result);
            }
        }

        private static int GetRandomBetween(RNGCryptoServiceProvider rngCryptoServiceProvider, byte[] randomNumber, int minimumValue, int maximumValue)
        {
            rngCryptoServiceProvider.GetBytes(randomNumber);
            return (int) (minimumValue + MathF.Floor(MathF.Max(0, randomNumber[0] / 255f - 0.0000001f) * (maximumValue - minimumValue + 1)));
        }

        private static decimal GetRandomBetweenJob(RNGCryptoServiceProvider rngCryptoServiceProvider, byte[] randomNumber, decimal minimumValue, decimal maximumValue)
        {
            rngCryptoServiceProvider.GetBytes(randomNumber);
            return minimumValue + Math.Floor((decimal) MathF.Max(0, randomNumber[0] / 255f - 0.0000001f) * (maximumValue - minimumValue + 1));
        }

        private static IEnumerable<decimal> DivideEvenly(decimal totalPossibilities, int totalThread)
        {
            var div = Math.Truncate(totalPossibilities / totalThread);
            var remainder = totalPossibilities % totalThread;

            for (var i = 0; i < totalThread; i++)
            {
                yield return i < remainder ? div + 1 : div;
            }
        }

        private static decimal SquareRoot(decimal square)
        {
            if (square < 0)
            {
                return 0;
            }

            var root = square / 3;

            for (var i = 0; i < 32; i++)
            {
                root = (root + square / root) / 2;
            }

            return root;
        }
    }
}