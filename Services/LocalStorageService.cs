using Material_Tracker.Models;
using Microsoft.JSInterop;

namespace Material_Tracker.Services;

public sealed class LocalStorageService(IJSRuntime js) : IStorageService
{
    public const string StorageKey = "sitepack:data:v1";

    public async Task<SitePackData> LoadAsync()
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (json is not null) return DataJson.Deserialize(json);

            var data = new SitePackData();
            await SaveAsync(data);
            return data;
        }
        catch (JSException exception)
        {
            throw new SitePackDataException("خواندن داده‌ها از حافظهٔ مرورگر ممکن نشد. دسترسی به حافظه را بررسی کنید.", exception);
        }
    }

    public async Task SaveAsync(SitePackData data)
    {
        var json = DataJson.Serialize(data);
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch (JSException exception)
        {
            throw new SitePackDataException(
                "ذخیرهٔ داده‌ها ممکن نشد. ممکن است حافظهٔ مرورگر پر یا دسترسی به آن مسدود باشد. تغییرات ذخیره نشدند.", exception);
        }
    }
}
