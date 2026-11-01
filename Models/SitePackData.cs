namespace Material_Tracker.Models;

public sealed class SitePackData
{
    public int SchemaVersion { get; set; } = 1;
    public List<Project> Projects { get; set; } = [];
    public List<Material> Materials { get; set; } = [];
    public List<MaterialTransaction> Transactions { get; set; } = [];
    public Guid? ActiveProjectId { get; set; }
}
