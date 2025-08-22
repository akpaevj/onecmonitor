using OneSwiss.Agent.Models;
using OneSwiss.Common;
using OneSwiss.Common.DTO;

namespace OneSwiss.Agent.Services
{
    public class OneSwissConnection : ServerConnection
    {
        private readonly string _serverAddress;
        private bool _authRequired;
        private readonly int _port;
        private readonly AgentInstance _agent;
        private readonly TokenRetriever _tokenRetriever;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public OneSwissConnection(
            TokenRetriever tokenRetriever,
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            IHostApplicationLifetime hostApplicationLifetime, 
            ILogger<OneSwissConnection> logger) : base(logger)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _tokenRetriever = tokenRetriever;

            using var scope = serviceProvider.CreateAsyncScope();
            using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _agent = appDbContext.AgentInstance.FirstOrDefault()!;

            _serverAddress = configuration.GetValue("Server", "ws://localhost:7002");
            _authRequired = configuration.GetValue("Auth:Required", false);
        }

        public async Task Start(bool mainConnection = false)
        {
            var token = _authRequired ? await _tokenRetriever.GetValidTokenAsync() : null;
            
            await Start(_serverAddress, token, async () =>
            {
                await WriteMessageToStream(MessageType.AgentInfo, 
                    new AgentInstanceDto
                    {
                        Id = _agent.Id,
                        InstanceName = _agent.InstanceName,
                        MainConnection = mainConnection,
                        UtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds
                    }, 
                    _hostApplicationLifetime.ApplicationStopping);
            }, _hostApplicationLifetime.ApplicationStopping);
        }
    }
}