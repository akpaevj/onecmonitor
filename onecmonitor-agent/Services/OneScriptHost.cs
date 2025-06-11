using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Xml;
using System.Xml.Serialization;
using OnecMonitor.Agent.Models;
using OnecMonitor.Common.Models;
using OnecMonitor.Common.Oscript;
using OneScript.Sources;
using OneScript.StandardLibrary;
using ScriptEngine;
using ScriptEngine.HostedScript;
using ScriptEngine.HostedScript.Extensions;
using ScriptEngine.Hosting;

namespace OnecMonitor.Agent.Services;

public class OneScriptExecutor : IHostApplication
{
    public EventHandler<(string Message, MessageStatusEnum Status)>? OnEcho = null;
    public EventHandler<Exception>? OnError = null;
    
    public void ExecutePackageScript(string path, OpmMetadata metadata)
    {
        var executablePath = Path.Combine(path, metadata.Executable);
        var librariesPath = Path.Combine(path, "oscript_modules");
        
        using var engine = CreateEngine(librariesPath);
        engine.Initialize();

        var source = SourceCodeBuilder
            .Create()
            .FromFile(executablePath)
            .Build();

        var process = engine.CreateProcess(this, source);
        var exitCode = process.Start();

        if (exitCode != 0)
            throw new Exception("Ошибка выполнения скрипта");
    }

    private static HostedScriptEngine CreateEngine(string librariesPath)
    {
        var builder = DefaultEngineBuilder
            .Create()
            .SetDefaultOptions()
            .UseImports()
            .SetupEnvironment(e =>
            {
                e.AddStandardLibrary();
            });

        builder.Services.RegisterSingleton<IDependencyResolver>(new FileSystemDependencyResolver
        {
            LibraryRoot = librariesPath
        });
        
        return new HostedScriptEngine(builder.Build());
    }

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