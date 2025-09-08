using MudBlazor;
using OneSwiss.Server.Components;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Extensions;

public static class DialogServiceExtensions
{
    public static async Task<bool> ShowItemDeletingDialog<T>(this IDialogService dialogService, Guid id,
        string itemName) where T : class, IHasId
    {
        var parameters = new DialogParameters<ItemDeletingDialog<T>>
            { { c => c.Id, id }, { c => c.ItemName, itemName } };
        var dialog = await dialogService.ShowAsync<ItemDeletingDialog<T>>(string.Empty, parameters);

        var result = await dialog.Result;

        return result!.Canceled;
    }
}