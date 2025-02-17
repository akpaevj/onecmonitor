using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using OneSTools.Common.Extensions;
using OneSTools.Common.Platform;

namespace OnecMonitor.Agent.Services.InfoBases;

public class RasHolder : IDisposable
{
    private readonly List<Process> _processes = [];
    private readonly Dictionary<int, RasService> _rasServiceModels = [];
    
    public List<RasService> GetRasServices()
    {
        var services = V8Services.GetRasServices();
        services.AddRange(_rasServiceModels.Values.ToList());

        return services;
    }
    
    public RasService GetActiveRasForRagent(RagentService ragent)
    {
        var service = GetRasServices()
            .Where(c => c.IsActive)
            .FirstOrDefault(c => c.RagentHost.IsLocalHost() && c.RagentPort == ragent.Port);
        
        return service ?? StartRasForRagent(ragent);
    }
    
    private RasService StartRasForRagent(RagentService ragent)
    {
        if (!ragent.Platform.HasRas)
            throw new Exception($"Платформа {ragent.Platform} не содержит компоненту RAS");

        var port = FindFreePort();
        
        var psi = new ProcessStartInfo
        {
            FileName = ragent.Platform.RasPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Arguments = $"cluster --port={port} localhost:{ragent.Port}"
        };
        
        var process = Process.Start(psi);

        if (process!.HasExited)
        {
            using var stream = process.StandardError;
            var error = stream.ReadToEnd();
            process.Dispose();
            
            throw new Exception($"Ошибка запуска RAS для агента кластера: {error}");
        }

        process.Exited += (_, _) =>
        {
            _rasServiceModels.Remove(process.Id);
            _processes.Remove(process);
        };
        
        _processes.Add(process!);

        var serviceModel = new RasService
        {
            Name = "RAS (OnecMonitor)",
            Platform = ragent.Platform,
            IsActive = true,
            Port = port,
            RagentHost = "localhost",
            RagentPort = ragent.Port
        };
        
        _rasServiceModels.Add(process.Id, serviceModel);
        
        // wait output
        Thread.Sleep(3);
        
        return serviceModel;
    }

    private static int FindFreePort()
    {
        int port;
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        try
        {
            var localEp = new IPEndPoint(IPAddress.Any, 0);
            socket.Bind(localEp);
            localEp = (IPEndPoint)socket.LocalEndPoint!;
            port = localEp.Port;
        }
        finally
        {
            socket.Close();
        }
        
        return port;
    }
    
    private void ReleaseUnmanagedResources()
    {
        _processes.ForEach(c => c.Dispose());
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~RasHolder()
    {
        ReleaseUnmanagedResources();
    }
}