using Material_Tracker.Models;

namespace Material_Tracker.Services;

public interface IProjectService
{
    IReadOnlyList<Project> List();
    Task CreateAsync(string name);
    Task RenameAsync(Guid id, string name);
    Task SelectAsync(Guid? id);
    Task DeleteAsync(Guid id);
}

public sealed class ProjectService(AppState state) : IProjectService
{
    public IReadOnlyList<Project> List() => state.Snapshot.Projects.OrderBy(project => project.Name).ToList();
    public Task CreateAsync(string name) => state.UpdateAsync(data =>
    {
        var project = new Project { Name = name.Trim() };
        data.Projects.Add(project);
        data.ActiveProjectId = project.Id;
    });
    public Task RenameAsync(Guid id, string name) => state.UpdateAsync(data =>
    {
        var project = data.Projects.SingleOrDefault(item => item.Id == id)
            ?? throw new SitePackDataException("پروژه پیدا نشد. فهرست را دوباره بررسی کنید.");
        project.Name = name.Trim();
    });
    public Task SelectAsync(Guid? id) => state.SelectProjectAsync(id);
    public Task DeleteAsync(Guid id) => state.UpdateAsync(data =>
    {
        if (!data.Projects.Any(project => project.Id == id)) throw new SitePackDataException("پروژه پیدا نشد.");
        if (data.ActiveProjectId == id || data.Materials.Any(item => item.ProjectId == id))
            throw new SitePackDataException("پروژهٔ فعال یا دارای مصالح قابل حذف نیست.");
        data.Projects.RemoveAll(project => project.Id == id);
    });
}
