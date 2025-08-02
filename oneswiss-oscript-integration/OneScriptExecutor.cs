using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using OneScript.DebugServices;
using OneScript.Sources;
using OneScript.StandardLibrary;
using OneSwiss.V8.Platform;
using ScriptEngine;
using ScriptEngine.HostedScript;
using ScriptEngine.Hosting;
using ExecutionContext = ScriptEngine.Machine.ExecutionContext;

namespace OneSwiss.OneScript;

public class OneScriptExecutor : IHostApplication
{
    public EventHandler<(string Message, MessageStatusEnum Status)>? OnEcho = null;
    public EventHandler<Exception>? OnError = null;
    private string[] _args;
    
    public void ExecutePackageScript(string path, OpmMetadata metadata, string[] args, Action<ExecutionContext> engineBuilder)
    {
        _args = args;
        var executablePath = Path.Combine(path, metadata.Executable);
        var librariesPath = Path.Combine(path, "oscript_modules");
        
        using var engine = CreateEngine(librariesPath, engineBuilder);

        var source = SourceCodeBuilder
            .Create()
            .FromFile(executablePath)
            .Build();

        var process = engine.CreateProcess(this, source);
        var exitCode = process.Start();

        if (exitCode != 0)
            throw new Exception("Ошибка выполнения скрипта");
    }

    private static HostedScriptEngine CreateEngine(string librariesPath, Action<ExecutionContext> engineBuilder)
    {
        var builder = DefaultEngineBuilder
            .Create()
            .SetDefaultOptions()
            .UseImports()
            .SetupEnvironment(e =>
            {
                e.AddStandardLibrary();
                e.AddAssembly(typeof(OneScriptExecutor).Assembly);
                engineBuilder.Invoke(e);
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
        return _args;
    }
}