using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OneSwiss.Server.Hubs;

[AllowAnonymous]
public class AgentConnectionsHub : Hub;