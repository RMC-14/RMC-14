using Content.Shared._RMC14.Chemistry.TuringDispenser;
using Content.Shared._RMC14.UserInterface;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Chemistry.TuringDispenser;

public sealed class RMCTuringDispenserUISystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCTuringDispenserComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnAfterState(Entity<RMCTuringDispenserComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<RMCTuringDispenserBui>(ent.Owner);
    }
}
