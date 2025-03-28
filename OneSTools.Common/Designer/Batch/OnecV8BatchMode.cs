using System.Diagnostics;
using OneSTools.Common.Platform;

namespace OneSTools.Common.Designer.Batch;

public sealed class OnecV8BatchMode : IDisposable
{
    private readonly List<string> _arguments = [];
    private readonly bool _designerMode;
    private string _outFilePath = string.Empty;
    private readonly ProcessStartInfo _processStartInfo;
    private Process? _process;
    private bool _needRaiseEvent = true;
    
    public string OutFileContent { get; private set; } = string.Empty;
    public event EventHandler<(int ExitCode, string OutFileContent)>? ProcessExited;
    
    public OnecV8BatchMode(V8Platform platform, string server, string infoBase, bool designerMode = true)
    {
        _designerMode = designerMode;
        _processStartInfo = InitProcessStartInfo(platform);
        _arguments.Add($"/S{server}\\{infoBase}");
    }
    
    public OnecV8BatchMode(V8Platform platform, string ibName, bool designerMode = true)
    {
        _designerMode = designerMode;
        _processStartInfo = InitProcessStartInfo(platform);
        _arguments.Add($"/IBName \"{ibName}\"");
    }

    public void StartSshAgent(string baseDirectoryPath = "", bool visible = false, bool waitForExit = false)
    {
        _arguments.Add("/AgentMode");
        _arguments.Add("/AgentSSHHostKeyAuto");

        if (!string.IsNullOrEmpty(baseDirectoryPath))
            _arguments.Add($"/AgentBaseDir\"{baseDirectoryPath}\"");
        
        if (visible)
            _arguments.Add("/Visible");
        
        Start(waitForExit);
    }

    public void ExecuteExternalDataProcessor(string path, string user, string password, string accessCode = "", bool waitForExit = false)
    {
        AddBatchModeCommonArgs(user, password, accessCode);
        
        _arguments.Add($"/Execute\"{path}\"");
        
        Start(waitForExit);
    }
    
    public void LoadConfiguration(string cfPath, string user, string password, string accessCode = "", bool waitForExit = false)
    {
        AddBatchModeCommonArgs(user, password, accessCode);
        
        _arguments.Add($"/LoadCfg\"{cfPath}\" /UpdateDBCfg -Dynamic- -Server -SessionTerminate force");
        
        Start(waitForExit);
    }
    
    public void UpdateConfiguration(string cfuPath, string user, string password, string accessCode = "", bool waitForExit = false)
    {
        AddBatchModeCommonArgs(user, password, accessCode);
        
        _arguments.Add($"/UpdateCfg\"{cfuPath}\" /UpdateDBCfg -Dynamic- -Server -SessionTerminate force");
        
        Start(waitForExit);
    }
    
    public void LoadExtension(string extensionName, string cfePath, string user, string password, string accessCode = "", bool waitForExit = false)
    {
        AddBatchModeCommonArgs(user, password, accessCode);
        
        _arguments.Add($"/LoadCfg\"{cfePath}\" -Extension\"{extensionName}\" /UpdateDBCfg");
        
        Start(waitForExit);
    }
    
    /// <summary>
    /// !!! Never call it while batch operation is running
    /// </summary>
    public void Stop()
    {
        _needRaiseEvent = false;
        Dispose();
    }
    
    private void Start(bool waitForExit = false)
    {
        _processStartInfo.Arguments = string.Join(" ", _arguments);
        
        _process = new Process();
        _process.StartInfo = _processStartInfo;

        if (!waitForExit)
            _process.Exited += Exited;
        
        if (!_process.Start())
            throw new Exception($"Failed to start {_processStartInfo.FileName} {_processStartInfo.Arguments}");

        if (!waitForExit) 
            return;
        
        _process.WaitForExit();
            
        SetOutFileContent();
            
        if (_process.ExitCode != 0)
            throw new Exception(OutFileContent);
    }
    
    private ProcessStartInfo InitProcessStartInfo(V8Platform platform)
    {
        if (!platform.HasOnecV8)
            throw new Exception($"{platform.PlatformPath} doesn't contain 1cv8 executable");
        
        var psi = new ProcessStartInfo
        {
            FileName = platform.OnecV8Path,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        _arguments.Add(_designerMode ? "DESIGNER" : "ENTERPRISE");

        return psi;
    }
    
    private void AddBatchModeCommonArgs(string user, string password, string accessCode = "")
    {
        _arguments.Add($"/N{user}");
        _arguments.Add($"/P{password}");
        _arguments.Add("/DisableStartupMessages");
        _arguments.Add("/DisableStartupDialogs");
        
        if (!string.IsNullOrEmpty(accessCode))
            _arguments.Add($"/UC{accessCode}");
        
        _outFilePath = Path.GetTempFileName();
        _arguments.Add($"/Out\"{_outFilePath}\"");
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

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) 
            return;
        
        _process?.Dispose();
        _process = null;
    }
}