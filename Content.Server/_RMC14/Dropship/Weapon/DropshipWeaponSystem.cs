using Content.Shared._RMC14.Dropship.Weapon;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Dropship.Weapon;

public sealed class DropshipWeaponSystem : SharedDropshipWeaponSystem
{
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    protected override void AddPvs(Entity<DropshipTerminalWeaponsComponent> terminal, Entity<ActorComponent?> actor)
    {
        base.AddPvs(terminal, actor);

        if (terminal.Comp.Target is not { } target)
            return;

        if (!Resolve(actor, ref actor.Comp, false))
            return;

        _viewSubscriber.AddViewSubscriber(target, actor.Comp.PlayerSession);
    }

    protected override void RemovePvs(Entity<DropshipTerminalWeaponsComponent> terminal, Entity<ActorComponent?> actor)
    {
        base.AddPvs(terminal, actor);

        if (terminal.Comp.Target is not { } target)
            return;

        if (!Resolve(actor, ref actor.Comp, false))
            return;

        _viewSubscriber.RemoveViewSubscriber(target, actor.Comp.PlayerSession);
    }
}
