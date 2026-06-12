using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Emplacements;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Sentry;
using Content.Shared._RMC14.Xenonids.Construction.DeployedTraps;
using Content.Shared._RMC14.Xenonids.DeployTraps;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.AcidMine;

public sealed class XenoAcidBlastSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MountableWeaponSystem _mg = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    private readonly HashSet<EntityUid> _targets = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAcidBlastComponent, MapInitEvent>(OnBlastInit);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoAcidBlastComponent>();
        while (query.MoveNext(out var uid, out var blast))
        {
            if (blast.Activated || blast.Activation == TimeSpan.Zero)
                continue;

            if (_timing.CurTime < blast.Activation)
                continue;

            OnBlastActivate((uid, blast));
        }
    }

    private void OnBlastInit(Entity<XenoAcidBlastComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Activation = _timing.CurTime + ent.Comp.Delay;
        Dirty(ent.Owner, ent.Comp);
        if (!_net.IsClient)
        {
            var telegraph = SpawnAtPosition(ent.Comp.TelegraphEffect, Transform(ent.Owner).Coordinates);
            _transform.SetParent(telegraph, ent.Owner);
        }
    }

    private void OnBlastActivate(Entity<XenoAcidBlastComponent> ent)
    {
        if (ent.Comp.Activated)
            return;

        ent.Comp.Activated = true;
        Dirty(ent, ent.Comp);

        var coords = Transform(ent.Owner).Coordinates;

        if (!_net.IsClient)
        {
            _audio.PlayPvs(ent.Comp.ExplosionSound, coords);
            SpawnAtPosition(ent.Comp.SmokeEffect, coords);
        }

        var hits = ProcessBlastHits(ent);

        if (!_net.IsClient && hits > 0 && ent.Comp.Attached is { } attachedXeno)
        {
            RefreshCooldowns(attachedXeno, hits, ent.Comp);
        }

        if (_net.IsClient && !IsClientSide(ent))
            return;

        QueueDel(ent);
    }

    private int ProcessBlastHits(Entity<XenoAcidBlastComponent> ent)
    {
        var hits = 0;
        var position = _transform.GetMapCoordinates(ent);
        _targets.Clear();
        _lookup.GetEntitiesInRange(position.MapId, position.Position, ent.Comp.BlastRadius, _targets, LookupFlags.Uncontained);

        foreach (var target in _targets)
        {
            if (target == ent.Comp.Attached)
                continue;

            if (!ent.Comp.AlreadyHit.Add(target))
                continue;

            if (!CanHit(ent, target))
                continue;

            _audio.PlayPredicted(
                ent.Comp.SizzleSound,
                target,
                null,
                ent.Comp.SizzleSound.Params.WithVolume(-10f));


            if (!_net.IsClient)
            {
                if (!HasComp<MobStateComponent>(target))
                {
                    ApplyStructureDamage(ent, target);
                }
                else
                {
                    ApplyMobDamage(ent, target);
                    hits++;
                }
            }
        }

        return hits;
    }

    private bool CanHit(Entity<XenoAcidBlastComponent> ent, EntityUid target)
    {
        return ent.Comp.Attached is { } attached &&
               (_xeno.CanAbilityAttackTarget(attached, target, true, true) || IsValidStructure(ent, target));
    }

    private bool IsValidStructure(Entity<XenoAcidBlastComponent> ent, EntityUid target)
    {
        return !HasComp<MobStateComponent>(target) &&
               !_hive.FromSameHive(ent.Owner, target) &&
               HasComp<DamageableComponent>(target) &&
               HasComp<BarricadeComponent>(target);
    }

    private void ApplyStructureDamage(Entity<XenoAcidBlastComponent> ent, EntityUid target)
    {
        var damageTarget = ResolveDamageTarget(target);
        var structureDamage = ent.Comp.Empowered
            ? ent.Comp.BaseDamage * ent.Comp.EmpoweredStructureDamageMultiplier
            : ent.Comp.BaseDamage * ent.Comp.StructureDamageMultiplier;
        _damage.TryChangeDamage(damageTarget, structureDamage, origin: ent.Comp.Attached);
    }

    private void ApplyMobDamage(Entity<XenoAcidBlastComponent> ent, EntityUid target)
    {
        var mobDamage = ent.Comp.BaseDamage;

        if (ent.Comp.Empowered)
            mobDamage = mobDamage * ent.Comp.EmpoweredMobDamageMultiplier;

        if (TryComp<XenoCaughtInTrapComponent>(target, out var caught) && caught.Applier == ent.Comp.Attached)
            mobDamage = mobDamage * ent.Comp.TrappedMobDamageMultiplier;

        var change = _damage.TryChangeDamage(target, mobDamage, origin: ent.Comp.Attached);
        if (change?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(target, entityManager: EntityManager);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { target }, filter);
        }

        if (ent.Comp.Empowered)
            ApplyOrExtendAcid(ent, target);
    }

    private void ApplyOrExtendAcid(Entity<XenoAcidBlastComponent> ent, EntityUid target)
    {
        if (TryComp(target, out UserAcidedComponent? existing))
        {
            existing.ExpiresAt += ent.Comp.AcidProlongDuration;
            Dirty(target, existing);
        }
        else
        {
            var acided = EnsureComp<UserAcidedComponent>(target);
            acided.Duration = ent.Comp.AcidDuration;
            acided.Damage = ent.Comp.AcidDamage;
            acided.ArmorPiercing = ent.Comp.AcidArmorPiercing;
            Dirty(target, acided);
        }
    }

    private void RefreshCooldowns(EntityUid xeno, int hits, XenoAcidBlastComponent blast)
    {
        foreach (var action in _actions.GetActions(xeno))
        {
            if (_actions.GetEvent(action) is not XenoDeployTrapsActionEvent)
                continue;

            if (action.Comp.Cooldown == null)
                continue;

            var cooldownEnd = action.Comp.Cooldown.Value.End - blast.DeployTrapsCooldownReduction * hits;
            if (cooldownEnd < action.Comp.Cooldown.Value.Start)
                _actions.ClearCooldown(action.AsNullable());
            else
                _actions.SetCooldown(action.AsNullable(), action.Comp.Cooldown.Value.Start, cooldownEnd);
        }
    }

    private EntityUid ResolveDamageTarget(EntityUid target)
    {
        if (_mg.TryGetWeaponMount(target, out var mount))
            return mount.Value;
        return target;
    }
}
