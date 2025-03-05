using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.Helpers;

public static class UiHelper
{
    public static SelectList SelectListFromEnum<T1>(T1? selectedValue = null) where T1 : struct, Enum
    {
        var values = Enum.GetValues<T1>().ToList();
        var selectListItems = values.Select(i => new { Id = i.ToString(), Name = i.GetAttributeOfType<DisplayAttribute>()?.Name ?? i.ToString() }).ToList();
        selectListItems.Insert(0, new { Id = "", Name = "Выберите элемент" });
        
        return new SelectList(
            selectListItems,
            "Id",
            "Name",
            selectedValue == null ? "" : selectedValue);
    }
    
    public static async Task<SelectList> SelectListFrom<T1>(
        IQueryable<T1> items,
        Func<T1, string> textSelector,
        Guid? selectedValue,
        CancellationToken cancellationToken) where T1 : DatabaseObject
    {
        var list = await items.ToListAsync(cancellationToken);
        var selectListItems = list.Select(i => new { Id = i.Id.ToString(), Name = textSelector(i) }).ToList();
        selectListItems.Add(new { Id = "", Name = "Выберите элемент" });
        
        return new SelectList(
            selectListItems,
            "Id",
            "Name",
            selectedValue?.ToString() ?? "");
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
    
    // public static async Task UpdateModelItems<T>(
    //     IQueryable<T> queryable,
    //     List<T> newItems,
    //     List<T> oldItems,
    //     CancellationToken cancellationToken) where T : DatabaseObject
    // {
    //     var ids = newItems.Select(c => c.Id);
    //     var oldIds = oldItems.Select(c => c.Id);
    //     
    //     var addedItems = newItems
    //         .Where(c => oldIds.Contains(c.Id))
    //         .ToList();
    //     
    //     // add new
    //     addedItems.ForEach(queryable);
    //     
    //     // remove deleted
    //     oldItems
    //         .Where(c => !addedItems.Contains(c))
    //         .ToList()
    //         .ForEach(c => oldItems.Remove(c));
    // }

    private static T? GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
    {
        var type = enumVal.GetType();
        var memInfo = type.GetMember(enumVal.ToString());
        var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
        return (attributes.Length > 0) ? (T)attributes[0] : null;
    }
}