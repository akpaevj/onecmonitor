using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels.Log;
using OnecMonitor.Server.ViewModels.Log.Index;
using System.Runtime.InteropServices;
using System.Threading;

namespace OnecMonitor.Server.Controllers
{
    public class LogTemplatesController : Controller
    {
        private readonly AppDbContext _appDbContext;

        public LogTemplatesController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var logTemplates = await _appDbContext.LogTemplates.AsNoTracking().ToListAsync(cancellationToken);

            var viewModel = new LogTemplatesIndexViewModel
            {
                Items = logTemplates.Select(c => new LogTemplatesListItemViewModel()
                {
                    Id = c.Id,
                    Name = c.Name,
                }).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Edit(Guid id, bool copy = false, CancellationToken cancellationToken = default)
        {
            var viewModel = new LogTemplateEditViewModel();

            if (id != Guid.Empty)
            {
                var item = await _appDbContext.LogTemplates.AsNoTracking().SingleAsync(c => c.Id == id, cancellationToken);

                viewModel.Id = copy ? Guid.Empty : item!.Id;
                viewModel.Name = item.Name + (copy ? " (copy)" : "");
                viewModel.Content = item!.Content;

                if (copy)
                    HttpContext.Request.RouteValues.Remove("copy");

                return View(viewModel);
            }
            else
                return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, LogTemplateEditViewModel log, CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
            {
                var model = new LogTemplate()
                {
                    Id = Guid.NewGuid(),
                    Name = log.Name,
                    Content = log.Content?.Trim() ?? ""
                };

                await _appDbContext.LogTemplates.AddAsync(model, cancellationToken);
            }
            else
            {
                var model = await _appDbContext.LogTemplates.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)!;

                if (model is not null)
                {
                    model.Name = log.Name;
                    model.Content = log.Content?.Trim() ?? "";
                }
            }

            await _appDbContext.SaveChangesAsync(cancellationToken);

            return Redirect("/LogTemplates");
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var logTemplate = _appDbContext.LogTemplates.FirstOrDefault(c => c.Id == id);

            _appDbContext.LogTemplates.Remove(logTemplate!);

            await _appDbContext.SaveChangesAsync();

            return Redirect("/LogTemplates");
        }
    }
}
