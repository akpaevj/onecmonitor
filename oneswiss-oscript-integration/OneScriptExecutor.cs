using System.Diagnostics.CodeAnalysis;
using OneScript.Contexts;
using OneScript.DebugServices;
using OneScript.Execution;
using OneScript.Sources;
using OneScript.StandardLibrary;
using ScriptEngine;
using ScriptEngine.HostedScript;
using ScriptEngine.Hosting;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;
using ScriptEngine.Machine.Debugger;
using ExecutionContext = ScriptEngine.Machine.ExecutionContext;

namespace OneSwiss.OneScript;

public class OneScriptExecutor : IHostApplication
{
    private string[] _args;
    public EventHandler<(string Message, MessageStatusEnum Status)>? OnEcho = null;
    public EventHandler<Exception>? OnError = null;

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

    public void ExecutePackageScript(string path, string executable, string[] args,
        Action<ExecutionContext> engineBuilder, bool debugMode = false)
    {
        IDebugger? debugger = null;

        if (debugMode)
        {
            var debugServer = new TcpDebugServer(2801);
            debugger = new DefaultDebugger(debugServer);
        }

        _args = args;
        var executablePath = Path.Combine(path, executable);
        var librariesPath = Path.Combine(path, "oscript_modules");

        using var engine = CreateEngine(librariesPath, engineBuilder, debugger);

        var source = SourceCodeBuilder
            .Create()
            .FromFile(executablePath)
            .Build();

        var process = engine.CreateProcess(this, source);
        var exitCode = process.Start();

        debugger?.NotifyProcessExit(exitCode);

        if (exitCode != 0)
            throw new Exception("Ошибка выполнения скрипта");
    }
    
    public void ExecuteScriptMethod(string path,
        string executable,
        string methodName,
        IValue[] args,
        Action<ExecutionContext> engineBuilder,
        bool debugMode = false)
    {
        DefaultDebugger? debugger = null;

        if (debugMode)
        {
            var debugServer = new TcpDebugServer(2801);
            debugger = new DefaultDebugger(debugServer);
        }

        var executablePath = Path.Combine(path, executable);
        var librariesPath = Path.Combine(path, "oscript_modules");

        using var engine = CreateEngine(librariesPath, engineBuilder, debugger);

        var source = SourceCodeBuilder
            .Create()
            .FromFile(executablePath)
            .Build();

        engine.Initialize();

        engine.SetGlobalEnvironment(this, source);

        if (debugMode)
        {
            debugger!.Start();
        }

        var bslProcess = engine.Services.Resolve<BslProcessFactory>().NewProcess();
        var compiledModule = engine.GetCompilerService().Compile(source, bslProcess);
        var contextInstance = engine.Engine.NewObject(compiledModule, bslProcess);

        var mn = contextInstance.GetMethodNumber(methodName);

        contextInstance.CallAsProcedure(mn, args, bslProcess);
    }
    
    public static IExecutableModule GetCompiledModule(string path, string executable, Action<ExecutionContext> engineBuilder)
    {
        var executablePath = Path.Combine(path, executable);
        var librariesPath = Path.Combine(path, "oscript_modules");

        using var engine = CreateEngine(librariesPath, engineBuilder);

        var source = SourceCodeBuilder
            .Create()
            .FromFile(executablePath)
            .Build();

        engine.Initialize();
        var bslProcess = engine.Services.Resolve<BslProcessFactory>().NewProcess();
        return engine.GetCompilerService().Compile(source, bslProcess);
    }

    public static HostedScriptEngine CreateEngine(
        string librariesPath,
        Action<ExecutionContext> engineBuilder,
        IDebugger? debugger = null)
    {
        var builder = DefaultEngineBuilder
            .Create()
            .SetDefaultOptions()
            .UseImports();

        if (debugger != null)
            builder.WithDebugger(debugger);

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
        
        var engine = builder.Build();

        return new HostedScriptEngine(engine);
    }
}