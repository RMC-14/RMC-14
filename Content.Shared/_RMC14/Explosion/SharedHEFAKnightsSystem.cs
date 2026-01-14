using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

public abstract class SharedHefaKnightsSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;

    private static readonly ProtoId<TagPrototype> SwordTag = "RMCSwordHEFA";

    public override void Initialize()
    {
        SubscribeLocalEvent<HefaKnightExplosionsComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HefaKnightExplosionsComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnUseInHand(Entity<HefaKnightExplosionsComponent> ent, ref UseInHandEvent args)
    {
        if (!_tag.HasTag(ent, SwordTag))
            return;

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

    private void OnMeleeHit(Entity<HefaKnightExplosionsComponent> ent, ref MeleeHitEvent args)
    {
        if (!_tag.HasTag(ent, SwordTag))
            return;
        if (!args.IsHit || !ent.Comp.Primed)
            return;
        if (args.HitEntities.Count == 0)
            return;
        if (_net.IsClient)
            return;

        // Get the first hit entity and explode at their position
        var target = args.HitEntities[0];
        ExplodeSword(ent, args.User, target);
    }

    protected virtual void ExplodeSword(Entity<HefaKnightExplosionsComponent> ent, EntityUid user, EntityUid target)
    {
    }
}
