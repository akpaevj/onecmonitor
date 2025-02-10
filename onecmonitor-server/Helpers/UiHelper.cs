using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.Helpers;

public static class UiHelper
{
    public static async Task<SelectList> SelectListFrom<T1>(
        IQueryable<T1> items,
        Func<T1, string> textSelector,
        Guid selectedValue,
        CancellationToken cancellationToken) where T1 : DatabaseObject
    {
        var list = await items.ToListAsync(cancellationToken);
        var selectListItems = list.Select(i => new { Id = i.Id.ToString(), Name = textSelector(i) }).ToList();
        selectListItems.Add(new { Id = Guid.Empty.ToString(), Name = "Please choose item" });
        
        return new SelectList(
            selectListItems,
            "Id",
            "Name",
            selectedValue.ToString());
    }

    public static async Task<List<SelectableItemViewModel>> SelectableItemsFrom<T1>(
        IQueryable<T1> queryable,
        List<SelectableItemViewModel> selectedItems,
        IMapper mapper,
        CancellationToken cancellationToken) where T1 : DatabaseObject
    {
        var allItems = await queryable.ToListAsync(cancellationToken);
        var availableItems = allItems
            .Where(i => selectedItems.FirstOrDefault(c => i.Id.ToString() == c.Id) == null).ToList();
        
        return mapper.Map<List<SelectableItemViewModel>>(availableItems);
    }

    public static async Task UpdateModelItems<T1>(
        IQueryable<T1> queryable,
        List<SelectableItemViewModel> vmItems,
        List<T1> modelItems,
        CancellationToken cancellationToken) where T1 : DatabaseObject
    {
        var ids = vmItems.Select(c => Guid.Parse(c.Id));
        
        var newItems = await queryable
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);
        
        // add new
        newItems
            .Where(c => !modelItems.Contains(c))
            .ToList()
            .ForEach(modelItems.Add);
        
        // remove deleted
        modelItems
            .Where(c => !newItems.Contains(c))
            .ToList()
            .ForEach(c => modelItems.Remove(c));
    }
}