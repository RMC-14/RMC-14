using Content.Shared._RMC14.Chemistry.Centrifuge;
using Content.Shared._RMC14.UserInterface;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Chemistry.Centrifuge;

public sealed class RMCCentrifugeUISystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCCentrifugeComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnAfterState(Entity<RMCCentrifugeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<RMCCentrifugeBui>(ent.Owner);
    }
}
