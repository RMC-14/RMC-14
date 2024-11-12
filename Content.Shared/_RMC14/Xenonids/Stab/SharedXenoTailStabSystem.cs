using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids.GasToggle;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Neurotoxin;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
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
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const int AttackMask = (int) (CollisionGroup.MobMask | CollisionGroup.Opaque);

    protected Box2Rotated LastTailAttack;
    private int _tailStabMaxTargets;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoTailStabComponent, XenoTailStabEvent>(OnXenoTailStab);
        SubscribeLocalEvent<XenoTailStabComponent, XenoGasToggleActionEvent>(OnXenoGasToggle);

        Subs.CVar(_config, RMCCVars.RMCTailStabMaxTargets, v => _tailStabMaxTargets = v, true);
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

        var tailRange = stab.Comp.TailRange.Float();
        var box = new Box2(userCoords.Position.X - 0.10f, userCoords.Position.Y, userCoords.Position.X + 0.10f, userCoords.Position.Y + tailRange);

        var matrix = Vector2.Transform(targetCoords.Position, _transform.GetInvWorldMatrix(transform));
        var rotation = _transform.GetWorldRotation(stab).RotateVec(-matrix).ToWorldAngle();
        var boxRotated = new Box2Rotated(box, rotation, userCoords.Position);
        LastTailAttack = boxRotated;

        // ray on the left side of the box
        var leftRay = new CollisionRay(boxRotated.BottomLeft, (boxRotated.TopLeft - boxRotated.BottomLeft).Normalized(), AttackMask);

        // ray on the right side of the box
        var rightRay = new CollisionRay(boxRotated.BottomRight, (boxRotated.TopRight - boxRotated.BottomRight).Normalized(), AttackMask);

        var hive = _hive.GetHive(stab.Owner);

        bool Ignored(EntityUid uid)
        {
            if (uid == stab.Owner)
                return true;

            if (!HasComp<MobStateComponent>(uid))
                return true;

            return _hive.IsMember(uid, hive);
        }

        // dont open allocations ahead
        // entity lookups dont work properly with Box2Rotated
        // so we do one ray cast on each side instead since its narrow enough
        // im sure you could calculate the ray bounds more efficiently
        // but have you seen these allocations either way
        var intersect = _physics.IntersectRayWithPredicate(transform.MapID, leftRay, tailRange, Ignored, false);
        intersect = intersect.Concat(_physics.IntersectRayWithPredicate(transform.MapID, rightRay, tailRange, Ignored, false));
        var results = intersect.Select(r => r.HitEntity).ToHashSet();

        var actualResults = new List<EntityUid>();
        var range = stab.Comp.TailRange.Float();
        foreach (var result in results)
        {
            if (!_interaction.InRangeUnobstructed(stab.Owner, result, range: range))
                continue;

            actualResults.Add(result);
            if (actualResults.Count >= _tailStabMaxTargets)
                break;
        }

        // TODO RMC14 sounds
        // TODO RMC14 lag compensation
        var damage = new DamageSpecifier(stab.Comp.TailDamage);
        if (actualResults.Count == 0)
        {
            var missEvent = new MeleeHitEvent(new List<EntityUid>(), stab, stab, damage, null);
            RaiseLocalEvent(stab, missEvent);

            foreach (var action in _actions.GetActions(stab))
            {
                if (TryComp(action.Id, out XenoTailStabActionComponent? actionComp))
                    _actions.SetCooldown(action.Id, actionComp.MissCooldown);
            }
        }
        else
        {
            args.Handled = true;

            var hitEvent = new MeleeHitEvent(actualResults, stab, stab, damage, null);
            RaiseLocalEvent(stab, hitEvent);

            if (!hitEvent.Handled)
            {
                _interaction.DoContactInteraction(stab, stab);

                foreach (var hit in actualResults)
                {
                    _interaction.DoContactInteraction(stab, hit);
                }

                var filter = Filter.Pvs(transform.Coordinates, entityMan: EntityManager).RemoveWhereAttachedEntity(o => o == stab.Owner);
                foreach (var hit in actualResults)
                {
                    var attackedEv = new AttackedEvent(stab, stab, args.Target);
                    RaiseLocalEvent(hit, attackedEv);

                    var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage + attackedEv.BonusDamage, hitEvent.ModifiersList);
                    var change = _damageable.TryChangeDamage(hit, modifiedDamage, origin: stab);

                    if (change?.GetTotal() > FixedPoint2.Zero)
                        _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { hit }, filter);

                    if (stab.Comp.InjectNeuro)
                    {
                        if (!TryComp<NeurotoxinInjectorComponent>(stab, out var neuroTox))
                           continue;

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
                        foreach (var (reagent, amount) in stab.Comp.Inject)
                        {
                            _solutionContainer.TryAddReagent(solutionEnt.Value, reagent, amount);
                        }
                    }

                    var msg = Loc.GetString("rmc-xeno-tail-stab-self", ("target", hit));
                    if (_net.IsServer)
                        _popup.PopupEntity(msg, stab, stab);

                    msg = Loc.GetString("rmc-xeno-tail-stab-target", ("user", stab));
                    _popup.PopupEntity(msg, stab, hit, PopupType.MediumCaution);

                    msg = Loc.GetString("rmc-xeno-tail-stab-others", ("user", stab), ("target", hit));
                    var othersFilter = Filter.PvsExcept(stab).RemovePlayerByAttachedEntity(hit);
                    _popup.PopupEntity(msg, stab, othersFilter, true, PopupType.SmallCaution);
                }
            }
        }

        var localPos = transform.LocalRotation.RotateVec(matrix);

        var length = localPos.Length();
        localPos *= tailRange / length;

        DoLunge((stab, stab, transform), localPos, "WeaponArcThrust");

        var sound = actualResults.Count > 0 ? stab.Comp.SoundHit : stab.Comp.SoundMiss;
        if (_net.IsServer)
            _audio.PlayPvs(sound, stab);

        var attackEv = new MeleeAttackEvent(stab);
        RaiseLocalEvent(stab, ref attackEv);
    }

    protected virtual void DoLunge(Entity<XenoTailStabComponent, TransformComponent> user, Vector2 localPos, EntProtoId animationId)
    {
    }
}
