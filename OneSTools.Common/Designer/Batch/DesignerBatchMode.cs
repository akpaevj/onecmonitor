using System.Diagnostics;
using OneSTools.Common.Platform;

namespace OneSTools.Common.Designer.Batch;

public class DesignerBatchMode : IDisposable
{
    private readonly List<string> _arguments = [];
    private string _outFilePath = string.Empty;
    private readonly ProcessStartInfo _processStartInfo;
    private Process? _process;
    private bool _needRaiseEvent = true;
    
    public event EventHandler<(int ExitCode, string OutFileContent)>? ProcessExited;
    
    public DesignerBatchMode(V8Platform platform, string server, string infoBase)
    {
        _processStartInfo = InitProcessStartInfo(platform);
        _arguments.Add($"/S{server}\\{infoBase}");
    }
    
    public DesignerBatchMode(V8Platform platform, string ibName)
    {
        _processStartInfo = InitProcessStartInfo(platform);
        _arguments.Add($"/IBName \"{ibName}\"");
    }

    public void StartSshAgent(string baseDirectoryPath = "", bool visible = false)
    {
        _arguments.Add("/AgentMode");
        _arguments.Add("/AgentSSHHostKeyAuto");

        if (!string.IsNullOrEmpty(baseDirectoryPath))
            _arguments.Add($"/AgentBaseDir\"{baseDirectoryPath}\"");
        
        if (visible)
            _arguments.Add("/Visible");
        
        Start();
    }
    
    public void LoadConfiguration(string cfPath, string user, string password, string accessCode = "")
    {
        AddBatchModeCommonArgs(user, password, accessCode);
        
        _arguments.Add($"/LoadCfg\"{cfPath}\"");
        _arguments.Add("/UpdateDBCfg -Dynamic- -Server -SessionTerminate force");
        
        Start();
    }
    
    public void UpdateConfiguration(string cfuPath, string user, string password, string accessCode = "")
    {
        AddBatchModeCommonArgs(user, password, accessCode);
        
        _arguments.Add($"/UpdateCfg\"{cfuPath}\"");
        _arguments.Add("/UpdateDBCfg -Dynamic- -Server -SessionTerminate force");
        
        Start();
    }
    
    public void LoadExtension(string extensionName, string cfePath, string user, string password, string accessCode = "")
    {
        AddBatchModeCommonArgs(user, password, accessCode);
        
        _arguments.Add($"/LoadCfg\"{cfePath}\"");
        _arguments.Add($"-Extension\"{extensionName}\"");
        _arguments.Add("/UpdateDBCfg -Dynamic- -Server -SessionTerminate force");
        
        Start();
    }
    
    /// <summary>
    /// !!! Never call it while batch operation is running
    /// </summary>
    public void Stop()
    {
        _needRaiseEvent = false;
        Dispose();
    }

    public async Task WaitForExit(CancellationToken cancellationToken)
    {
        if (_process != null)
            await _process.WaitForExitAsync(cancellationToken);
    }
    
    private void Start()
    {
        _processStartInfo.Arguments = string.Join(" ", _arguments);
        
        _process = new Process();
        _process.StartInfo = _processStartInfo;

        _process.Exited += Exited;
        if (!_process.Start())
            throw new Exception($"Failed to start {_processStartInfo.FileName} {_processStartInfo.Arguments}");
    }
    
    private ProcessStartInfo InitProcessStartInfo(V8Platform platform)
    {
        if (!platform.HasOnecV8)
            throw new Exception($"{platform.PlatformPath} doesn't contain 1cv8 executable");
        
        var psi = new ProcessStartInfo
        {
            FileName = platform.OnecV8Path
        };
        _arguments.Add("DESIGNER");

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

        var outFilContent = string.Empty;
        if (!string.IsNullOrEmpty(_outFilePath) && File.Exists(_outFilePath))
            File.ReadAllText(outFilContent);
        
        ProcessExited?.Invoke(this, (_process?.ExitCode ?? 0, outFilContent));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        
        _process?.Dispose();
        _process = null;
    }
}