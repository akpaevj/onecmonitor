using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OneSTools.Common.Designer.Agent.Models;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace OneSTools.Common.Designer.Agent;

public class DesignerAgentClient : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly SshClient _client;
    
    private ShellStream _shellStream = null!;

    public DesignerAgentClient(string user, string password, string host = "localhost", int port = 1543)
    {
        _host = host;
        _port = port;
        _username = user;
        _password = password;
        
        var connectionInfo = new ConnectionInfo(
            _host,
            _port,
            _username,
            new PasswordAuthenticationMethod(_username, _password))
        {
            Timeout = TimeSpan.FromHours(12),
            MaxSessions = 1
        };
        
        _client = new SshClient(connectionInfo);
    }
    
    public async Task<bool> WaitAgentAvailable(TimeSpan timeout)
    {
        using var socket = new TcpClient();
        var endTime = DateTime.Now.Add(timeout);
        
        while (DateTime.Now < endTime)
        {
            try
            {
                await socket.ConnectAsync(_client.ConnectionInfo.Host, _client.ConnectionInfo.Port);
                return true;
            }
            catch (Exception)
            {
                // ignored
            }

            await Task.Delay(100);
        }
        
        return false;
    }

    public async Task<SftpClient> GetSftpClient(CancellationToken cancellationToken)
    {
        var sftp = new SftpClient(_host, _port, _username, _password);
        await sftp.ConnectAsync(cancellationToken);
        
        return sftp;
    }

    public async Task Connect()
    {
        _client.Connect();
        _shellStream = _client.CreateShellStreamNoTerminal();

        await _shellStream.WaitDataAvailable();
        _shellStream.Read();
        await _shellStream.FlushAsync();

        _shellStream.WriteLine("options set --show-prompt=no");
        await _shellStream.WaitDataAvailable();
        _shellStream.Read();
        await _shellStream.FlushAsync();
        
        await _shellStream.WriteCommand("options set --output-format=json");
    }
    
    public async Task EnableProgressNotification()
        => await _shellStream.WriteCommand("options set --notify-progress=yes");
    
    public async Task DisableProgressNotification()
        => await _shellStream.WriteCommand("options set --notify-progress=no"); 

    public async Task ConnectIb()
        => await _shellStream.WriteCommand("common connect-ib");

    public async Task<ExtensionInfo> GetExtension(string name)
    {
        var response = await _shellStream.WriteCommand($"config extensions properties get --extension={name}");
        return JsonSerializer.Deserialize<ExtensionInfo>(response.First().Body.RootElement.ToString())!;
    }

    public async Task<List<ExtensionInfo>> GetAllExtensions()
    {
        var response = await _shellStream.WriteCommand("config extensions properties get --all-extensions");
        
        return JsonSerializer
            .Deserialize<List<ExtensionPropertiesMessage>>(response.First().Body.RootElement.ToString())!
            .Select(i => i.Body)
            .ToList();
    }

    public async Task DisconnectIb()
        => await _shellStream.WriteCommand("common disconnect-ib");

    public async Task Shutdown()
        => await _shellStream.WriteCommand("common shutdown");
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            _client.Dispose();
    }
}