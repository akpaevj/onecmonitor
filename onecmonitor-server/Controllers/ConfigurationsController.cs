using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OnecMonitor.Server;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.Configurations;

namespace OnecMonitor.Server.Controllers;

public class ConfigurationsController : Controller
{
    private readonly AppDbContext _appDbContext;
    private readonly IWebHostEnvironment _env;
    
    public ConfigurationsController(AppDbContext appDbContext, IWebHostEnvironment webHostEnvironment)
    {
        _appDbContext = appDbContext;
        _env = webHostEnvironment;
    }
    
    public async Task<IActionResult> Index()
    {
        return View(new ConfigurationsIndexViewModel
        {
            Items = await _appDbContext.Configurations.Select(c => new ConfigurationViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Version = c.Version,
                IsExtension = c.IsExtension
            }).ToListAsync()
        });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        if (id == Guid.Empty)
            return View(new ConfigurationViewModel());
        
        var item = await _appDbContext.Configurations.FindAsync(id);
        
        if (item == null)
            return NotFound();
        
        return View(new ConfigurationViewModel
        {
            Id = item.Id,
            Name = item.Name,
            Version = item.Version,
            IsExtension = item.IsExtension
        });
    }

    [HttpPost]
    [RequestFormLimits(
        MultipartBodyLengthLimit = int.MaxValue,
        ValueLengthLimit = int.MaxValue)
    ]
    public async Task<IActionResult> Save(Guid id, ConfigurationViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = id == Guid.Empty;

        if (!isNew)
            ModelState.Remove(nameof(ConfigurationViewModel.File));
        
        if (!ModelState.IsValid)
            return View("Edit", vm);
        
        var model = isNew ? new V8Configuration
        {
            Id = Guid.NewGuid()
        } : await _appDbContext.Configurations.FindAsync([id], cancellationToken);
        
        if (model == null)
            return NotFound();

        if (isNew)
        {
            // save file
            var fileName = $"{vm.Name}_{vm.Version}{Path.GetExtension(vm.File.FileName)}";
            var path = Path.Combine(_env.ContentRootPath, "Data", fileName);

            if (System.IO.File.Exists(path))
                return View("Error", new ErrorViewModel { Message = $"File {path} already exists." });
                
            await using var stream = new FileStream(path, FileMode.Create);
            await vm.File.CopyToAsync(stream, cancellationToken);
                
            model.DataPath = path;
            
            await _appDbContext.Configurations.AddAsync(model, cancellationToken);
        }

        model.Name = vm.Name;
        model.Version = vm.Version;
        model.IsExtension = vm.IsExtension;

        await _appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
    
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await _appDbContext.Configurations.FindAsync(
            [id], 
            cancellationToken: cancellationToken);

        if (System.IO.File.Exists(item!.DataPath))
            System.IO.File.Delete(item.DataPath);
        
        _appDbContext.Configurations.Remove(item!);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
}