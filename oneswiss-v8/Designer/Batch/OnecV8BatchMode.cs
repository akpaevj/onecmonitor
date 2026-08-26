using System.Diagnostics;
using OneSwiss.V8.Platform;

namespace OneSwiss.V8.Designer.Batch;

public sealed class OnecV8BatchMode : IDisposable
{
    private readonly V8Platform _platform;
    private readonly List<string> _arguments = [];
    private bool _needRaiseEvent = true;
    private string _outFilePath = string.Empty;
    private Process? _process;

    private OnecV8BatchMode(V8Platform platform, string mode)
    {
        if (!platform.HasOnecV8)
            throw new Exception($"{platform.PlatformPath} не содержит исполняемый файл 1cv8");

        _platform = platform;
        _arguments.Add(mode);
    }

    public string OutFileContent { get; private set; } = string.Empty;

    public void Dispose()
    {
        Dispose(true);
    }

    public event EventHandler<(int ExitCode, string OutFileContent)>? ProcessExited;

    public static OnecV8BatchMode CreateDesignerBatch(V8Platform platform, string server, string infoBase)
    {
        var batch = new OnecV8BatchMode(platform, "DESIGNER");
        batch._arguments.Add($"/S{server}\\{infoBase}");

        return batch;
    }

    public static OnecV8BatchMode CreateDesignerBatch(V8Platform platform, string ibPath)
    {
        var batch = new OnecV8BatchMode(platform, "DESIGNER");
        batch._arguments.Add($"/F\"{ibPath}\"");

        return batch;
    }

    public static OnecV8BatchMode CreateEnterpriseBatch(V8Platform platform, string server, string infoBase)
    {
        var batch = new OnecV8BatchMode(platform, "ENTERPRISE");
        batch._arguments.Add($"/S{server}\\{infoBase}");

        return batch;
    }

    public static OnecV8BatchMode CreateEnterpriseBatch(V8Platform platform, string ibPath)
    {
        var batch = new OnecV8BatchMode(platform, "ENTERPRISE");
        batch._arguments.Add($"/F\"{ibPath}\"");

        return batch;
    }

    public static async Task CreateFileInfoBase(V8Platform platform, string path)
    {
        using var batch = new OnecV8BatchMode(platform, "CREATEINFOBASE");
        batch._arguments.Add($"\"File=\"{path}\";\"");
        batch.AddOutArgument();

        await batch.Start(true);
    }

    public async Task StartSshAgent(string baseDirectoryPath = "", bool visible = false, bool waitForExit = false)
    {
        _arguments.Add("/AgentMode");
        _arguments.Add("/AgentSSHHostKeyAuto");

        if (!string.IsNullOrEmpty(baseDirectoryPath))
            _arguments.Add($"/AgentBaseDir\"{baseDirectoryPath}\"");

        if (visible)
            _arguments.Add("/Visible");

        await Start(waitForExit);
    }

    public async Task ExecuteExternalDataProcessor(string path, string user, string password, string accessCode = "",
        bool waitForExit = false)
    {
        AddBatchModeCommonArgs(user, password, accessCode);

        _arguments.Add($"/Execute\"{path}\"");

        await Start(waitForExit);
    }

    public async Task LoadConfiguration(string cfPath, string user, string password, string accessCode = "",
        bool waitForExit = false)
    {
        AddBatchModeCommonArgs(user, password, accessCode);

        _arguments.Add($"/LoadCfg\"{cfPath}\" /UpdateDBCfg -Dynamic- -Server -SessionTerminate force");

        await Start(waitForExit);
    }

    public async Task UpdateConfiguration(string cfuPath, string user, string password, string accessCode = "",
        bool waitForExit = false)
    {
        AddBatchModeCommonArgs(user, password, accessCode);

        _arguments.Add($"/UpdateCfg\"{cfuPath}\" /UpdateDBCfg -Dynamic- -Server -SessionTerminate force");

        await Start(waitForExit);
    }

    public async Task<string[]> GetExtensionsList(string user, string password, string accessCode = "", bool waitForExit = false)
    {
        AddBatchModeCommonArgs(user, password, accessCode);

        _arguments.Add("/DumpDBCfgList -AllExtensions");

        await Start(waitForExit);

        return OutFileContent.Split('\n').Select(c => c.Trim()).ToArray();
    }

