using System;
using System.Collections.Generic;
using System.Linq;

namespace Niind.Helpers
{
    public static class LinqHelper
    {
        public static IEnumerable<IEnumerable<byte>> Chunk(this byte[] fullList, int batchSize)
        {
            var total = 0;
            while (total < fullList.LongLength)
            {
                yield return fullList.Skip(total).Take(batchSize).ToArray();
                total += batchSize;
            }
        }
        public static IEnumerable<TResult> ZipMany<T, TResult>(this IEnumerable<T>[] sequences, Func<T[], TResult> resultSelector)
        {
            var enumerators = sequences.Select(s => s.GetEnumerator()).ToArray();
            while(enumerators.All(e => e.MoveNext()))
                yield return resultSelector(enumerators.Select(e => e.Current).ToArray());
        } 
    }
}