using Content.Shared._RMC14.AntiAir;
using Content.Shared._RMC14.UserInterface;
using Robust.Client.Timing;

namespace Content.Client._RMC14.AntiAir;

public sealed class RMCAlmayerAntiAirUISystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCAlmayerAntiAirComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnState(Entity<RMCAlmayerAntiAirComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_timing.CurTick != _timing.LastRealTick)
            return;

        _rmcUI.RefreshUIs<RMCAlmayerAntiAirBui>(ent.Owner);
    }
}
