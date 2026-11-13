using Content.Shared._RMC14.Chemistry.TuringDispenser;
using Content.Shared._RMC14.UserInterface;

namespace Content.Client._RMC14.Chemistry.TuringDispenser;

public sealed class RMCTuringDispenserSystem : SharedRMCTuringDispenserSystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCTuringDispenserComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnAfterState(Entity<RMCTuringDispenserComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshUIs(ent);
    }

    protected override void RefreshUIs(Entity<RMCTuringDispenserComponent> ent)
    {
        _rmcUI.RefreshUIs<RMCTuringDispenserBui>(ent.Owner);
    }
}
