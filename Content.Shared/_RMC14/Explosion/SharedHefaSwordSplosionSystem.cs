using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Explosion;

public abstract class SharedHefaSwordSplosionSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HefaSwordSplosionComponent, UseInHandEvent>(OnUseInHand, before: [typeof(ClothingSystem)]);
        SubscribeLocalEvent<HefaSwordSplosionComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnUseInHand(Entity<HefaSwordSplosionComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.Primed = !ent.Comp.Primed;
        Dirty(ent);

        if (ent.Comp.Primed)
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.PrimedPopup), args.User, args.User);
            if (ent.Comp.PrimeSound != null)
                _audio.PlayPredicted(ent.Comp.PrimeSound, ent, args.User);
        }
        else
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.DeprimedPopup), args.User, args.User);
        }
    }

    private void OnMeleeHit(Entity<HefaSwordSplosionComponent> ent, ref MeleeHitEvent args)
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

    protected virtual void ExplodeSword(Entity<HefaSwordSplosionComponent> ent, EntityUid user, EntityUid target)
    {
    }
}
