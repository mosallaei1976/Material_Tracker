namespace Material_Tracker.Services;

public interface IJsonBackupService
{
    Task<string> ExportJsonAsync();
    Task<bool> RestoreJsonAsync(string json);
}
