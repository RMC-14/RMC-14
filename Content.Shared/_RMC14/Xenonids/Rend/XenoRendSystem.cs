using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Xenonids.Sweep;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Rend;

public sealed class XenoRendSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCActionsSystem _actions = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoRendComponent, XenoRendActionEvent>(OnXenoRendAction);
    }

    private void OnXenoRendAction(Entity<XenoRendComponent> xeno, ref XenoRendActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_actions.TryUseAction(args))
            return;

        args.Handled = true;

        EnsureComp<XenoSweepingComponent>(xeno);
        _emote.TryEmoteWithChat(xeno, xeno.Comp.HissEmote);

        foreach (var ent in _entityLookup.GetEntitiesInRange<MobStateComponent>(_transform.GetMapCoordinates(xeno), xeno.Comp.Range))
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, ent))
                continue;

            if (!_interact.InRangeUnobstructed(xeno.Owner, ent.Owner, xeno.Comp.Range))
                continue;

            var myDamage = _damage.TryChangeDamage(ent, xeno.Comp.Damage, origin: xeno, tool: xeno);
            if (myDamage?.GetTotal() > FixedPoint2.Zero)
            {
                var filter = Filter.Pvs(ent, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
                _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { ent }, filter);
            }

            if (_net.IsServer)
                SpawnAttachedTo(xeno.Comp.Effect, ent.Owner.ToCoordinates());

            _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);
        }
    }
}
