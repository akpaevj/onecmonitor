using OnecMonitor.Common;
using OnecMonitor.Common.Models;
using MessagePack;
using System.Net.Sockets;
using System.Net;
using Microsoft.Extensions.Logging;

namespace OnecMonitor.Agent.Services
{
    public class ServerConnection : OnecMonitorConnection
    {
        private readonly AgentInstance _agentInstance;
        private readonly string _host;
        private readonly int _port;

        private readonly SemaphoreSlim _connectingSemaphore = new(1);
        private readonly ILogger<ServerConnection> _logger;

        public ServerConnection(IServiceProvider serviceProvider) : base(false)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            using var scope = serviceProvider.CreateAsyncScope();
            using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _agentInstance = appDbContext!.AgentInstance.FirstOrDefault();

            var instanceName = configuration.GetValue("Agent:InstanceName", Environment.MachineName);
            if (string.IsNullOrEmpty(instanceName))
                instanceName = Environment.MachineName;

            if (_agentInstance == null) 
            {
                _agentInstance = new AgentInstance
                {
                    Id = Guid.NewGuid(),
                    InstanceName = instanceName,
                    UtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds
                };

                appDbContext!.AgentInstance.Add(_agentInstance);
                appDbContext.SaveChanges();
            }
            else if (_agentInstance!.InstanceName != instanceName)
            {
                _agentInstance.InstanceName = instanceName;

                appDbContext!.AgentInstance.Update(_agentInstance);
                appDbContext.SaveChanges();
            }

            _host = configuration.GetValue("OnecMonitor:Host", "0.0.0.0") ?? "0.0.0.0";
            _port = configuration.GetValue("OnecMonitor:Port", 7001);

            _logger = serviceProvider.GetRequiredService<ILogger<ServerConnection>>();

            RunSteamLoops();
        }

        public async Task SubscribeForCommands(CancellationToken cancellationToken)
        {
            await WriteMessage(MessageType.SubscribingForCommands, null, cancellationToken);
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
            => await WriteMessage(MessageType.TechLogEventContent, item, null, cancellationToken);

        protected async override Task StartReadingFromStream(CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    await Reconnect(cancellationToken);

                    _logger.LogTrace("Start reading from network stream");

                    await base.StartReadingFromStream(cancellationToken);

                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NotConnected)
                {

                }
            }
            while (!cancellationToken.IsCancellationRequested);
        }

        protected async override Task StartWritingToStream(CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    await Reconnect(cancellationToken);

                    _logger.LogTrace("Start writing to network stream");

                    await base.StartWritingToStream(cancellationToken);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NotConnected)
                {
                    
                }
            }
            while (!cancellationToken.IsCancellationRequested);
        }

        private async Task Reconnect(CancellationToken cancellationToken)
        {
            await _connectingSemaphore.WaitAsync(cancellationToken);

            if (_socket?.Connected == true)
                return;

            _logger.LogTrace($"Trying connect to {_host}:{_port}");

            _socket?.Dispose();

            var addresses = await Dns.GetHostAddressesAsync(_host, AddressFamily.InterNetwork, cancellationToken);
            if (addresses.Length == 0)
                throw new Exception("Couldn't resolve server address");
            var endPoint = new IPEndPoint(addresses[0], _port);

            _logger.LogTrace($"Server's resolved address: {endPoint.Address}");

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };
            await _socket.ConnectAsync(endPoint, cancellationToken);

            _stream = new NetworkStream(_socket);

            await WriteMessageToStream(MessageType.AgentInfo, _agentInstance, cancellationToken);

            _logger.LogInformation("Connected to the server");

            _connectingSemaphore.Release();
        }
    }
}