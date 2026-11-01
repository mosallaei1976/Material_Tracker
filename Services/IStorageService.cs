using Material_Tracker.Models;

namespace Material_Tracker.Services;

public interface IStorageService
{
    Task<SitePackData> LoadAsync();
    Task SaveAsync(SitePackData data);
}
