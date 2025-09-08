using Microsoft.JSInterop;

namespace OneSwiss.Server.Extensions;

public static class JsRuntimeExtensions
{
    public static async Task<T> GetLocalStorageItem<T>(this IJSRuntime jsRuntime, string key)
    {
        return await jsRuntime.InvokeAsync<T>("localStorage.getItem", key);
    }

    public static async Task SetLocalStorageItem(this IJSRuntime jsRuntime, string key, string value)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
    }
}