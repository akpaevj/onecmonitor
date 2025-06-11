using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Dto.ErrorLoggingService;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.ErrorLoggingService;
using OnecMonitor.Server.ViewModels.ErrorLoggingService.Index;
using OnecMonitor.Server.ViewModels.TechLogSettings;

namespace OnecMonitor.Server.Controllers;

public class ErrorLoggingServiceController(
    AppDbContext dbContext, 
    IMapper mapper, 
    ILogger<ErrorLoggingServiceController> logger) : Controller
{
    
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await dbContext.ErrorReports.AsNoTracking().ToListAsync(cancellationToken);

        var reports = items
            .Select(c =>
            {
                var reportRoot = JsonSerializer.Deserialize<ReportRoot>(c.Report, ErrorReportsHelper.ReportSerializerOptions);
                
                return new ErrorLoggingServiceListItemViewModel
                {
                    Id = c.Id,
                    Configuration = reportRoot!.ConfigInfo.Name,
                    ConfigurationVersion = reportRoot.ConfigInfo.Version,
                    Date = reportRoot.Time,
                    PlatformVersion = reportRoot.ServerInfo.AppVersion,
                    UserName = reportRoot.SessionInfo.UserName
                };
            })
            .OrderByDescending(c => c.Date)
            .ToList();

        return View(new ErrorLoggingServiceIndexViewModel
        {
            Items = reports
        });
    }
    
    [HttpGet]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
    {
        var settings = await dbContext.ErrorLoggingServiceSettings
            .ProjectTo<ErrorLoggingServiceSettingsViewModel>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        return View(settings ?? new ErrorLoggingServiceSettingsViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> SaveSettings(ErrorLoggingServiceSettingsViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = vm.Id == Guid.Empty;

        if (!vm.Enabled)
            ModelState.Clear();
        
        if (!ModelState.IsValid)
            return View("Settings", vm);

        var model = isNew ? new ErrorLoggingServiceSettings()
        {
            Id = Guid.NewGuid()
        } : await dbContext.ErrorLoggingServiceSettings.FirstOrDefaultAsync(i => i.Id == vm.Id, cancellationToken);
        
        if (model == null)
            return NotFound();

        dbContext.Entry(model).State = isNew ? EntityState.Added : EntityState.Modified;
        
        mapper.Map(vm, model);
        
        await dbContext.SaveChangesAsync(cancellationToken);

        return View("Settings", vm);
    }
    
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var model = await dbContext.ErrorReports
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (model == null)
            return NotFound();
        
        return View(new ErrorReportViewModel
        {
            Report = JsonSerializer.Deserialize<ReportRoot>(model.Report, ErrorReportsHelper.ReportSerializerOptions)!,
            Screenshot = $"data:image/png;base64,{Convert.ToBase64String(model.Screenshot)}"
        });
    }
    
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.ErrorReports.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        
        dbContext.ErrorReports.Remove(item!);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
}