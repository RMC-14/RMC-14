using Content.Shared._RMC14.Chemistry.SmartFridge;
using Content.Shared._RMC14.UserInterface;
using Robust.Client.Timing;
using Robust.Shared.Containers;

namespace Content.Client._RMC14.Chemistry.SmartFridge;

public sealed class RMCSmartFridgeSystem : SharedRMCSmartFridgeSystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSmartFridgeComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<RMCSmartFridgeComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<RMCSmartFridgeComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnState(Entity<RMCSmartFridgeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_timing.CurTick != _timing.LastRealTick)
            return;

        RefreshUIs(ent);
    }

    private void OnInserted(Entity<RMCSmartFridgeComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        RefreshUIs(ent);
    }

    private void OnRemoved(Entity<RMCSmartFridgeComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RefreshUIs(ent);
    }

    private void RefreshUIs(Entity<RMCSmartFridgeComponent> ent)
    {
        _rmcUI.RefreshUIs<RMCSmartFridgeBui>(ent.Owner);
    }
}
