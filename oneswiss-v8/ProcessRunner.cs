using System.Diagnostics;
using System.Text;

namespace OneSwiss.V8;

public abstract class ProcessRunner
{
    private static Encoding GetRussianEncoding()
    {
        return OperatingSystem.IsLinux() ? Encoding.UTF8 : Encoding.GetEncoding(866);
    }
    
    /// <summary>
    /// Запускает процесс асинхронно и возвращает результат выполнения или вызывает исключение при ошибке исполнения
    /// </summary>
    /// <param name="command">Команда или имя исполняемого файла</param>
    /// <param name="args">Аргументы команды</param>
    /// <param name="workingDir">Рабочая директория</param>
    /// <param name="timeout">Таймаут выполнения</param>
    /// <returns>Результат выполнения процесса</returns>
    public static async Task<ProcessResult> RunAndThrowAsync(
        string command,
        List<string> args,
        string? workingDir = null,
        TimeSpan? timeout = null)
    {
        var result = await RunAsync(command, string.Join(" ", args), workingDir, timeout);

        if (result.ExitCode > 0)
            throw new Exception(result.Error);

        return result;
    }

    /// <summary>
    ///     Запускает процесс асинхронно и возвращает результат выполнения
    /// </summary>
    /// <param name="command">Команда или имя исполняемого файла</param>
    /// <param name="args">Аргументы команды</param>
    /// <param name="workingDir">Рабочая директория</param>
    /// <param name="timeout">Таймаут выполнения</param>
    /// <returns>Результат выполнения процесса</returns>
    public static async Task<ProcessResult> RunAsync(
        string command,
        List<string> args,
        string? workingDir = null,
        TimeSpan? timeout = null)
    {
        return await RunAsync(command, string.Join(" ", args), workingDir, timeout);
    }

    /// <summary>
    ///     Запускает процесс асинхронно и возвращает результат выполнения
    /// </summary>
    /// <param name="command">Команда или имя исполняемого файла</param>
    /// <param name="args">Аргументы команды</param>
    /// <param name="workingDir">Рабочая директория</param>
    /// <param name="timeout">Таймаут выполнения</param>
    /// <returns>Результат выполнения процесса</returns>
    public static async Task<ProcessResult> RunAsync(
        string command,
        string? args = null,
        string? workingDir = null,
        TimeSpan? timeout = null)
    {
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = args ?? string.Empty,
            WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = GetRussianEncoding(),
            StandardErrorEncoding = GetRussianEncoding()
        };
        process.EnableRaisingEvents = true;

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) => AppendIfNotNull(output, e.Data);
        process.ErrorDataReceived += (_, e) => AppendIfNotNull(error, e.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var cancellationSource = timeout.HasValue
            ? new CancellationTokenSource(timeout.Value)
            : new CancellationTokenSource();

        try
        {
            await process.WaitForExitAsync(cancellationSource.Token);
        }
        catch (OperationCanceledException) when (timeout.HasValue)
        {
            process.Kill();
            throw new TimeoutException($"Process timed out after {timeout.Value.TotalSeconds} seconds");
        }

        // Даем время на завершение асинхронного чтения вывода
        await Task.Delay(100, cancellationSource.Token);

        return new ProcessResult(
            output.ToString(),
            error.ToString(),
            process.ExitCode);
    }

    private static void AppendIfNotNull(StringBuilder builder, string? data)
    {
        if (data is not null) builder.AppendLine(data);
    }
}