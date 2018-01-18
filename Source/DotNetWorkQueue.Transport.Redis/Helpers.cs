using System.Collections.Generic;
using System.Linq;

namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Misc Extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Splits a list into multiple IEnumerable, up to a maximum size for each one
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        /// <remarks>For example, an input with 97 items split into a size of 20 would result in
        /// 20
        /// 20
        /// 20
        /// 20
        /// 17
        /// </remarks>
        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> source, int size)
        {
            var partition = new List<T>(size);
            var counter = 0;

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    partition.Add(enumerator.Current);
                    counter++;
                    if (counter%size != 0) continue;
                    yield return partition.ToList();
                    partition.Clear();
                    counter = 0;
                }

                if (counter != 0)
                    yield return partition;
            }
        }
    }
}
