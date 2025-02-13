using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels.TechLogSettings;

namespace OnecMonitor.Server.Controllers;

public class TechLogSettingsController(AppDbContext appDbContext, IMapper mapper) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await appDbContext.TechLogSettings
            .ProjectTo<TechLogSettingsEditViewModel>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        return View(settings ?? new TechLogSettingsEditViewModel());
    }
    
    [HttpPost]
    public async Task<IActionResult> Save(TechLogSettingsEditViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = vm.Id == Guid.Empty;

        if (!vm.Enabled)
        {
            ModelState.Remove(nameof(vm.ClickHouseHost));
            ModelState.Remove(nameof(vm.ClickHouseDatabase));
            ModelState.Remove(nameof(vm.ClickHousePort));
            ModelState.Remove(nameof(vm.ClickHouseUser));
            ModelState.Remove(nameof(vm.ClickHousePassword));
        }
        
        if (!ModelState.IsValid)
            return View("Index", vm);
        
        var model = isNew ? new TechLogSettings
        {
            Id = Guid.NewGuid()
        } : await appDbContext.TechLogSettings.FirstOrDefaultAsync(i => i.Id == vm.Id, cancellationToken);
        
        if (model == null)
            return NotFound();

        appDbContext.Entry(model).State = isNew ? EntityState.Added : EntityState.Modified;
        
        mapper.Map(vm, model);
        
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
}