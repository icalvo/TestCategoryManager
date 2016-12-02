using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestCategoryManager
{
    public static class AsynchronousEnumerableExtensions
    {
        public static Task<TResult> AggregateAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            TResult seed,
            Func<TResult, TSource, Task<TResult>> func)
        {
            return source.AggregateAsync(seed, CancellationToken.None, (result, item, token) => func(result, item));
        }

        public static Task<TResult> AggregateAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            TResult seed,
            Func<TResult, TSource, CancellationToken, Task<TResult>> func)
        {
            return source.AggregateAsync(seed, CancellationToken.None, func);
        }

        public static async Task<TResult> AggregateAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            TResult seed,
            CancellationToken token,
            Func<TResult, TSource, CancellationToken, Task<TResult>> func)
        {
            TResult result = seed;
            foreach (var item in source)
            {
                result = await func(result, item, token);
            }

            return result;
        }
    }
}