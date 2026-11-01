using Material_Tracker.Models;

namespace Material_Tracker.Services;

public sealed class AppState(IStorageService storage)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private SitePackData data = new();

    public bool IsInitialized { get; private set; }
    public string? LastError { get; private set; }
    public event Action? Changed;

    // Return detached copies: callers cannot mutate committed state without saving.
    public SitePackData Snapshot => DataJson.Clone(data);
    public Guid? ActiveProjectId => data.ActiveProjectId;
    public IReadOnlyList<Material> ActiveMaterials => Snapshot.Materials
        .Where(material => material.ProjectId == ActiveProjectId).ToList();
    public IReadOnlyList<MaterialTransaction> ActiveTransactions
    {
        get
        {
            var snapshot = Snapshot;
            var ids = snapshot.Materials.Where(material => material.ProjectId == snapshot.ActiveProjectId)
                .Select(material => material.Id).ToHashSet();
            return snapshot.Transactions.Where(transaction => ids.Contains(transaction.MaterialId))
                .OrderByDescending(transaction => transaction.Date)
                .ThenByDescending(transaction => transaction.CreatedAtUtc)
                .ThenBy(transaction => transaction.Id).ToList();
        }
    }

    public Task InitializeAsync() => ExecuteAsync(async () => await EnsureInitializedAsync());

    public Task UpdateAsync(Action<SitePackData> update) => ExecuteAsync(async () =>
    {
        await EnsureInitializedAsync();
        var candidate = Snapshot;
        update(candidate);
        // Detach again so a callback cannot retain and later mutate the committed document.
        candidate = DataJson.Clone(candidate);
        EnsureSafeDeletion(candidate);
        await storage.SaveAsync(candidate);
        data = candidate;
    });

    public Task SelectProjectAsync(Guid? projectId) => UpdateAsync(candidate => candidate.ActiveProjectId = projectId);

    internal async Task<bool> RestoreAsync(SitePackData replacement, Func<Task<bool>> confirm)
    {
        var restored = false;
        var candidate = DataJson.Clone(replacement);
        await ExecuteAsync(async () =>
        {
            // Deliberately allow recovery even if the existing stored JSON is corrupt.
            if (!await confirm()) return;
            await storage.SaveAsync(candidate);
            data = candidate;
            IsInitialized = true;
            restored = true;
        });
        return restored;
    }

    private async Task EnsureInitializedAsync()
    {
        if (IsInitialized) return;
        data = DataJson.Clone(await storage.LoadAsync());
        IsInitialized = true;
    }

    private void EnsureSafeDeletion(SitePackData candidate)
    {
        foreach (var material in data.Materials)
        {
            if (data.Transactions.Any(transaction => transaction.MaterialId == material.Id) &&
                !candidate.Materials.Any(item => item.Id == material.Id && item.ProjectId == material.ProjectId))
                throw new SitePackDataException("مصالح دارای تراکنش قابل حذف یا انتقال به پروژهٔ دیگر نیست.");
        }
        foreach (var project in data.Projects.Where(project => !candidate.Projects.Any(item => item.Id == project.Id)))
        {
            if (project.Id == data.ActiveProjectId || data.Materials.Any(material => material.ProjectId == project.Id))
                throw new SitePackDataException("حذف پروژهٔ فعال یا دارای مصالح در این مرحله مجاز نیست.");
        }
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        await gate.WaitAsync();
        try
        {
            await action();
            LastError = null;
        }
        catch (SitePackDataException exception)
        {
            LastError = exception.Message;
            throw;
        }
        finally
        {
            gate.Release();
            Changed?.Invoke();
        }
    }
}
