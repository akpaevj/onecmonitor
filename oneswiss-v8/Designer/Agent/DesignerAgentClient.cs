using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Channels;
using OneSwiss.V8.Designer.Agent.Models;
using Renci.SshNet;

namespace OneSwiss.V8.Designer.Agent;

public sealed class DesignerAgentClient : IDisposable
{
    private readonly SshClient _client;
    private readonly string _host;
    private readonly string _password;
    private readonly int _port;
    private readonly string _username;

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

    private Channel<DesignerAgentMessage> MessagesChannel { get; } = Channel.CreateUnbounded<DesignerAgentMessage>();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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

    public async Task Connect(CancellationToken cancellationToken)
    {
        await _client.ConnectAsync(cancellationToken);
        _shellStream = _client.CreateShellStreamNoTerminal();

        // Пропустим приветственную шляпу
        await WaitDataAvailable();
        _shellStream.Read();
        await _shellStream.FlushAsync(cancellationToken);

        WriteCommand("options set --show-prompt=no");
        await WaitDataAvailable();
        _shellStream.Read();

        _ = StartReadLoop(cancellationToken);

        WriteCommand("options set --output-format=json");
        await MessagesChannel.EnsureNextSuccess();
    }

    public async Task EnableProgressNotification()
    {
        WriteCommand("options set --notify-progress=yes");
        await MessagesChannel.EnsureNextSuccess();
    }

    public async Task DisableProgressNotification()
    {
        WriteCommand("options set --notify-progress=no");
        await MessagesChannel.EnsureNextSuccess();
    }

    public async Task ConnectIb()
    {
        WriteCommand("common connect-ib");
        await MessagesChannel.EnsureNextSuccess();
    }

    public async Task LoadCfg(string path)
    {
        WriteCommand($"config load-cfg --file \"{path}\"");
        await MessagesChannel.EnsureNextSuccess();
    }

    public async Task<string> DumpConfiguration()
    {
        var path = Guid.NewGuid().ToString();
        var listFilePath = Path.GetTempFileName();

        try
        {
            Directory.CreateDirectory(path);

            File.WriteAllText(listFilePath, "Configuration");

            WriteCommand($"config dump-config-to-files --dir=\"{path}\" --list-file=\"{listFilePath}\"");
            var messages = await MessagesChannel.ReadTillSuccess();

            return File.ReadAllText(Path.Combine(path, "Configuration.xml"));
        }
        catch
        {
            throw;
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            if (File.Exists(listFilePath))
                File.Delete(listFilePath);
        }
    }

    public void UpdateDbCfg()
    {
        WriteCommand("config update-db-cfg --dynamic-disable --server --session-terminate=force");
    }

    public async Task LoadExtension(string path, string extensionName)
    {
        WriteCommand($"config load-cfg --file=\"{path}\" --extension=\"{extensionName}\"");
        await MessagesChannel.EnsureNextSuccess();
    }

    public void UpdateDbCfgExtension(string extensionName)
    {
        WriteCommand(
            $"config update-db-cfg --extension=\"{extensionName}\" --dynamic-disable --server --session-terminate=force");
    }

    public async Task DeleteExtension(string extensionName)
    {
        WriteCommand($"config extensions delete --extension=\"{extensionName}\"");
        await MessagesChannel.EnsureNextSuccess();
    }

    public async Task DeleteAllExtensions()
    {
        WriteCommand("config extensions delete --all-extensions");
        await MessagesChannel.EnsureNextSuccess();
    }

    public async Task<ExtensionInfo> GetExtension(string name)
    {
        WriteCommand($"config extensions properties get --extension={name}");
        return await MessagesChannel.ReadNextMessage<ExtensionInfo>();
    }

    public async Task<List<ExtensionInfo>> GetAllExtensions()
    {
        WriteCommand("config extensions properties get --all-extensions");
        var properties = await MessagesChannel.ReadNextMessage<List<ExtensionPropertiesMessage>>();
        return properties.Select(c => c.Body).ToList();
    }

    public void DisconnectIb()
    {
        WriteCommand("common disconnect-ib");
    }

    public void Shutdown()
    {
        WriteCommand("common shutdown");
    }

    public async Task ReadMessagesTillSuccess(Action<DesignerAgentMessage> handler)
    {
        while (true)
        {
            var next = await MessagesChannel.EnsureNextNotError();

            if (next.Type == "success")
                break;

            handler.Invoke(next);
        }
    }

    private async Task WaitDataAvailable()
    {
        while (!_shellStream.DataAvailable)
            await Task.Delay(100);
    }

    private void WriteCommand(string command)
    {
        _shellStream.WriteLine(command);
    }

    private async Task StartReadLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await WaitDataAvailable();

            var data = _shellStream.Read();

            var response = JsonSerializer.Deserialize<DesignerAgentMessage[]>(data);

            if (response is null)
                throw new Exception("Failed to deserialize designer agent response");

            foreach (var message in response)
                await MessagesChannel.Writer.WriteAsync(message, cancellationToken);
        }
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _client.Dispose();
        MessagesChannel.Writer.Complete();
    }
}