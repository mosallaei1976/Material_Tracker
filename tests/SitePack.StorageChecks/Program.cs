using System.Text.Json.Nodes;
using Material_Tracker.Models;
using Material_Tracker.Services;
using Microsoft.JSInterop;
using Material_Tracker.Utilities;

var checks = new List<(string Name, Func<Task> Run)>();

Check("Persian calendar handles leap years, Persian/Arabic digits, and round trips", () =>
{
    Assert(PersianDate.Parse("۱۴۰۳/۱۲/۳۰") == new DateOnly(2025, 3, 20));
    Assert(PersianDate.Parse("١٤٠٤/٠١/٠١") == new DateOnly(2025, 3, 21));
    Assert(PersianDate.Format(new DateOnly(2025, 3, 21)) == "۱۴۰۴/۰۱/۰۱");
    foreach (var day in Enumerable.Range(0, 730))
    {
        var date = new DateOnly(2025, 1, 1).AddDays(day);
        Assert(PersianDate.Parse(PersianDate.Format(date)) == date);
    }
    return Task.CompletedTask;
});

Check("Invalid Jalali dates produce Persian errors", async () =>
{
    foreach (var text in new[] { "۱۴۰۴/۱۲/۳۰", "۱۴۰۵/۱۳/۰۱", "۱۴۰۵/۰۲/۳۲", "۱۴۰۵/۰/۱", "2026-11-02", "", "۱۴۰۵/abc/۱" })
        await Reject(() => { PersianDate.Parse(text); return Task.CompletedTask; });
});

Check("Persian decimal input remains precise and rejects ambiguous commas", async () =>
{
    Assert(PersianText.ParseDecimal("۱۲٫۳۴۵", "مقدار") == 12.345m);
    Assert(PersianText.ParseDecimal("١٢.٥", "مقدار") == 12.5m);
    Assert(PersianText.Number(-12.5m) == "-۱۲٫۵");
    await Reject(() => { PersianText.ParseDecimal("1,5", "مقدار"); return Task.CompletedTask; });
});

Check("Project services create, select, rename, and reject duplicate names", async () =>
{
    var (_, state, _) = Setup();
    var projects = new ProjectService(state);
    await projects.CreateAsync("  پروژه اول  ");
    Assert(projects.List().Single().Name == "پروژه اول" && state.ActiveProjectId == projects.List().Single().Id);
    await Reject(() => projects.CreateAsync("پروژه اول"));
    await projects.CreateAsync("پروژه دوم");
    var first = projects.List().Single(item => item.Name == "پروژه اول");
    await projects.RenameAsync(first.Id, "نام جدید");
    await projects.SelectAsync(first.Id);
    Assert(projects.List().Count == 2 && state.ActiveProjectId == first.Id);
    await Reject(() => projects.DeleteAsync(first.Id));
});

Check("Material services enforce ownership and project-local search", async () =>
{
    var (_, state, _) = Setup();
    var projects = new ProjectService(state);
    var materials = new MaterialService(state);
    await projects.CreateAsync("اول");
    var firstId = state.ActiveProjectId!.Value;
    var material = new Material { ProjectId = firstId, Name = "سیمان", Category = "سازه", Unit = "کیسه" };
    await materials.SaveAsync(material, true);
    Assert(materials.List("سیم", "سازه").Count == 1 && materials.List("آجر").Count == 0);
    await projects.CreateAsync("دوم");
    Assert(materials.List().Count == 0);
    await Reject(() => materials.SaveAsync(material, false));
    await Reject(() => materials.DeleteAsync(material.Id));
    await projects.SelectAsync(firstId);
    Assert(materials.List().Count == 1);
});

