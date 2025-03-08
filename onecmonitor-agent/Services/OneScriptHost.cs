using System.Diagnostics.CodeAnalysis;
using OneScript.Commons;
using OneScript.Sources;
using OneScript.StandardLibrary;
using OneScript.StandardLibrary.Collections;
using ScriptEngine;
using ScriptEngine.HostedScript;
using ScriptEngine.HostedScript.Extensions;
using ScriptEngine.Hosting;
using ScriptEngine.Machine;

namespace OnecMonitor.Agent.Services;

public class OneScriptExecutor : IHostApplication
{
    public EventHandler<(string Message, MessageStatusEnum Status)>? OnEcho;
    public EventHandler<Exception>? OnError;
    
    public void Execute(string script)
    {
        using var engine = CreateEngine();
        engine.Initialize();

        var source = SourceCodeBuilder
            .Create()
            .FromString(script)
            .Build();

        var process = engine.CreateProcess(this, source);
        var exitCode = process.Start();

        if (exitCode != 0)
            throw new Exception("Ошибка выполнения скрипта");
    }
    
    private static HostedScriptEngine CreateEngine()
        => new(DefaultEngineBuilder
            .Create()
            .SetDefaultOptions()
            .UseNativeRuntime()
            .UseImports()
            .SetupEnvironment(e =>
            {
                e.AddStandardLibrary();
            })
            .UseFileSystemLibraries()
            .Build());

    public void Echo(string str, MessageStatusEnum status = MessageStatusEnum.Ordinary)
    {
        OnEcho?.Invoke(this, (str, status));
    }

    public void ShowExceptionInfo(Exception exc)
    {
        OnError?.Invoke(this, exc);
    }

    public bool InputString([UnscopedRef] out string result, string prompt, int maxLen, bool multiline)
    {
        throw new NotImplementedException();
    }

    public string[] GetCommandLineArguments()
    {
        throw new NotImplementedException();
    }
}