    public async Task LoadExtension(string extensionName, string cfePath, string user, string password,
        string accessCode = "", bool waitForExit = false)
    {
        AddBatchModeCommonArgs(user, password, accessCode);

        _arguments.Add($"/LoadCfg\"{cfePath}\" -Extension\"{extensionName}\" /UpdateDBCfg");

        await Start(waitForExit);
    }

    public async Task DeleteExtension(string extensionName, string user, string password, string accessCode = "",
        bool waitForExit = false)
    {
        AddBatchModeCommonArgs(user, password, accessCode);

        _arguments.Add($"/DeleteCfg -Extension\"{extensionName}\"");

        await Start(waitForExit);
    }

    /// <summary>
    ///     Выгружает отчет по истории хранилища по указанному пути
    /// </summary>
    /// <param name="connectionString">Строка соединения с хранилищем. Указывается в точно таком же виде, как и в конфигураторе</param>
    /// <param name="user">Пользователь хранилища</param>
    /// <param name="password">Пароль пользователя хранилища</param>
    /// <param name="version">Номер версии, с которой начинается строиться отчет. -1 если необходимо выгрузить последнюю версию</param>
    /// <returns>Читатель отчета хранилища конфигураций</returns>
    public async Task<ConfigRepositoryReportReader> GetConfigRepositoryReportReader(
        string connectionString,
        string user,
        string password,
        int version = -1,
        string extension = "")
    {
        DisableStartupDialogAndMessages();
        AddOutArgument();

        AddConfigRepositoryCommonArgs(connectionString, user, password);

        var reportPath = Path.GetTempFileName();
        _arguments.Add($"/ConfigurationRepositoryReport\"{reportPath}\"");
        _arguments.Add("-ReportFormat txt");

        if (version != -1)
            _arguments.Add($"-NBegin {version}");
        
        if (!string.IsNullOrEmpty(extension))
            _arguments.Add($"-Extension {extension}");

        await Start(true);

        return new ConfigRepositoryReportReader(reportPath);
    }

    public async Task DumpConfigToFiles(string path, string user = "", string password = "", string extension = "",
        bool isUpdate = false)
    {
        AddBatchModeCommonArgs(user, password);

        _arguments.Add($"/DumpConfigToFiles\"{path}\"");

        if (!string.IsNullOrEmpty(extension))
            _arguments.Add($"-Extension\"{extension}\"");

        if (isUpdate)
            _arguments.Add("-update");

        _arguments.Add("-force");

        await Start(true);
    }

    public async Task<string> DumpConfiguration(string user = "", string password = "")
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var listFilePath = Path.GetTempFileName();

