using System.Diagnostics;

namespace OneSwiss.V8.Extensions;

public static class ProcessExtensions
{
    public static async Task WaitForExitAsync(
        this Process process,
        CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        process.Exited += (_, _) => tcs.TrySetResult();

        if (process.HasExited)
            return;

        await using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());

        await tcs.Task;
    }
}