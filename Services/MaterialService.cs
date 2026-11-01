using Material_Tracker.Models;

namespace Material_Tracker.Services;

public sealed record MaterialStock(Material Material, decimal Stock)
{
    public bool IsLow => Material.MinStockWarning is decimal threshold && Stock < threshold;
}

public interface IMaterialService
{
    IReadOnlyList<MaterialStock> List(string? search = null, string? category = null);
    Task SaveAsync(Material material, bool isNew);
    Task DeleteAsync(Guid id);
}

public sealed class MaterialService(AppState state) : IMaterialService
{
    public IReadOnlyList<MaterialStock> List(string? search = null, string? category = null)
    {
        var data = state.Snapshot;
        var materials = data.Materials.Where(item => item.ProjectId == data.ActiveProjectId);
        if (!string.IsNullOrWhiteSpace(search)) materials = materials.Where(item => item.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(category)) materials = materials.Where(item => item.Category == category);
        var activeIds = data.Materials.Where(item => item.ProjectId == data.ActiveProjectId).Select(item => item.Id).ToHashSet();
        var stocks = data.Transactions.Where(item => activeIds.Contains(item.MaterialId)).GroupBy(item => item.MaterialId).ToDictionary(group => group.Key,
            group => group.Sum(item => item.Type == TransactionType.Inflow ? item.Quantity : -item.Quantity));
        return materials.OrderBy(item => item.Name).Select(item => new MaterialStock(item, stocks.GetValueOrDefault(item.Id))).ToList();
    }

    public Task SaveAsync(Material material, bool isNew)
    {
        var copy = new Material
        {
            Id = material.Id, ProjectId = material.ProjectId, Name = material.Name.Trim(),
            Category = material.Category.Trim(), Unit = material.Unit, MinStockWarning = material.MinStockWarning
        };
        return state.UpdateAsync(data =>
        {
            if (data.ActiveProjectId is null || copy.ProjectId != data.ActiveProjectId)
                throw new SitePackDataException("مصالح باید متعلق به پروژهٔ فعال باشد.");
            var existing = data.Materials.SingleOrDefault(item => item.Id == copy.Id);
            if (isNew)
            {
                if (existing is not null) throw new SitePackDataException("مصالح قبلاً ثبت شده است.");
                data.Materials.Add(copy);
            }
            else
            {
                if (existing is null || existing.ProjectId != data.ActiveProjectId)
                    throw new SitePackDataException("مصالح در پروژهٔ فعال پیدا نشد.");
                if (existing.Unit != copy.Unit && data.Transactions.Any(item => item.MaterialId == copy.Id))
                    throw new SitePackDataException("واحد مصالح دارای تراکنش قابل تغییر نیست.");
                data.Materials[data.Materials.IndexOf(existing)] = copy;
            }
        });
    }

    public Task DeleteAsync(Guid id) => state.UpdateAsync(data =>
    {
        if (!data.Materials.Any(item => item.Id == id && item.ProjectId == data.ActiveProjectId))
            throw new SitePackDataException("مصالح در پروژهٔ فعال پیدا نشد.");
        if (data.Transactions.Any(item => item.MaterialId == id))
            throw new SitePackDataException("مصالح دارای تراکنش قابل حذف نیست.");
        data.Materials.RemoveAll(item => item.Id == id);
    });
}
