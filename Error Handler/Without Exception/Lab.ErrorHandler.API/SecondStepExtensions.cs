using Lab.ErrorHandler.API.Models;

namespace Lab.ErrorHandler.API;

public static class SecondStepExtensions
{
    /// <summary>
    /// 接續執行第二個方法
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static async Task<TResult> Then<TSource, TResult>(this Task<TSource> first,
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
    public static async Task<(Failure Failure, TResult Data)> WhenSuccess<TSource, TResult>(
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
}