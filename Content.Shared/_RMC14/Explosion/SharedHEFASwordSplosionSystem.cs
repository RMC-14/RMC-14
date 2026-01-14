using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Explosion;

public abstract class SharedHefaSwordSplosionSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HefaSwordOnHitTriggerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HefaSwordOnHitTriggerComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnUseInHand(Entity<HefaSwordOnHitTriggerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.Primed = !ent.Comp.Primed;
        Dirty(ent);

        if (ent.Comp.Primed)
        {
            Popup.PopupClient(Loc.GetString(ent.Comp.PrimedPopup), args.User, args.User);
            if (ent.Comp.PrimeSound != null)
                Audio.PlayPredicted(ent.Comp.PrimeSound, ent, args.User);
        }
        else
        {
            Popup.PopupClient(Loc.GetString(ent.Comp.DeprimedPopup), args.User, args.User);
        }
    }

    private void OnMeleeHit(Entity<HefaSwordOnHitTriggerComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || !ent.Comp.Primed)
            return;
        if (_net.IsClient)
            return;

        // Only triggers on mobs.
        foreach (var hit in args.HitEntities)
        {
            if (!HasComp<MobStateComponent>(hit))
                continue;

            ExplodeSword(ent, args.User, hit);
            return;
        }
    }

    protected virtual void ExplodeSword(Entity<HefaSwordOnHitTriggerComponent> ent, EntityUid user, EntityUid target)
    {
    }
}
