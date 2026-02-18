namespace DavidHome.Optimizely.VirtualText.Core;

internal static class AsyncHelper
{
    private static readonly TaskFactory TaskFactory = new(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

    // Helper for async methods that return a value
    public static TResult RunSync<TResult>(Func<Task<TResult>> func)
    {
        return TaskFactory
            .StartNew(func)
            .Unwrap() // Unwrap the inner task
            .GetAwaiter()
            .GetResult(); // Block synchronously and propagate exceptions correctly
    }

    // Helper for async methods that return void (Task)
    public static void RunSync(Func<Task> func)
    {
        TaskFactory
            .StartNew(func)
            .Unwrap()
            .GetAwaiter()
            .GetResult();
    }
}