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
    
    public void ExecuteScriptMethod(string path,
        string executable,
        string methodName,
        IValue[] args,
        Action<ExecutionContext> engineBuilder,
        bool debugMode = false)
    {
        IDebugController? debugController = null;

        try
        {
            if (debugMode)
            {
                var debugServer = new TcpDebugServer(2801);
                debugController = debugServer.CreateDebugController();
            }

            var executablePath = Path.Combine(path, executable);
            var librariesPath = Path.Combine(path, "oscript_modules");

            using var engine = CreateEngine(librariesPath, engineBuilder, debugController);

            var source = SourceCodeBuilder
                .Create()
                .FromFile(executablePath)
                .Build();

            engine.Initialize();
            
            engine.SetGlobalEnvironment(this, source);

            if (debugMode)
            {
                debugController!.Init();
                debugController.Wait();
            }
            
            var bslProcess = engine.Services.Resolve<BslProcessFactory>().NewProcess();
            var compiledModule = engine.GetCompilerService().Compile(source, bslProcess);
            var contextInstance = engine.Engine.NewObject(compiledModule, bslProcess);

            var mn = contextInstance.GetMethodNumber(methodName);
            
            contextInstance.CallAsProcedure(mn, args, bslProcess);
        }
        finally
        {
            debugController?.Dispose();
        }
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
        
        var engine = builder.Build();

        return new HostedScriptEngine(engine);
    }
}