using System;
using System.Collections;

namespace DebuggerPlugins.Logger.Extensions
{
    internal static class EnumerableExtensions
    {
        public static int CountLessOrEqual(this IEnumerable enumerable, int maxCount)
        {
            if (maxCount < 0)
                throw new ArgumentException("Count must have non-negative value", nameof(maxCount));

            var enumerator = enumerable.GetEnumerator();
            for (int count = 0; count < maxCount; count++)
            {
                if (!enumerator.MoveNext())
                    return count;
            }

            return maxCount;
        }
    }
}