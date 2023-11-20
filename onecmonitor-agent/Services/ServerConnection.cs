using OnecMonitor.Common;
using OnecMonitor.Common.Models;
using MessagePack;
using System.Net.Sockets;
using System.Net;
using Microsoft.Extensions.Logging;
using System.Threading;
using OnecMonitor.Common.TechLog;

namespace OnecMonitor.Agent.Services
{
    public class ServerConnection : OnecMonitorConnection
    {
        private readonly string _host;
        private readonly int _port;

        private readonly ILogger<ServerConnection> _logger;

        public AgentInstance AgentInstance { get; private set; }

        public ServerConnection(IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime) : base(false)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            using var scope = serviceProvider.CreateAsyncScope();
            using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AgentInstance = appDbContext!.AgentInstance.FirstOrDefault();

            var instanceName = configuration.GetValue("Agent:InstanceName", Environment.MachineName);
            if (string.IsNullOrEmpty(instanceName))
                instanceName = Environment.MachineName;

            if (AgentInstance == null) 
            {
                AgentInstance = new AgentInstance
                {
                    Id = Guid.NewGuid(),
                    InstanceName = instanceName,
                    UtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds
                };

                appDbContext!.AgentInstance.Add(AgentInstance);
                appDbContext.SaveChanges();
            }
            else if (AgentInstance!.InstanceName != instanceName)
            {
                AgentInstance.InstanceName = instanceName;

                appDbContext!.AgentInstance.Update(AgentInstance);
                appDbContext.SaveChanges();
            }

            _host = configuration.GetValue("OnecMonitor:Host", "0.0.0.0") ?? "0.0.0.0";
            _port = configuration.GetValue("OnecMonitor:Port", 7001);

            _logger = serviceProvider.GetRequiredService<ILogger<ServerConnection>>();

            Disconnected += () =>
            {
                _logger.LogWarning("Disconnected from the server");

                _ = RunLoops(hostApplicationLifetime.ApplicationStopping);
            };

            _ = RunLoops(hostApplicationLifetime.ApplicationStopping);
        }

        protected async Task RunLoops(CancellationToken cancellationToken)
        {
            await ConnectInLoop(cancellationToken);

            RunStreamLoops();

            _logger.LogTrace("Stream loops started");
        }

        public async Task SubscribeForCommands(CancellationToken cancellationToken)
        {
            await WriteMessage(MessageType.SubscribingForCommands, null, cancellationToken);

            _logger.LogTrace("SubscribingForCommands message is queued");
        }

        public async Task<long> GetLastFilePosition(Guid seanceId, Guid templateId, string folder, string file, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Last position in file requested");

            var request = new LastFilePositionRequest()
            {
                SeanceId = seanceId,
                TemplateId = templateId,
                Folder = folder,
                File = file
            };

            var response = await WriteMessageAndWaitResult(MessageType.LastFilePositionRequest, request, cancellationToken);

            if (response.Header.Type == MessageType.LastFilePosition)
                return MessagePackSerializer.Deserialize<long>(response.Data, null, cancellationToken);
            else
                throw new Exception("Received unexpected message type");
        }

        public async Task<List<TechLogSeanceDto>> GetTechLogSeances(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Tech log seacnes requested");

            var response = await WriteMessageAndWaitResult(MessageType.TechLogSeancesRequest, cancellationToken);

            if (response.Header.Type == MessageType.TechLogSeances)
                return MessagePackSerializer.Deserialize<List<TechLogSeanceDto>>(response.Data, null, cancellationToken);
            else
                throw new Exception("Received unexpected message type");
        }

        public async Task SendTechLogEventContent(TechLogEventContentDto item, CancellationToken cancellationToken)
        {
            //try
            //{
            //    if (!TechLogParser.TryParse(AgentInstance, item, out var item2))
            //    {
            //        var a = 1;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    var a = 1;
            //}

            await WriteMessage(MessageType.TechLogEventContent, item, null, cancellationToken);

            _logger.LogTrace("Event content message is queued");
        }

        private async Task ConnectInLoop(CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    await Reconnect(cancellationToken);

                    _logger.LogTrace("Connection to server is established");
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NotConnected)
                {
                    _logger.LogTrace("Failed to connect to the server");
                }

                if (_socket?.Connected == true)
                    break;
            }
            while (!cancellationToken.IsCancellationRequested);
        }

        private async Task Reconnect(CancellationToken cancellationToken)
        {
            _logger.LogTrace($"Trying connect to {_host}:{_port}");

            _socket?.Dispose();

            var addresses = await Dns.GetHostAddressesAsync(_host, AddressFamily.InterNetwork, cancellationToken);
            if (addresses.Length == 0)
                throw new Exception("Couldn't resolve server address");
            var endPoint = new IPEndPoint(addresses[0], _port);

            _logger.LogTrace($"Server's resolved address: {endPoint.Address}");

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
            };

            var _cts = new CancellationTokenSource();

            try
            {
                _cts.CancelAfter(10 * 1000);
                cancellationToken.Register(_cts.Cancel);

                var connectAsync = _socket.ConnectAsync(endPoint, cancellationToken);
                var connectTask = connectAsync.AsTask();
                await connectTask.WaitAsync(_cts.Token);
            }
            catch(Exception)
            {

            }

            _cts?.Dispose();

            if (!_socket.Connected)
                throw new SocketException((int)SocketError.NotConnected);
            else
            {
                _stream = new NetworkStream(_socket);
                _logger.LogInformation("Connected to the server");
                await WriteMessageToStream(MessageType.AgentInfo, AgentInstance, cancellationToken);
            }
        }
    }
}