Check("Transactions update calculated stock, preserve timestamps, and filter dates", async () =>
{
    var (_, state, _) = Setup();
    await state.UpdateAsync(data => Copy(Sample(), data));
    var materials = new MaterialService(state);
    var transactions = new TransactionService(state);
    var material = state.ActiveMaterials.Single();
    material.MinStockWarning = 5;
    await materials.SaveAsync(material, false);
    var usage = new MaterialTransaction
    {
        MaterialId = material.Id, Type = TransactionType.Outflow, Quantity = 10,
        Date = new DateOnly(2026, 11, 3), WorkArea = "طبقه اول", SellerPhone = "09120000000"
    };
    await transactions.SaveAsync(usage, true);
    Assert(materials.List().Single().Stock == 2.345m && materials.List().Single().IsLow);
    Assert(transactions.List(type: TransactionType.Outflow).Single().SellerPhone is null);
    Assert(transactions.List(from: new DateOnly(2026, 11, 3)).Count == 1);
    var saved = transactions.List(type: TransactionType.Outflow).Single();
    var timestamp = saved.CreatedAtUtc;
    saved.Quantity = 20;
    saved.CreatedAtUtc = default;
    await transactions.SaveAsync(saved, false);
    Assert(materials.List().Single().Stock == -7.655m);
    Assert(transactions.List(type: TransactionType.Outflow).Single().CreatedAtUtc == timestamp);
    await transactions.DeleteAsync(usage.Id);
    Assert(materials.List().Single().Stock == 12.345m);
});

Check("Transaction services block cross-project create, edit, and delete", async () =>
{
    var (_, state, _) = Setup();
    await state.UpdateAsync(data => Copy(Sample(), data));
    var transactions = new TransactionService(state);
    var original = transactions.List().Single();
    await new ProjectService(state).CreateAsync("پروژه دیگر");
    Assert(transactions.List().Count == 0);
    await Reject(() => transactions.SaveAsync(original, false));
    await Reject(() => transactions.DeleteAsync(original.Id));
    original.Id = Guid.NewGuid();
    await Reject(() => transactions.SaveAsync(original, true));
});

Check("Material service protects unit and history after transactions exist", async () =>
{
    var (_, state, _) = Setup();
    await state.UpdateAsync(data => Copy(Sample(), data));
    var materials = new MaterialService(state);
    var material = state.ActiveMaterials.Single();
    material.Unit = "تن";
    await Reject(() => materials.SaveAsync(material, false));
    await Reject(() => materials.DeleteAsync(material.Id));
    Assert(state.ActiveMaterials.Single().Unit == "کیسه");
});

Check("First use writes one valid empty versioned document", async () =>
{
    var (browser, state, _) = Setup();
    await Task.WhenAll(state.InitializeAsync(), state.InitializeAsync());
    Assert(browser.Writes == 1 && browser.Values.Keys.Single() == LocalStorageService.StorageKey);
    Assert(state.IsInitialized && state.Snapshot.SchemaVersion == 1 && state.Snapshot.Projects.Count == 0);
});

Check("Reload and backup preserve dates, decimals, and phone strings", async () =>
{
    var (browser, state, backup) = Setup();
    var sample = Sample();
    await state.UpdateAsync(data => Copy(sample, data));
    var exported = await backup.ExportJsonAsync();
    Assert(exported.Contains("2026-11-02"));
    var reloaded = new AppState(new LocalStorageService(browser));
    await reloaded.InitializeAsync();
    var transaction = reloaded.Snapshot.Transactions.Single();
    Assert(transaction.Quantity == 12.345m && transaction.SellerPhone == "09121234567" &&
        transaction.DriverPhone == "+98 (912) 123-4567" && transaction.Date == new DateOnly(2026, 11, 2));
    Assert(exported == await new JsonBackupService(reloaded, browser).ExportJsonAsync());
});

Check("Active project selection persists and isolates transaction queries", async () =>
{
    var (browser, state, _) = Setup();
    var sample = Sample();
    var second = new Project { Name = "پروژه دوم" };
    var material = new Material { ProjectId = second.Id, Name = "سیمان", Category = "سازه", Unit = "کیسه" };
    sample.Projects.Add(second);
    sample.Materials.Add(material);
    sample.Transactions.Add(new MaterialTransaction
    {
        MaterialId = material.Id, Type = TransactionType.Outflow, Quantity = 1,
        Date = new DateOnly(2026, 11, 2), WorkArea = "طبقه دوم"
    });
    await state.UpdateAsync(data => Copy(sample, data));
    Assert(state.ActiveMaterials.Single().ProjectId == sample.ActiveProjectId);
    Assert(state.ActiveTransactions.Single().Type == TransactionType.Inflow);
    await state.SelectProjectAsync(second.Id);
    Assert(state.ActiveTransactions.Single().MaterialId == material.Id);
    var reloaded = new AppState(new LocalStorageService(browser));
    await reloaded.InitializeAsync();
    Assert(reloaded.ActiveProjectId == second.Id);
    await state.SelectProjectAsync(null);
    Assert(state.ActiveMaterials.Count == 0 && state.ActiveTransactions.Count == 0);
});

