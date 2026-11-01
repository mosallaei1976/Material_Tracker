using Material_Tracker.Models;

namespace Material_Tracker.Services;

public static class DataValidator
{
    public static IReadOnlyList<string> Units { get; } = Array.AsReadOnly(new[]
    {
        "عدد", "شاخه", "کیسه", "بسته", "کارتن", "کیلوگرم", "تن",
        "متر طول", "متر مربع", "متر مکعب", "لیتر"
    });

    public static void Validate(SitePackData data)
    {
        Require(data.SchemaVersion == 1, "نسخهٔ داده‌ها پشتیبانی نمی‌شود.");
        Require(data.Projects is not null && data.Materials is not null && data.Transactions is not null,
            "فهرست‌های داده نباید خالی از مقدار باشند.");

        var projectIds = new HashSet<Guid>();
        var projectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in data.Projects!)
        {
            Require(project is not null, "اطلاعات پروژه نامعتبر است.");
            Require(project!.Id != Guid.Empty && projectIds.Add(project.Id), "شناسهٔ پروژه نامعتبر یا تکراری است.");
            Require(!string.IsNullOrWhiteSpace(project.Name) && projectNames.Add(project.Name.Trim()),
                "نام پروژه الزامی است و نباید تکراری باشد.");
            Require(IsUtc(project.CreatedAtUtc), "زمان ایجاد پروژه باید معتبر و بر اساس UTC باشد.");
        }

        Require(data.ActiveProjectId is null || projectIds.Contains(data.ActiveProjectId.Value),
            "پروژهٔ فعال در داده‌ها وجود ندارد.");

        var materialIds = new HashSet<Guid>();
        var materialNames = new HashSet<(Guid, string)>();
        foreach (var material in data.Materials!)
        {
            Require(material is not null, "اطلاعات مصالح نامعتبر است.");
            Require(material!.Id != Guid.Empty && materialIds.Add(material.Id), "شناسهٔ مصالح نامعتبر یا تکراری است.");
            Require(projectIds.Contains(material.ProjectId), "پروژهٔ مصالح وجود ندارد.");
            Require(!string.IsNullOrWhiteSpace(material.Name) &&
                materialNames.Add((material.ProjectId, material.Name.Trim().ToUpperInvariant())),
                "نام مصالح الزامی است و نباید در پروژه تکراری باشد.");
            Require(!string.IsNullOrWhiteSpace(material.Category), "دسته‌بندی مصالح الزامی است.");
            Require(Units.Contains(material.Unit), "واحد مصالح معتبر نیست.");
            Require(material.MinStockWarning is null or >= 0, "حد هشدار موجودی نمی‌تواند منفی باشد.");
        }

        var transactionIds = new HashSet<Guid>();
        foreach (var transaction in data.Transactions!)
        {
            Require(transaction is not null, "اطلاعات تراکنش نامعتبر است.");
            Require(transaction!.Id != Guid.Empty && transactionIds.Add(transaction.Id),
                "شناسهٔ تراکنش نامعتبر یا تکراری است.");
            Require(materialIds.Contains(transaction.MaterialId), "مصالح تراکنش وجود ندارد.");
            Require(Enum.IsDefined(transaction.Type), "نوع تراکنش معتبر نیست.");
            Require(transaction.Quantity > 0, "مقدار تراکنش باید بیشتر از صفر باشد.");
            Require(transaction.Date != default, "تاریخ تراکنش معتبر نیست.");
            Require(IsUtc(transaction.CreatedAtUtc), "زمان ایجاد تراکنش باید معتبر و بر اساس UTC باشد.");
            Require(transaction.Type != TransactionType.Outflow || !string.IsNullOrWhiteSpace(transaction.WorkArea),
                "محل مصرف برای خروج مصالح الزامی است.");
            Require(transaction.Type != TransactionType.Outflow ||
                (string.IsNullOrWhiteSpace(transaction.SellerName) && string.IsNullOrWhiteSpace(transaction.SellerPhone) &&
                 string.IsNullOrWhiteSpace(transaction.DriverName) && string.IsNullOrWhiteSpace(transaction.DriverPhone)),
                "اطلاعات فروشنده و راننده فقط برای ورود مصالح قابل ثبت است.");
            ValidatePhone(transaction.SellerPhone);
            ValidatePhone(transaction.DriverPhone);
        }
    }

    private static bool IsUtc(DateTime value) => value != default && value.Kind == DateTimeKind.Utc;

    private static void ValidatePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var phone = value.Trim();
        Require(phone.Any(char.IsDigit) && phone.Select((c, i) =>
            char.IsDigit(c) || c is ' ' or '-' or '(' or ')' || (c == '+' && i == 0)).All(valid => valid),
            "شمارهٔ تلفن معتبر نیست.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new SitePackDataException(message);
    }
}
