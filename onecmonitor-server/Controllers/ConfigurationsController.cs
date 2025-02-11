using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OnecMonitor.Server;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.Configurations;

namespace OnecMonitor.Server.Controllers;

public class ConfigurationsController(AppDbContext appDbContext, IMapper mapper, IWebHostEnvironment webHostEnvironment) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(new ConfigurationsIndexViewModel
        {
            Items = await appDbContext.Configurations
                .ProjectTo<ConfigurationListItemViewModel>(mapper.ConfigurationProvider)
                .ToListAsync()
        });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        if (id == Guid.Empty)
            return View(new ConfigurationEditViewModel());
        
        var item = await appDbContext.Configurations.FindAsync(id);
        
        if (item == null)
            return NotFound();
        
        return View(new ConfigurationEditViewModel
        {
            Id = item.Id,
            Name = item.Name,
            Version = item.Version
        });
    }

    [HttpPost]
    [RequestFormLimits(
        MultipartBodyLengthLimit = int.MaxValue,
        ValueLengthLimit = int.MaxValue)
    ]
    public async Task<IActionResult> Save(Guid id, ConfigurationEditViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = id == Guid.Empty;

        if (!isNew)
            ModelState.Remove(nameof(ConfigurationEditViewModel.File));
        
        if (!ModelState.IsValid)
            return View("Edit", vm);
        
        var model = isNew ? new V8Configuration
        {
            Id = Guid.NewGuid()
        } : await appDbContext.Configurations.FindAsync([id], cancellationToken);
        
        if (model == null)
            return NotFound();

        if (isNew)
        {
            // save file
            var extension = Path.GetExtension(vm.File.FileName);
            var fileName = $"{vm.Name}_{vm.Version}{extension}";
            var path = Path.Combine(webHostEnvironment.ContentRootPath, "Data", fileName);

            if (System.IO.File.Exists(path))
                return View("Error", new ErrorViewModel($"File {path} already exists."));
                
            await using var stream = new FileStream(path, FileMode.Create);
            await vm.File.CopyToAsync(stream, cancellationToken);
                
            model.DataPath = path;
            
            await appDbContext.Configurations.AddAsync(model, cancellationToken);

            if (extension.Equals(".CF", StringComparison.InvariantCultureIgnoreCase))
                model.IsConfiguration = true;
            else if (extension.Equals(".CFE", StringComparison.InvariantCultureIgnoreCase))
                model.IsExtension = true;
            else if (extension.Equals(".CFU", StringComparison.InvariantCultureIgnoreCase))
                model.IsUpdate = true;
        }

        model.Name = vm.Name;
        model.Version = vm.Version;

        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
    
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await appDbContext.Configurations.FindAsync(
            [id], 
            cancellationToken: cancellationToken);

        if (System.IO.File.Exists(item!.DataPath))
            System.IO.File.Delete(item.DataPath);
        
        appDbContext.Configurations.Remove(item!);
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
}