Check("Detached snapshots and retained callback references cannot mutate state", async () =>
{
    var (_, state, _) = Setup();
    SitePackData? retained = null;
    await state.UpdateAsync(data => { Copy(Sample(), data); retained = data; });
    retained!.Projects[0].Name = "تغییر ناخواسته";
    var snapshot = state.Snapshot;
    snapshot.Projects.Clear();
    state.ActiveMaterials[0].Name = "تغییر ناخواسته";
    Assert(state.Snapshot.Projects.Single().Name == "پروژه نمونه" && state.ActiveMaterials.Single().Name == "سیمان");
});

Check("Concurrent updates do not lose changes", async () =>
{
    var (_, state, _) = Setup();
    await Task.WhenAll(Enumerable.Range(0, 20).Select(i => state.UpdateAsync(data =>
        data.Projects.Add(new Project { Name = $"پروژه {i}" }))));
    Assert(state.Snapshot.Projects.Count == 20);
});

Check("Failed save preserves previous storage and state and reports an error", async () =>
{
    var (browser, state, _) = Setup();
    await state.UpdateAsync(data => Copy(Sample(), data));
    var original = browser.Values[LocalStorageService.StorageKey];
    browser.FailWrites = true;
    await Reject(() => state.UpdateAsync(data => data.Projects[0].Name = "نام جدید"));
    Assert(state.LastError is not null && browser.Values[LocalStorageService.StorageKey] == original);
    Assert(state.Snapshot.Projects.Single().Name == "پروژه نمونه");
    browser.FailWrites = false;
    await state.UpdateAsync(data => data.Projects[0].Name = "نام جدید");
    Assert(state.LastError is null && state.Snapshot.Projects.Single().Name == "نام جدید");
});

Check("Read denial leaves state uninitialized and reports an error", async () =>
{
    var (browser, state, _) = Setup();
    browser.FailReads = true;
    await Reject(state.InitializeAsync);
    Assert(!state.IsInitialized && state.LastError is not null && browser.Writes == 0);
});

Check("Corrupt stored data is preserved and can be recovered by confirmed restore", async () =>
{
    var (browser, state, backup) = Setup();
    browser.Values[LocalStorageService.StorageKey] = "broken JSON";
    await Reject(state.InitializeAsync);
    Assert(browser.Values[LocalStorageService.StorageKey] == "broken JSON" && !state.IsInitialized);
    browser.ConfirmResult = true;
    Assert(await backup.RestoreJsonAsync(DataJson.Serialize(Sample())));
    Assert(state.IsInitialized && state.LastError is null && state.Snapshot.Projects.Count == 1);
});

Check("Cancelled restore leaves state and storage unchanged", async () =>
{
    var (browser, state, backup) = Setup();
    await state.UpdateAsync(data => Copy(Sample(), data));
    var original = await backup.ExportJsonAsync();
    browser.ConfirmResult = false;
    Assert(!await backup.RestoreJsonAsync(DataJson.Serialize(new SitePackData())));
    Assert(browser.Confirmations == 1 && await backup.ExportJsonAsync() == original);
    Assert(browser.Values[LocalStorageService.StorageKey] == original);
});

Check("Confirmed restore replaces all data and notifies after commit", async () =>
{
    var (browser, state, backup) = Setup();
    await state.UpdateAsync(data => Copy(Sample(), data));
    var events = 0;
    state.Changed += () =>
    {
        events++;
        Assert(DataJson.Serialize(state.Snapshot) == browser.Values[LocalStorageService.StorageKey]);
    };
    browser.ConfirmResult = true;
    Assert(await backup.RestoreJsonAsync(DataJson.Serialize(new SitePackData())));
    Assert(events == 1 && state.Snapshot.Projects.Count == 0 && state.ActiveProjectId is null);
});

