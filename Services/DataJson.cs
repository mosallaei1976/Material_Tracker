using System.Text.Json;
using Material_Tracker.Models;

namespace Material_Tracker.Services;

internal static class DataJson
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Serialize(SitePackData data)
    {
        DataValidator.Validate(data);
        return JsonSerializer.Serialize(data, Options);
    }

    public static SitePackData Deserialize(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            RequireFields(document.RootElement, "SchemaVersion", "Projects", "Materials", "Transactions", "ActiveProjectId");
            RequireRecords(document.RootElement.GetProperty("Projects"), "Id", "Name", "CreatedAtUtc");
            RequireRecords(document.RootElement.GetProperty("Materials"), "Id", "ProjectId", "Name", "Category", "Unit");
            RequireRecords(document.RootElement.GetProperty("Transactions"),
                "Id", "MaterialId", "Type", "Quantity", "Date", "CreatedAtUtc");
            var data = JsonSerializer.Deserialize<SitePackData>(json, Options)
                ?? throw new SitePackDataException("محتوای فایل پشتیبان معتبر نیست.");
            DataValidator.Validate(data);
            return data;
        }
        catch (JsonException exception)
        {
            throw new SitePackDataException("ساختار JSON یا نوع مقادیر داده معتبر نیست.", exception);
        }
    }

    public static SitePackData Clone(SitePackData data) => Deserialize(Serialize(data));

    private static void RequireRecords(JsonElement array, params string[] fields)
    {
        if (array.ValueKind != JsonValueKind.Array)
            throw new SitePackDataException("فهرست‌های داده باید آرایه باشند.");
        foreach (var record in array.EnumerateArray()) RequireFields(record, fields);
    }

    private static void RequireFields(JsonElement element, params string[] fields)
    {
        if (element.ValueKind != JsonValueKind.Object || fields.Any(field => !element.TryGetProperty(field, out _)))
            throw new SitePackDataException("بخشی از اطلاعات الزامی در داده‌ها وجود ندارد.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (element.EnumerateObject().Any(property => !names.Add(property.Name)))
            throw new SitePackDataException("نام فیلد تکراری در داده‌ها وجود دارد.");
    }
}