        try
        {
            Directory.CreateDirectory(path);

            File.WriteAllText(listFilePath, "Configuration");

            AddBatchModeCommonArgs(user, password);

            _arguments.Add($"/DumpConfigToFiles\"{path}\"");
            _arguments.Add($"-listFile\"{listFilePath}\"");

            await Start(true);

            return File.ReadAllText(Path.Combine(path, "Configuration.xml"));
        }
        catch
        {
            throw;
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            if (File.Exists(listFilePath))
                File.Delete(listFilePath);
        }
    }

    /// <summary>
    ///     Выгружает конфигурацию хранилища в файл
    /// </summary>
    /// <param name="path">Путь к файлу выгрузки</param>
    /// <param name="connectionString">Строка соединения с хранилищем. Указывается в точно таком же виде, как и в конфигураторе</param>
    /// <param name="user">Пользователь хранилища</param>
    /// <param name="password">Пароль пользователя хранилища</param>
    /// <param name="version">Версия хранилища</param>
    /// <param name="extension">Имя расширения</param>
    public async Task DumpConfigRepository(string path, string connectionString, string user, string password,
        int version = -1, string extension = "")
    {
        DisableStartupDialogAndMessages();
        AddOutArgument();

        AddConfigRepositoryCommonArgs(connectionString, user, password);
        _arguments.Add($"/ConfigurationRepositoryDumpCfg\"{path}\"");

        if (version != -1)
            _arguments.Add($"-v {version}");

        if (!string.IsNullOrEmpty(extension))
            _arguments.Add($"-Extension\"{extension}\"");

        await Start(true);
    }

    /// <summary>
    ///     Обновляет конфигурацию ИБ из хранилища конфигураций
    /// </summary>
    /// <param name="connectionString">Строка соединения с хранилищем. Указывается в точно таком же виде, как и в конфигураторе</param>
    /// <param name="repUser">Пользователь хранилища</param>
    /// <param name="repPassword">Пароль пользователя хранилища</param>
    /// <param name="user">Пользователь ИБ</param>
    /// <param name="password">Пароль пользователя ИБ</param>
    /// <param name="version">Версия хранилища</param>
    /// <param name="extension">Имя расширения</param>
    public async Task UpdateConfigFromRepository(string connectionString, string repUser, string repPassword,
        string user = "", string password = "", int version = -1, string extension = "")
    {
        AddBatchModeCommonArgs(user, password);
        AddConfigRepositoryCommonArgs(connectionString, repUser, repPassword);

        _arguments.Add("/ConfigurationRepositoryUpdateCfg");

        if (version != -1)
            _arguments.Add($"-v {version}");

        _arguments.Add("-force");

        if (!string.IsNullOrEmpty(extension))
            _arguments.Add($"-Extension\"{extension}\"");

        await Start(true);
    }

    /// <summary>
    /// !!! Never call it while batch operation is running
    /// </summary>
    public void Stop()
    {
        _needRaiseEvent = false;
        Dispose();
    }

    private async Task Start(bool waitForExit = false)
    {
        if (waitForExit)
        {
            var result = await ProcessRunner.RunAsync(_platform.OnecV8Path, _arguments);
            
            SetOutFileContent();
            
            if (result.ExitCode != 0)
                throw new Exception(string.IsNullOrEmpty(OutFileContent.Trim()) ? result.Error : OutFileContent);
        }
        else
        {
            var processStartInfo = new ProcessStartInfo(_platform.OnecV8Path, string.Join(" ", _arguments));
            
            _process = new Process();
            _process.StartInfo = processStartInfo;

            if (!waitForExit)
                _process.Exited += Exited;

            if (!_process.Start())
                throw new Exception($"Failed to start {processStartInfo.FileName} {processStartInfo.Arguments}");

            if (!waitForExit)
                return;

            _process.WaitForExit();

            SetOutFileContent();

            if (_process.ExitCode != 0)
                throw new Exception(OutFileContent);
        }
    }

    private void AddConfigRepositoryCommonArgs(string connectionString, string user, string password)
    {
        _arguments.Add($"/ConfigurationRepositoryF\"{connectionString}\"");
        _arguments.Add($"/ConfigurationRepositoryN{user}");
        _arguments.Add($"/ConfigurationRepositoryP{password}");
    }

    private void AddBatchModeCommonArgs(string user = "", string password = "", string accessCode = "")
    {
        if (user != string.Empty)
            _arguments.Add($"/N{user}");

        if (password != string.Empty)
            _arguments.Add($"/P{password}");

        DisableStartupDialogAndMessages();

        if (!string.IsNullOrEmpty(accessCode))
            _arguments.Add($"/UC{accessCode}");

        AddOutArgument();
    }

    private void AddOutArgument()
    {
        _outFilePath = Path.GetTempFileName();
        _arguments.Add($"/Out\"{_outFilePath}\"");
    }

    private void DisableStartupDialogAndMessages()
    {
        _arguments.Add("/DisableStartupMessages");
        _arguments.Add("/DisableStartupDialogs");
    }

    private void Exited(object? sender, EventArgs e)
    {
        if (!_needRaiseEvent)
            return;

        SetOutFileContent();
        ProcessExited?.Invoke(this, (_process?.ExitCode ?? 0, OutFileContent));
    }

    private void SetOutFileContent()
    {
        OutFileContent = string.Empty;
        if (!string.IsNullOrEmpty(_outFilePath) && File.Exists(_outFilePath))
            OutFileContent = File.ReadAllText(_outFilePath);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _process?.Kill();
        _process?.Dispose();
        _process = null;
        
        if (File.Exists(_outFilePath))
            File.Delete(_outFilePath);
    }
}