Check("Failed restore write retains existing committed data", async () =>
{
    var (browser, state, backup) = Setup();
    await state.UpdateAsync(data => Copy(Sample(), data));
    var original = await backup.ExportJsonAsync();
    browser.ConfirmResult = true;
    browser.FailWrites = true;
    await Reject(async () => { await backup.RestoreJsonAsync(DataJson.Serialize(new SitePackData())); });
    Assert(DataJson.Serialize(state.Snapshot) == original && browser.Values[LocalStorageService.StorageKey] == original);
});

Check("Confirmation errors cannot replace data", async () =>
{
    var (browser, state, backup) = Setup();
    await state.UpdateAsync(data => Copy(Sample(), data));
    browser.FailConfirmation = true;
    var writes = browser.Writes;
    await Reject(async () => { await backup.RestoreJsonAsync(DataJson.Serialize(new SitePackData())); });
    Assert(browser.Writes == writes && state.Snapshot.Projects.Count == 1);
});

Check("Deleting referenced material and its history together is blocked", async () =>
{
    var (_, state, _) = Setup();
    await state.UpdateAsync(data => Copy(Sample(), data));
    await Reject(() => state.UpdateAsync(data => { data.Materials.Clear(); data.Transactions.Clear(); }));
    Assert(state.Snapshot.Transactions.Count == 1);
});

Check("Deleting active or populated projects is blocked", async () =>
{
    var (_, state, _) = Setup();
    await state.UpdateAsync(data => Copy(Sample(), data));
    await Reject(() => state.UpdateAsync(data => Copy(new SitePackData(), data)));
    Assert(state.Snapshot.Projects.Count == 1);
});

var invalidDocuments = new (string Name, Action<JsonObject> Mutate)[]
{
    ("unsupported schema", root => root["SchemaVersion"] = 2),
    ("missing schema", root => root.Remove("SchemaVersion")),
    ("null collection", root => root["Projects"] = null),
    ("missing record ID", root => root["Projects"]![0]!.AsObject().Remove("Id")),
    ("empty ID", root => root["Projects"]![0]!["Id"] = Guid.Empty.ToString()),
    ("duplicate ID", root => root["Projects"]!.AsArray().Add(root["Projects"]![0]!.DeepClone())),
    ("duplicate project name", root =>
    {
        var project = root["Projects"]![0]!.DeepClone();
        project["Id"] = Guid.NewGuid().ToString();
        root["Projects"]!.AsArray().Add(project);
    }),
    ("missing active project", root => root["ActiveProjectId"] = Guid.NewGuid().ToString()),
    ("orphan material", root => root["Materials"]![0]!["ProjectId"] = Guid.NewGuid().ToString()),
    ("unknown unit", root => root["Materials"]![0]!["Unit"] = "unknown"),
    ("negative threshold", root => root["Materials"]![0]!["MinStockWarning"] = -1),
    ("orphan transaction", root => root["Transactions"]![0]!["MaterialId"] = Guid.NewGuid().ToString()),
    ("zero quantity", root => root["Transactions"]![0]!["Quantity"] = 0),
    ("negative quantity", root => root["Transactions"]![0]!["Quantity"] = -1),
    ("unknown transaction type", root => root["Transactions"]![0]!["Type"] = 42),
    ("missing date", root => root["Transactions"]![0]!.AsObject().Remove("Date")),
    ("invalid calendar date", root => root["Transactions"]![0]!["Date"] = "2026-02-30"),
    ("default date", root => root["Transactions"]![0]!["Date"] = "0001-01-01"),
    ("non-UTC timestamp", root => root["Projects"]![0]!["CreatedAtUtc"] = "2026-11-02T12:00:00"),
    ("missing usage area", root =>
    {
        var transaction = root["Transactions"]![0]!;
        transaction["Type"] = 1;
        transaction["SellerName"] = null;
        transaction["SellerPhone"] = null;
        transaction["DriverPhone"] = null;
    }),
    ("numeric phone", root => root["Transactions"]![0]!["SellerPhone"] = 9121234567L),
    ("invalid phone characters", root => root["Transactions"]![0]!["SellerPhone"] = "abc")
};

