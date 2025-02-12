using Content.Shared._RMC14.Atmos;
using Content.Shared.ActionBlocker;
using Content.Shared.Coordinates;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.TailFountain;

public sealed class XenoTailFountainSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCFlammableSystem _flame = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoTailFountainComponent, XenoTailFountainActionEvent>(OnTailFountainAction);
    }

    private void OnTailFountainAction(Entity<XenoTailFountainComponent> xeno, ref XenoTailFountainActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_actionBlocker.CanAttack(xeno))
            return;

        if (xeno.Owner == args.Target)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-tail-fountain-fail-self"), xeno, xeno, PopupType.Small);
            return;
        }

        if (!HasComp<MobStateComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-tail-fountain-fail"), xeno, xeno, PopupType.Small);
            return;
        }

        args.Handled = true;

        _flame.Extinguish(args.Target);
        _audio.PlayPredicted(xeno.Comp.ExtinguishSound, args.Target, xeno);
        _popup.PopupPredicted(Loc.GetString("rmc-xeno-tail-fountain-self", ("target", args.Target)),
            Loc.GetString("rmc-xeno-tail-fountain-others", ("user", xeno), ("target", args.Target)), xeno, xeno, PopupType.SmallCaution);

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.Acid, args.Target.ToCoordinates());

        if (TryComp(xeno, out MeleeWeaponComponent? melee))
        {
            if (_timing.CurTime < melee.NextAttack)
                return;

            melee.NextAttack = _timing.CurTime + TimeSpan.FromSeconds(1);
            Dirty(xeno, melee);
        }

        var attackEv = new MeleeAttackEvent(xeno);
        RaiseLocalEvent(xeno, ref attackEv);
    }
}
