using Content.Shared._RMC14.Marines.Access;
using Content.Shared._RMC14.UserInterface;
using Robust.Client.Timing;
using Robust.Shared.Containers;

namespace Content.Client._RMC14.Access;

public sealed class IdModificationConsoleUISystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IdModificationConsoleComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<IdModificationConsoleComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<IdModificationConsoleComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnState(Entity<IdModificationConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_timing.CurTick != _timing.LastRealTick)
            return;

        RefreshUIs(ent);
    }

    private void OnInserted(Entity<IdModificationConsoleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        RefreshUIs(ent);
    }

    private void OnRemoved(Entity<IdModificationConsoleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RefreshUIs(ent);
    }

    private void RefreshUIs(Entity<IdModificationConsoleComponent> ent)
    {
        _rmcUI.RefreshUIs<IdModificationConsoleBui>(ent.Owner);
    }
}
