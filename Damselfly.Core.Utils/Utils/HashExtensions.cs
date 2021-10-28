using System.Numerics;

namespace Damselfly.Core.Utils
{
    public class HashExtensions
    {
        /// <summary>
        /// Returns a percentage similarity score for two perceptual
        /// hashees where 1.0 is 100% identical, 0.5 is 50% match,
        /// and 0 is not a match at all.
        /// </summary>
        /// <param name="hash1"></param>
        /// <param name="hash2"></param>
        /// <returns></returns>
        public static double Similarity(ulong hash1, ulong hash2)
        {
            return (64 - BitOperations.PopCount(hash1 ^ hash2)) / 64.0;
        }

    }
}

