using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using OneScript.DebugServices;
using OneScript.Sources;
using OneScript.StandardLibrary;
using OneSwiss.V8.Platform;
using ScriptEngine;
using ScriptEngine.HostedScript;
using ScriptEngine.Hosting;
using ScriptEngine.Machine;
using ExecutionContext = ScriptEngine.Machine.ExecutionContext;

namespace OneSwiss.OneScript;

public class OneScriptExecutor : IHostApplication
{
    public EventHandler<(string Message, MessageStatusEnum Status)>? OnEcho = null;
    public EventHandler<Exception>? OnError = null;
    private string[] _args;
    
    public void ExecutePackageScript(string path, string executable, string[] args, Action<ExecutionContext> engineBuilder, bool debugMode = false)
    {
        IDebugController? debugController = null;

        try
        {
            if (debugMode)
            {
                var debugServer = new TcpDebugServer(2801);
                debugController = debugServer.CreateDebugController();
            }

            _args = args;
            var executablePath = Path.Combine(path, executable);
            var librariesPath = Path.Combine(path, "oscript_modules");

            using var engine = CreateEngine(librariesPath, engineBuilder, debugController);

            var source = SourceCodeBuilder
                .Create()
                .FromFile(executablePath)
                .Build();

            var process = engine.CreateProcess(this, source);
            var exitCode = process.Start();
            
            debugController?.NotifyProcessExit(exitCode);

            if (exitCode != 0)
                throw new Exception("Ошибка выполнения скрипта");
        }
        finally
        {
            debugController?.Dispose();
        }
    }

    private static HostedScriptEngine CreateEngine(
        string librariesPath, 
        Action<ExecutionContext> engineBuilder, 
        IDebugController? debugController = null)
    {
        var builder = DefaultEngineBuilder
            .Create()
            .SetDefaultOptions()
            .UseImports();

        if (debugController != null)
            builder.WithDebugger(debugController);

        builder.SetupEnvironment(e =>
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