foreach (var (name, mutate) in invalidDocuments)
{
    Check($"Reject {name} before confirmation or write", async () =>
    {
        var (browser, _, backup) = Setup();
        var root = JsonNode.Parse(DataJson.Serialize(Sample()))!.AsObject();
        mutate(root);
        await Reject(async () => { await backup.RestoreJsonAsync(root.ToJsonString()); });
        Assert(browser.Writes == 0 && browser.Confirmations == 0);
    });
}

foreach (var invalid in new[] { "", "{", "null", "[]", "{}", "{\"SchemaVersion\":1,\"SchemaVersion\":2}" })
{
    Check("Reject malformed/incomplete JSON without writing", async () =>
    {
        var (browser, _, backup) = Setup();
        await Reject(async () => { await backup.RestoreJsonAsync(invalid); });
        Assert(browser.Writes == 0 && browser.Confirmations == 0);
    });
}

var failed = 0;
foreach (var (name, run) in checks)
{
    try { await run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception exception) { failed++; Console.WriteLine($"FAIL {name}: {exception}"); }
}
Console.WriteLine($"{checks.Count - failed}/{checks.Count} checks passed.");
return failed == 0 ? 0 : 1;

void Check(string name, Func<Task> run) => checks.Add((name, run));
static void Assert(bool condition)
{
    if (!condition) throw new Exception("Assertion failed.");
}
static async Task Reject(Func<Task> action)
{
    try { await action(); }
    catch (SitePackDataException exception)
    {
        Assert(exception.Message.Any(character => character >= '\u0600' && character <= '\u06ff'));
        return;
    }
    throw new Exception("Expected a Persian data/storage error.");
}
static (BrowserStub Browser, AppState State, JsonBackupService Backup) Setup()
{
    var browser = new BrowserStub();
    var state = new AppState(new LocalStorageService(browser));
    return (browser, state, new JsonBackupService(state, browser));
}
static void Copy(SitePackData source, SitePackData target)
{
    target.SchemaVersion = source.SchemaVersion;
    target.Projects = source.Projects;
    target.Materials = source.Materials;
    target.Transactions = source.Transactions;
    target.ActiveProjectId = source.ActiveProjectId;
}
static SitePackData Sample()
{
    var project = new Project { Name = "پروژه نمونه" };
    var material = new Material { ProjectId = project.Id, Name = "سیمان", Category = "سازه", Unit = "کیسه" };
    return new SitePackData
    {
        Projects = [project], Materials = [material], ActiveProjectId = project.Id,
        Transactions = [new MaterialTransaction
        {
            MaterialId = material.Id, Type = TransactionType.Inflow, Quantity = 12.345m,
            Date = new DateOnly(2026, 11, 2), SellerPhone = "09121234567", DriverPhone = "+98 (912) 123-4567"
        }]
    };
}

sealed class BrowserStub : IJSRuntime
{
    public Dictionary<string, string> Values { get; } = [];
    public int Writes { get; private set; }
    public int Confirmations { get; private set; }
    public bool ConfirmResult { get; set; }
    public bool FailReads { get; set; }
    public bool FailWrites { get; set; }
    public bool FailConfirmation { get; set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        // Force an asynchronous boundary to exercise state serialization across awaits.
        await Task.Yield();
        object? result = null;
        switch (identifier)
        {
            case "localStorage.getItem":
                if (FailReads) throw new JSException("SecurityError");
                result = Values.GetValueOrDefault((string)args![0]!);
                break;
            case "localStorage.setItem":
                if (FailWrites) throw new JSException("QuotaExceededError");
                Values[(string)args![0]!] = (string)args[1]!;
                Writes++;
                break;
            case "confirm":
                if (FailConfirmation) throw new JSException("Confirmation unavailable");
                Confirmations++;
                result = ConfirmResult;
                break;
            default: throw new InvalidOperationException($"Unexpected JS call: {identifier}");
        }
        return result is null ? default! : (TValue)result;
    }
}
