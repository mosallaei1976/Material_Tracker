namespace Material_Tracker.Models;

public class MaterialTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MaterialId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? SellerName { get; set; }
    public string? SellerPhone { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string? DocumentNumber { get; set; }
    public string? WorkArea { get; set; }
    public string? Purpose { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
