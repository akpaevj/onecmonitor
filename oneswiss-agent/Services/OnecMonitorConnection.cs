using OneSwiss.Agent.Models;
using OneSwiss.Common;
using OneSwiss.Common.DTO;

namespace OneSwiss.Agent.Services
{
    public class OnecMonitorConnection : ServerConnection
    {
        private readonly string _host;
        private readonly int _port;
        private readonly AgentInstance _agent;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public OnecMonitorConnection(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            IHostApplicationLifetime hostApplicationLifetime, 
            ILogger<OnecMonitorConnection> logger) : base(logger)
        {
            _hostApplicationLifetime = hostApplicationLifetime;

            using var scope = serviceProvider.CreateAsyncScope();
            using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _agent = appDbContext.AgentInstance.FirstOrDefault()!;

            _host = configuration.GetValue("OnecMonitor:Host", "0.0.0.0");
            _port = configuration.GetValue("OnecMonitor:Port", 7001);
        }

        public async Task Start(bool mainConnection = false)
        {
            await Start(_host, _port, async () =>
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