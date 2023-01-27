using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OnecMonitor.Server.ViewModels;

namespace OnecMonitor.Server.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            var viewModel = new ErrorViewModel()
            {
                Message = exceptionHandlerPathFeature?.Error.Message ?? string.Empty
            };

            return View(viewModel);
        }
    }
}
