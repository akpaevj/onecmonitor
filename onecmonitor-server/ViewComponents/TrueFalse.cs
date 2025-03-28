using Microsoft.AspNetCore.Mvc;

namespace OnecMonitor.Server.ViewComponents;

public class TrueFalse : ViewComponent
{
    public IViewComponentResult Invoke(bool value)
        => View(value);
}