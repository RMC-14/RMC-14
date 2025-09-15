using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared._RMC14.Xenonids.Rage;
using Content.Shared._RMC14.Xenonids.Sweep;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Eviscerate;

public sealed class XenoEviscerateSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedXenoHealSystem _xenoHeal = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly XenoRageSystem _rage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;

    private readonly HashSet<Entity<MobStateComponent>> _hit = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoEviscerateComponent, XenoEviscerateActionEvent>(OnXenoEviscerateAction);
        SubscribeLocalEvent<XenoEviscerateComponent, XenoEviscerateDoAfterEvent>(OnXenoEviscerateDoAfter);
    }

    private void OnXenoEviscerateAction(Entity<XenoEviscerateComponent> xeno, ref XenoEviscerateActionEvent args)
    {
        var rage = _rage.GetRage(xeno.Owner);

        if (rage <= 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-eviscerate-fail"), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        var listRage = rage - 1;
        var windupReduction = xeno.Comp.WindupReductionAtRageLevels[listRage];
        var windupTime = xeno.Comp.WindupTime - windupReduction;

        var ev = new XenoEviscerateDoAfterEvent(listRage);
        var doAfter = new DoAfterArgs(EntityManager, xeno, windupTime, ev, xeno)
        {
            BreakOnMove = true,
            Hidden = true,
            MovementThreshold = 0.5f,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            args.Handled = true;

            _stun.TrySlowdown(xeno, windupTime, false, 0f, 0f);
            _rage.IncrementRage(xeno.Owner, -1);

            if (rage > 1)
            {
                var selfMsg = Loc.GetString("rmc-xeno-eviscerate-windup-self");
                var msg = Loc.GetString("rmc-xeno-eviscerate-windup", ("xeno", xeno));
                _popup.PopupPredicted(selfMsg, msg, xeno, xeno, PopupType.MediumCaution);
            }
            else
            {
                var selfMsg = Loc.GetString("rmc-xeno-eviscerate-windup-small-self");
                var msg = Loc.GetString("rmc-xeno-eviscerate-windup-small", ("xeno", xeno));
                _popup.PopupPredicted(selfMsg, msg, xeno, xeno, PopupType.MediumCaution);
            }
        }
    }

    private void OnXenoEviscerateDoAfter(Entity<XenoEviscerateComponent> xeno, ref XenoEviscerateDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        EnsureComp<XenoSweepingComponent>(xeno);
        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);
        _rmcPulling.TryStopAllPullsFromAndOn(xeno);
        _emote.TryEmoteWithChat(xeno, xeno.Comp.RoarEmote);

        var damage = xeno.Comp.DamageAtRageLevels[args.Rage];
        var range = xeno.Comp.RangeAtRageLevels[args.Rage];
        var transform = Transform(xeno.Owner);

        if (_net.IsClient) // todo prediction
            return;

        _hit.Clear();
        _entityLookup.GetEntitiesInRange(transform.Coordinates, range, _hit);

        var validTargets = 0;
        var origin = _transform.GetMapCoordinates(xeno);
        foreach (var mob in _hit)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, mob))
                continue;

            if (!_interact.InRangeUnobstructed(xeno.Owner, mob.Owner, range))
                continue;

            _rmcPulling.TryStopAllPullsFromAndOn(mob);

            _damageable.TryChangeDamage(mob, _xeno.TryApplyXenoSlashDamageMultiplier(mob, damage), origin: xeno, tool: xeno);

            var filter = Filter.Pvs(mob, entityManager: EntityManager);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { mob }, filter);

            if (range > 1.5f)
            {
                _audio.PlayPvs(xeno.Comp.RageHitSound, mob); // todo spawn gibs
                _stun.TryParalyze(mob, xeno.Comp.StunTime, true);
            }
            else
            {
                _audio.PlayPvs(xeno.Comp.HitSound, mob);
            }

            validTargets += 1;
        }

        var healAmount = Math.Clamp(validTargets * xeno.Comp.LifeStealPerMarine, 0, xeno.Comp.MaxLifeSteal);
        _xenoHeal.CreateHealStacks(xeno, healAmount, xeno.Comp.HealDelay, 1, xeno.Comp.HealDelay);
    }
}
