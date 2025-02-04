using System.Diagnostics;
using OneSTools.Common.Platform;

namespace OneSTools.Common.Designer.Agent;

public class DesignerAgent : IDisposable
{
    private List<string> _arguments = [];
    private readonly ProcessStartInfo _processStartInfo;
    private Process? _process;
    private bool _needRaiseEvent = true;
    
    public event EventHandler<int>? ProcessExited;
    
    public DesignerAgent(V8Platform platform, string server, string infoBase, bool visible = false)
    {
        _processStartInfo = InitProcessStartInfo(platform);
        _arguments.Add($"/S{server}\\{infoBase}");
        AddCommonArgs(visible);
    }
    
    public DesignerAgent(V8Platform platform, string ibName, bool visible = false)
    {
        _processStartInfo = InitProcessStartInfo(platform);
        _arguments.Add($"/IBName \"{ibName}\"");
        AddCommonArgs(visible);
    }

    public void Start()
    {
        _processStartInfo.Arguments = string.Join(" ", _arguments);
        
        _process = new Process();
        _process.StartInfo = _processStartInfo;

        _process.Exited += Exited;
        if (!_process.Start())
            throw new Exception($"Failed to start {_processStartInfo.FileName} {_processStartInfo.Arguments}");
    }

    public void Stop()
    {
        _needRaiseEvent = false;
        Dispose();
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

    private void AddCommonArgs(bool visible)
    {
        _arguments.Add("/AgentMode");
        _arguments.Add("/AgentSSHHostKeyAuto");
        
        if (visible)
            _arguments.Add("/Visible");
    }

    private void Exited(object? sender, EventArgs e)
    {
        if (!_needRaiseEvent) return;
        
        ProcessExited?.Invoke(this, _process?.ExitCode ?? 0);
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