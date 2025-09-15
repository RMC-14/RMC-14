using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Barricade;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.GasToggle;
using Content.Shared._RMC14.Xenonids.Neurotoxin;
using Content.Shared._RMC14.Xenonids.Rotate;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Stab;

public abstract class SharedXenoTailStabSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDirectionalAttackBlockSystem _directionBlock = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoRotateSystem _rotate = default!;
    [Dependency] private readonly RMCDazedSystem _daze = default!;
    [Dependency] private readonly RMCCameraShakeSystem _cameraShake = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;

    protected Box2Rotated LastTailAttack;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoTailStabComponent, XenoTailStabEvent>(OnXenoTailStab);
        SubscribeLocalEvent<XenoTailStabComponent, XenoGasToggleActionEvent>(OnXenoGasToggle);
    }

    private void OnXenoGasToggle(Entity<XenoTailStabComponent> stab, ref XenoGasToggleActionEvent args)
    {
        if (!stab.Comp.Toggle)
            return;

        stab.Comp.InjectNeuro = !stab.Comp.InjectNeuro;
    }


    private void OnXenoTailStab(Entity<XenoTailStabComponent> stab, ref XenoTailStabEvent args)
    {
        if (!_actionBlocker.CanAttack(stab) ||
            !TryComp(stab, out TransformComponent? transform))
        {
            return;
        }

        var userCoords = _transform.GetMapCoordinates(stab, transform);
        if (userCoords.MapId == MapId.Nullspace)
            return;

        var targetCoords = _transform.ToMapCoordinates(args.Target);
        if (userCoords.MapId != targetCoords.MapId)
            return;

        if (TryComp(stab, out MeleeWeaponComponent? melee))
        {
            if (_timing.CurTime < melee.NextAttack)
                return;

            melee.NextAttack = _timing.CurTime + TimeSpan.FromSeconds(1);
            Dirty(stab, melee);
        }

        // TODO RMC14 sounds
        // TODO RMC14 lag compensation
        var damaged = false;
        var damage = new DamageSpecifier(stab.Comp.TailDamage);
        var eve = new RMCGetTailStabBonusDamageEvent(new DamageSpecifier());
        RaiseLocalEvent(stab, ref eve);
        damage += eve.Damage;
        if (args.Entity == null ||
            TerminatingOrDeleted(args.Entity) ||
            !_xeno.CanAbilityAttackTarget(stab,args.Entity.Value, true))
        {
            var missEvent = new MeleeHitEvent(new List<EntityUid>(), stab, stab, damage, null);
            RaiseLocalEvent(stab, missEvent);

            foreach (var action in _actions.GetActions(stab))
            {
                if (TryComp(action, out XenoTailStabActionComponent? actionComp))
                    _actions.SetCooldown(action.AsNullable(), actionComp.MissCooldown);
            }
        }
        else
        {
            args.Handled = true;

            var hit = args.Entity.Value;
            var hitEvent = new MeleeHitEvent(new List<EntityUid>{hit}, stab, stab, damage, null);
            RaiseLocalEvent(stab, hitEvent);

            if (!hitEvent.Handled)
            {
                _interaction.DoContactInteraction(stab, stab);
                _interaction.DoContactInteraction(stab, hit);

                var targetPosition = _transform.GetMoverCoordinates(hit).Position;
                var userPosition = _transform.GetMoverCoordinates(stab).Position;
                var entities = GetNetEntityList(_melee.ArcRayCast(userPosition,
                        (targetPosition -
                         userPosition).ToWorldAngle(),
                        0,
                        stab.Comp.TailRange.Float(),
                        _transform.GetMapId(stab.Owner),
                        stab)
                    .ToList());

                foreach (var potentialTarget in entities)
                {
                    var target = GetEntity(potentialTarget);
                    if (!_directionBlock.IsAttackBlocked(stab, target))
                        continue;

                    hit = target;
                    break;
                }

                var filter = Filter.Pvs(transform.Coordinates, entityMan: EntityManager).RemoveWhereAttachedEntity(o => o == stab.Owner);

                var attackedEv = new AttackedEvent(stab, stab, args.Target);
                RaiseLocalEvent(hit, attackedEv);

                var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage + attackedEv.BonusDamage, hitEvent.ModifiersList);
                var change = _damageable.TryChangeDamage(hit, _xeno.TryApplyXenoSlashDamageMultiplier(hit, modifiedDamage), origin: stab , tool: stab);

                if (change?.GetTotal() > FixedPoint2.Zero)
                {
                    damaged = true;
                    _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { hit }, filter);
                }

                if (_net.IsServer)
                {
                    SpawnAttachedTo(stab.Comp.HitAnimationId, hit.ToCoordinates());

                    if (_size.TryGetSize(stab, out var size))
                    {
                        if (size >= RMCSizes.Big)
                            _daze.TryDaze(hit, stab.Comp.BigDazeTime, true);
                        else if (size == RMCSizes.Xeno)
                            _daze.TryDaze(hit, stab.Comp.DazeTime, true);
                    }
                }

                _cameraShake.ShakeCamera(hit, 2, 1);

                if (!HasComp<XenoComponent>(hit))
                {
                    if (stab.Comp.InjectNeuro &&
                        TryComp<NeurotoxinInjectorComponent>(stab, out var neuroTox))
                    {


                        if (!EnsureComp<NeurotoxinComponent>(hit, out var neuro))
                        {
                            neuro.LastMessage = _timing.CurTime;
                            neuro.LastAccentTime = _timing.CurTime;
                            neuro.LastStumbleTime = _timing.CurTime;
                        }
                        neuro.NeurotoxinAmount += neuroTox.NeuroPerSecond;
                        neuro.ToxinDamage = neuroTox.ToxinDamage;
                        neuro.OxygenDamage = neuroTox.OxygenDamage;
                        neuro.CoughDamage = neuroTox.CoughDamage;
                    }
                    else if (stab.Comp.Inject != null &&
                             _solutionContainer.TryGetInjectableSolution(hit, out var solutionEnt, out _))
                    {
                        var total = FixedPoint2.Zero;
                        foreach (var amount in stab.Comp.Inject.Values)
                        {
                            total += amount;
                        }

                        var available = solutionEnt.Value.Comp.Solution.AvailableVolume;
                        if (available < total)
                        {
                            _solutionContainer.SplitSolution(solutionEnt.Value, total - available);
                        }

                        foreach (var (reagent, amount) in stab.Comp.Inject)
                        {
                            _solutionContainer.TryAddReagent(solutionEnt.Value, reagent, amount);
                        }
                    }
                }

                var hitName = Identity.Name(hit, EntityManager, stab);
                var msg = Loc.GetString("rmc-xeno-tail-stab-self", ("target", hitName));
                if (_net.IsServer)
                    _popup.PopupEntity(msg, stab, stab);

                msg = Loc.GetString("rmc-xeno-tail-stab-target", ("user", stab));
                _popup.PopupEntity(msg, stab, hit, PopupType.MediumCaution);

                var othersFilter = Filter.PvsExcept(stab).RemovePlayerByAttachedEntity(hit);
                foreach (var other in othersFilter.Recipients)
                {
                    if (other.AttachedEntity is not { } otherEnt)
                        continue;

                    hitName = Identity.Name(hit, EntityManager, otherEnt);
                    msg = Loc.GetString("rmc-xeno-tail-stab-others", ("user", stab), ("target", hitName));
                    _popup.PopupEntity(msg, stab, othersFilter, true, PopupType.SmallCaution);
                }
            }
        }

        if (_net.IsServer)
        {
            if (args.Entity != null && !TerminatingOrDeleted(args.Entity))
            {
                var direction = _transform.GetWorldRotation(stab).GetDir();
                var angle = direction.ToAngle() - Angle.FromDegrees(180);
                _rotate.RotateXeno(stab, angle.GetDir());
            }

            var sound = args.Entity != null && damaged && !TerminatingOrDeleted(args.Entity) && args.Entity != stab ? stab.Comp.SoundHit : stab.Comp.SoundMiss;
            _audio.PlayPvs(sound, stab);
        }

        var attackEv = new MeleeAttackEvent(stab);
        RaiseLocalEvent(stab, ref attackEv);
    }

    protected virtual void DoLunge(Entity<XenoTailStabComponent, TransformComponent> user, Vector2 localPos, EntProtoId animationId)
    {
    }
}
