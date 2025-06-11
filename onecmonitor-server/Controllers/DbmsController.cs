using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Common.Models;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels.Dbms;

namespace OnecMonitor.Server.Controllers;

public class DbmsController(AppDbContext appDbContext, IMapper mapper) : Controller
{
    public async Task<IActionResult> Index()
        => View(await appDbContext.Dbms.ProjectTo<DbmsListItemViewModel>(mapper.ConfigurationProvider).ToListAsync());
    
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var vm = id != Guid.Empty
            ? await appDbContext.Dbms
                .AsNoTracking()
                .ProjectTo<DbmsEditViewModel>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            : new DbmsEditViewModel();

        if (vm == null)
            return NotFound();
        
        return View(PrepareViewModel(vm));
    }
    
    public async Task<IActionResult> Save(DbmsEditViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = vm.Id == Guid.Empty;
        
        if (!ModelState.IsValid)
            return View("Edit", PrepareViewModel(vm));
        
        var model = isNew ? new Dbms
        {
            Id = Guid.NewGuid()
        } : await appDbContext.Dbms.FirstOrDefaultAsync(i => i.Id == vm.Id, cancellationToken);
        
        if (model == null)
            return NotFound();

        if (isNew)
            appDbContext.Entry(model).State = EntityState.Added;

        mapper.Map(vm, model);
        
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
    
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await appDbContext.Dbms.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        
        if (item == null)
            return NotFound();
        
        appDbContext.Entry(item).State = EntityState.Deleted;
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
    
    private DbmsEditViewModel PrepareViewModel(DbmsEditViewModel vm)
    {
        vm.Types = UiHelper.SelectListFromEnum<DbmsType>(vm.Type);
        return vm;
    }
}