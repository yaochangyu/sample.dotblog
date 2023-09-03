namespace Lab.ErrorHandler.API;

public static class SecondStepExtensions
{
    // public static async Task<T2> Then<T1, T2>(this T1 first, Func<T1, Task<T2>> second)
    // {
    //     return await second(first).ConfigureAwait(false);
    // }
    //

    /// <summary>
    /// 接續執行第二個方法
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static async Task<TResult> ThenAnyAsync<TSource, TResult>(this Task<TSource> first,
        Func<TSource, Task<TResult>> second)
    {
        return await second(await first.ConfigureAwait(false)).ConfigureAwait(false);
    }

    /// <summary>
    /// 接續第二個方法，第一個方法有錯誤時，不執行第二個方法
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static async Task<(Failure Failure, TResult Data)> ThenAsyncIfNoFailure<TSource, TResult>(
        this Task<(Failure Failure, TSource Data)> first,
        Func<TSource, Task<(Failure, TResult)>> second)
    {
        var result = await first.ConfigureAwait(false);
        if (result.Failure != null)
        {
            return (result.Failure, default(TResult));
        }

        return await second(result.Data).ConfigureAwait(false);
    }

    // public static async Task<TResult> ThenAsync<TSource, TResult>(this Task<TSource> first,
    //     Func<TSource, TResult> second)
    // {
    //     return second(await first.ConfigureAwait(false));
    // }

    // public static Task<T2> Then<T1, T2>(this T1 first, Func<T1, T2> second)
    // {
    //     return Task.FromResult(second(first));
    // }

    // public static TResult Pipe<TSource, TResult>(this TSource target, Func<TSource, TResult> func) =>
    //     func(target);
    //
    // public static async Task<TR> PipeAsync<T, TR>(this Task<T> target, Func<T, TR> func) =>
    //     func(await target);
    //
    // public static async Task<TR> PipeAsync<T, TR>(this Task<T> target, Func<T, Task<TR>> func) =>
    //     await func(await target);
}