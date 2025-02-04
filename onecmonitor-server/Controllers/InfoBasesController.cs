using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels.InfoBases;
using OnecMonitor.Server.ViewModels.InfoBases.Index;

namespace OnecMonitor.Server.Controllers;

public class InfoBasesController : Controller
{
    private readonly AppDbContext _appDbContext;
    
    public InfoBasesController(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }
    
    // GET
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new InfoBasesIndexViewModel
        {
            Items = await _appDbContext.InfoBases.Include(c => c.Agent).Select(c => new InfoBaseListItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Agent = c.Agent.InstanceName,
                PublishAddress = c.PublishAddress,
                AdminUser = c.AdminUser,
                AdminPassword = c.AdminPassword
            }).ToListAsync(cancellationToken),
        });
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var vm = new InfoBaseEditViewModel();
        
        if (id != Guid.Empty)
        {
            var model = await _appDbContext.InfoBases.FindAsync([id], cancellationToken);
            vm.Id = model!.Id;
            vm.Name = model.Name;
            vm.AgentId = model.AgentId;
            vm.AdminUser = model.AdminUser;
            vm.AdminPassword = model.AdminPassword;
            vm.PublishAddress = model.PublishAddress;
        }
        
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
        } : await _appDbContext.InfoBases.FindAsync([id], cancellationToken);
        
        if (model == null)
            return NotFound();

        if (isNew)
            await _appDbContext.InfoBases.AddAsync(model, cancellationToken);

        model.Name = vm.Name;
        model.AgentId = vm.AgentId;
        model.AdminUser = vm.AdminUser;
        model.AdminPassword = vm.AdminPassword;
        model.PublishAddress = vm.PublishAddress;

        await _appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
    
    private async Task<InfoBaseEditViewModel> InitVewModel(InfoBaseEditViewModel vm, CancellationToken cancellationToken)
    {
        var agents = await _appDbContext.Agents.ToListAsync(cancellationToken);
        vm.Agents = new SelectList(
            agents.Select(c => new { Id = c.Id.ToString(), Name = c.InstanceName }), 
            nameof(InfoBase.Id),
            nameof(InfoBase.Name),
            vm.AgentId.ToString());

        return vm;
    }
    
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await _appDbContext.InfoBases.FindAsync(
            [id], 
            cancellationToken: cancellationToken);
        
        _appDbContext.Entry(item!).State = EntityState.Deleted;
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
}