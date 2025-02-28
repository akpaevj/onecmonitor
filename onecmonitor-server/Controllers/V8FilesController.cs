using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.V8Files;

namespace OnecMonitor.Server.Controllers;

public class V8FilesController(AppDbContext appDbContext, IMapper mapper, IWebHostEnvironment webHostEnvironment) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(new V8FilesIndexViewModel
        {
            Items = await appDbContext.V8Files
                .ProjectTo<V8FileListItemViewModel>(mapper.ConfigurationProvider)
                .ToListAsync()
        });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        if (id == Guid.Empty)
            return View(new V8FileEditViewModel());
        
        var item = await appDbContext.V8Files.FindAsync(id);
        
        if (item == null)
            return NotFound();
        
        return View(new V8FileEditViewModel
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
    public async Task<IActionResult> Save(Guid id, V8FileEditViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = id == Guid.Empty;
        
        if (!isNew)
            ModelState.Remove(nameof(V8FileEditViewModel.File));
        
        if (!ModelState.IsValid)
            return View("Edit", vm);
        
        var model = isNew ? new V8File
        {
            Id = Guid.NewGuid()
        } : await appDbContext.V8Files.FindAsync([id], cancellationToken);
        
        if (model == null)
            return NotFound();
        
        if (isNew)
        {
            // save file
            var extension = Path.GetExtension(vm.File.FileName);
            
            if (extension.Equals(".CF", StringComparison.InvariantCultureIgnoreCase))
                model.IsConfiguration = true;
            else if (extension.Equals(".CFE", StringComparison.InvariantCultureIgnoreCase))
                model.IsExtension = true;
            else if (extension.Equals(".CFU", StringComparison.InvariantCultureIgnoreCase))
                model.IsUpdate = true;
            else if (extension.Equals(".EPF", StringComparison.InvariantCultureIgnoreCase))
                model.IsExternalDataProcessor = true;
            else
                ModelState.AddModelError(nameof(V8FileEditViewModel.File), "Invalid file format");
            
            if (!ModelState.IsValid)
                return View("Edit", vm);
            
            var fileName = $"{vm.Name}_{vm.Version}{extension}";
            var path = Path.Combine(webHostEnvironment.ContentRootPath, "Data", fileName);
        
            if (System.IO.File.Exists(path))
                return View("Error", new ErrorViewModel($"File {path} already exists."));
                
            await using var stream = new FileStream(path, FileMode.Create);
            await vm.File.CopyToAsync(stream, cancellationToken);
                
            model.DataPath = path;
            
            await appDbContext.V8Files.AddAsync(model, cancellationToken);
        }
        
        model.Name = vm.Name;
        model.Version = vm.Version;
        
        await appDbContext.SaveChangesAsync(cancellationToken);
        
        return RedirectToAction("Index");
    }
    
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await appDbContext.V8Files.FindAsync(
            [id], 
            cancellationToken: cancellationToken);

        if (System.IO.File.Exists(item!.DataPath))
            System.IO.File.Delete(item.DataPath);
        
        appDbContext.V8Files.Remove(item!);
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
}