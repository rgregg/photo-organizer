using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace PhotoOrganizer
{
    static class ForEachAsyncExtension
    {
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            try
                            {
                                await body(partition.Current);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error during ForEachAsync: {0}", ex.Message);
                            }
                }));
        }

    }
}
