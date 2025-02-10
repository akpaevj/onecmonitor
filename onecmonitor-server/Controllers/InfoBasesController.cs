using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels.InfoBases;
using OnecMonitor.Server.ViewModels.InfoBases.Index;

namespace OnecMonitor.Server.Controllers;

public class InfoBasesController(AppDbContext appDbContext, IMapper mapper) : Controller
{
    // GET
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(new InfoBasesIndexViewModel
        {
            Items = await appDbContext.InfoBases
                .Include(c => c.Cluster)
                .ProjectTo<InfoBaseListItemViewModel>(mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken)
        });

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var vm = id == Guid.Empty
            ? new InfoBaseEditViewModel()
            : await appDbContext.InfoBases.Include(c => c.Cluster).ProjectTo<InfoBaseEditViewModel>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (vm == null)
            return NotFound();
        
        return View(await InitVewModel(vm, cancellationToken));
    }
    
    public async Task<IActionResult> Save(Guid id, InfoBaseEditViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = id == Guid.Empty;
        
        if (!ModelState.IsValid)
            return View("Edit", await InitVewModel(vm, cancellationToken));
        
        var model = isNew ? new InfoBase
        {
            Id = Guid.NewGuid()
        } : await appDbContext.InfoBases.FindAsync([id], cancellationToken);
        
        if (model == null)
            return NotFound();

        if (isNew)
            await appDbContext.InfoBases.AddAsync(model, cancellationToken);
        
        mapper.Map(vm, model);

        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
    
    private async Task<InfoBaseEditViewModel> InitVewModel(InfoBaseEditViewModel vm, CancellationToken cancellationToken)
    {
        var clusters = await appDbContext.Clusters.ToListAsync(cancellationToken);
        vm.Clusters = new SelectList(
            clusters.Select(c => new { Id = c.Id.ToString(), Name = c.Name }), 
            nameof(Cluster.Id),
            nameof(Cluster.Name),
            vm.ClusterId.ToString());

        return vm;
    }
    
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await appDbContext.InfoBases.FindAsync(
            [id], 
            cancellationToken: cancellationToken);
        
        appDbContext.Entry(item!).State = EntityState.Deleted;
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
}