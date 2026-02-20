using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.ARES.ExternalTerminals;
using Content.Shared._RMC14.UserInterface;
using Robust.Client.Timing;

namespace Content.Client._RMC14.ARES;

public sealed class ARESExternalTerminalUISystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ARESExternalTerminalComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnState(Entity<ARESExternalTerminalComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_timing.CurTick != _timing.LastRealTick)
            return;

        RefreshUIs(ent);
    }

    private void RefreshUIs(Entity<ARESExternalTerminalComponent> ent)
    {
        _rmcUI.RefreshUIs<ARESExternalTerminalBui>(ent.Owner);
    }
}
