using Material_Tracker.Models;

namespace Material_Tracker.Services;

public interface ITransactionService
{
    IReadOnlyList<MaterialTransaction> List(Guid? materialId = null, TransactionType? type = null, DateOnly? from = null, DateOnly? to = null);
    Task SaveAsync(MaterialTransaction transaction, bool isNew);
    Task DeleteAsync(Guid id);
}

public sealed class TransactionService(AppState state) : ITransactionService
{
    public IReadOnlyList<MaterialTransaction> List(Guid? materialId = null, TransactionType? type = null, DateOnly? from = null, DateOnly? to = null) =>
        state.ActiveTransactions.Where(item => (materialId is null || item.MaterialId == materialId) &&
            (type is null || item.Type == type) && (from is null || item.Date >= from) && (to is null || item.Date <= to)).ToList();

    public Task SaveAsync(MaterialTransaction transaction, bool isNew)
    {
        var inflow = transaction.Type == TransactionType.Inflow;
        var copy = new MaterialTransaction
        {
            Id = transaction.Id, MaterialId = transaction.MaterialId, Type = transaction.Type,
            Quantity = transaction.Quantity, Date = transaction.Date, Notes = transaction.Notes?.Trim(),
            SellerName = inflow ? transaction.SellerName?.Trim() : null,
            SellerPhone = inflow ? transaction.SellerPhone?.Trim() : null,
            DriverName = inflow ? transaction.DriverName?.Trim() : null,
            DriverPhone = inflow ? transaction.DriverPhone?.Trim() : null,
            DocumentNumber = transaction.DocumentNumber?.Trim(),
            WorkArea = inflow ? null : transaction.WorkArea?.Trim(), Purpose = inflow ? null : transaction.Purpose?.Trim()
        };
        return state.UpdateAsync(data =>
        {
            EnsureOwnership(data, copy.MaterialId);
            var existing = data.Transactions.SingleOrDefault(item => item.Id == copy.Id);
            if (isNew)
            {
                if (existing is not null) throw new SitePackDataException("تراکنش قبلاً ثبت شده است.");
                data.Transactions.Add(copy);
            }
            else
            {
                if (existing is null) throw new SitePackDataException("تراکنش پیدا نشد.");
                EnsureOwnership(data, existing.MaterialId);
                copy.CreatedAtUtc = existing.CreatedAtUtc;
                data.Transactions[data.Transactions.IndexOf(existing)] = copy;
            }
        });
    }

    public Task DeleteAsync(Guid id) => state.UpdateAsync(data =>
    {
        var transaction = data.Transactions.SingleOrDefault(item => item.Id == id)
            ?? throw new SitePackDataException("تراکنش پیدا نشد.");
        EnsureOwnership(data, transaction.MaterialId);
        data.Transactions.Remove(transaction);
    });

    private static void EnsureOwnership(SitePackData data, Guid materialId)
    {
        if (data.ActiveProjectId is null || !data.Materials.Any(item => item.Id == materialId && item.ProjectId == data.ActiveProjectId))
            throw new SitePackDataException("مصالح تراکنش باید متعلق به پروژهٔ فعال باشد.");
    }
}
