using Content.Shared._RMC14.Chemistry.SmartFridge;
using Content.Shared._RMC14.UserInterface;
using Robust.Shared.Containers;

namespace Content.Client._RMC14.Chemistry.SmartFridge;

public sealed class RMCSmartFridgeSystem : SharedRMCSmartFridgeSystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSmartFridgeComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<RMCSmartFridgeComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<RMCSmartFridgeComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnState(Entity<RMCSmartFridgeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
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
