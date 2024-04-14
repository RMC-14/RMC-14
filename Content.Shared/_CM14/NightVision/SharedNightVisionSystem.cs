using Content.Shared.Alert;
using Content.Shared.Rounding;

namespace Content.Shared._CM14.NightVision;

public abstract class SharedNightVisionSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NightVisionComponent, ComponentStartup>(OnNightVisionStartup);
        SubscribeLocalEvent<NightVisionComponent, MapInitEvent>(OnNightVisionMapInit);
        SubscribeLocalEvent<NightVisionComponent, AfterAutoHandleStateEvent>(OnNightVisionAfterHandle);
        SubscribeLocalEvent<NightVisionComponent, ComponentRemove>(OnNightVisionRemove);
    }

    private void OnNightVisionStartup(Entity<NightVisionComponent> ent, ref ComponentStartup args)
    {
        NightVisionChanged(ent);
    }

    private void OnNightVisionAfterHandle(Entity<NightVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        NightVisionChanged(ent);
    }

    private void OnNightVisionMapInit(Entity<NightVisionComponent> ent, ref MapInitEvent args)
    {
        UpdateAlert(ent);
    }

    private void OnNightVisionRemove(Entity<NightVisionComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
        NightVisionRemoved(ent);
    }

    public void Toggle(Entity<NightVisionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.State = ent.Comp.State switch
        {
            NightVisionState.Off => NightVisionState.Half,
            NightVisionState.Half => NightVisionState.Full,
            NightVisionState.Full => NightVisionState.Off,
            _ => throw new ArgumentOutOfRangeException()
        };

        Dirty(ent);
        UpdateAlert((ent, ent.Comp));
    }

    private void UpdateAlert(Entity<NightVisionComponent> ent)
    {
        var level = MathF.Max((int) NightVisionState.Off, (int) ent.Comp.State);
        var max = _alerts.GetMaxSeverity(ent.Comp.Alert);
        var severity = max - ContentHelpers.RoundToLevels(level, (int) NightVisionState.Full, max + 1);
        _alerts.ShowAlert(ent, ent.Comp.Alert, (short) severity);

        NightVisionChanged(ent);
    }

    protected virtual void NightVisionChanged(Entity<NightVisionComponent> ent)
    {
    }

    protected virtual void NightVisionRemoved(Entity<NightVisionComponent> ent)
    {
    }
}
