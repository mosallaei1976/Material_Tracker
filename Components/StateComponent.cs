using Material_Tracker.Services;
using Microsoft.AspNetCore.Components;

namespace Material_Tracker.Components;

public abstract class StateComponent : ComponentBase, IDisposable
{
    [Inject] protected AppState State { get; set; } = default!;
    private bool disposed;

    protected override void OnInitialized() => State.Changed += HandleChanged;
    protected virtual void OnStateChanged() { }
    private void HandleChanged()
    {
        if (disposed) return;
        _ = InvokeAsync(() =>
        {
            if (disposed) return;
            OnStateChanged();
            StateHasChanged();
        });
    }
    public void Dispose()
    {
        disposed = true;
        State.Changed -= HandleChanged;
        GC.SuppressFinalize(this);
    }
}
