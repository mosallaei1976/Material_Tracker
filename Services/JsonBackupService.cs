using Microsoft.JSInterop;

namespace Material_Tracker.Services;

public sealed class JsonBackupService(AppState state, IJSRuntime js) : IJsonBackupService
{
    public async Task<string> ExportJsonAsync()
    {
        await state.InitializeAsync();
        return DataJson.Serialize(state.Snapshot);
    }

    public Task<bool> RestoreJsonAsync(string json)
    {
        // Validate before asking for confirmation or changing either storage or state.
        var replacement = DataJson.Deserialize(json);
        return state.RestoreAsync(replacement, ConfirmAsync);
    }

    private async Task<bool> ConfirmAsync()
    {
        try
        {
            return await js.InvokeAsync<bool>("confirm",
                "بازیابی نسخهٔ پشتیبان، همهٔ داده‌های فعلی سایت‌پَک را جایگزین می‌کند. آیا ادامه می‌دهید؟");
        }
        catch (JSException exception)
        {
            throw new SitePackDataException("نمایش تأیید بازیابی ممکن نشد. داده‌ها جایگزین نشدند.", exception);
        }
    }
}
