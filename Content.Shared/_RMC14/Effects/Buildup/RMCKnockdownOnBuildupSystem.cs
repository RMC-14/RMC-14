using Content.Shared.Stunnable;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Effects.Buildup;

public sealed class RMCKnockdownOnBuildupSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCKnockdownOnBuildupComponent, RMCBuildupTriggeredEvent>(OnBuildupTriggered);
    }

    private void OnBuildupTriggered(Entity<RMCKnockdownOnBuildupComponent> ent, ref RMCBuildupTriggeredEvent args)
    {
        if (_net.IsClient)
            return;

        _stun.TryKnockdown(args.Target, ent.Comp.Duration, ent.Comp.Refresh);
    }
}
