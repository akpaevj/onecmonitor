using Microsoft.AspNetCore.Mvc;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewComponents
{
    public class LockWaitingGraphMemberDetails : ViewComponent
    {
        public IViewComponentResult Invoke(LockWaitingGraphMember member)
            => View(member);
    }
}
