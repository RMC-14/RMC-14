using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Aura;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids.Paralyzing;
using Content.Shared._RMC14.Xenonids.Paratoxin.ParatoxinSlashes;
using Content.Shared._RMC14.Xenonids.Stab;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Paratoxin;

public sealed class ParatoxinSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly CMArmorSystem _armor = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedAuraSystem _aura = default!;

    private readonly EntProtoId<SkillDefinitionComponent> ResistSkill = "RMCSkillEndurance";

    private const int ResistLevel = 5;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParatoxinAffectedComponent, MobStateChangedEvent>(OnParatoxinMobStateChanged);
        SubscribeLocalEvent<ParatoxinAffectedComponent, ComponentShutdown>(OnParatoxinRemoved);

        SubscribeLocalEvent<ParatoxinOnHitComponent, ProjectileHitEvent>(OnParatoxinOnHit, after: [typeof(CMClusterGrenadeSystem)]);

        SubscribeLocalEvent<ParatoxinCoatedSlashesComponent, MeleeHitEvent>(OnParatoxinSlashesHit);
        SubscribeLocalEvent<ParatoxinCoatedSlashesComponent, ComponentShutdown>(OnParatoxinSlashesExpire);

        SubscribeLocalEvent<CatalyticTailStabComponent, XenoAfterTailStabEvent>(OnCatalyticTailStab);

        SubscribeLocalEvent<CatalyticBuffComponent, CMGetArmorEvent>(OnCatalyticBuffGetArmor);
        SubscribeLocalEvent<CatalyticBuffComponent, RefreshMovementSpeedModifiersEvent>(OnCatalyticBuffRefreshSpeed);
    }

    private void OnParatoxinMobStateChanged(Entity<ParatoxinAffectedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemCompDeferred<ParatoxinAffectedComponent>(ent);
    }

    private void OnParatoxinRemoved(Entity<ParatoxinAffectedComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(ent, ParatoxinVisuals.Stacks, 0);
    }

    private void OnParatoxinOnHit(Entity<ParatoxinOnHitComponent> spit, ref ProjectileHitEvent args)
    {
        TryChangeStacks(args.Target, args.Shooter, spit.Comp.StacksToApply);
    }

    private void OnParatoxinSlashesHit(Entity<ParatoxinCoatedSlashesComponent> xeno, ref MeleeHitEvent args)
    {
        foreach (var hit in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, hit))
                continue;

            if (!TryChangeStacks(hit, xeno, xeno.Comp.StacksPerSlash))
                continue;

            if (--xeno.Comp.NumberOfSlashes <= 0)
                RemCompDeferred<ParatoxinCoatedSlashesComponent>(xeno);

            Dirty(xeno);

            return;
        }
    }

    private void OnParatoxinSlashesExpire(Entity<ParatoxinCoatedSlashesComponent> xeno, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoParatoxinSlashActionEvent>(xeno))
        {
            _actions.SetToggled(action.AsNullable(), false);
        }
    }

    private void OnCatalyticTailStab(Entity<CatalyticTailStabComponent> xeno, ref XenoAfterTailStabEvent args)
    {
        var stacks = GetStacks(args.Hit);
        if (stacks == 0)
            return;

        _damage.TryChangeDamage(args.Hit, xeno.Comp.DamagePerStack * stacks, origin: xeno, tool: xeno);

        if (stacks >= xeno.Comp.MinStacksToBuff)
        {
            var buff = EnsureComp<CatalyticBuffComponent>(xeno);
            buff.Armor = xeno.Comp.ArmorGain;
            buff.SpeedMultiplier = xeno.Comp.SpeedMultiplier;
            buff.ExpiresAt = _timing.CurTime + xeno.Comp.BuffDuration;
            Dirty(xeno, buff);

            _armor.UpdateArmorValue(xeno.Owner);
            _speed.RefreshMovementSpeedModifiers(xeno);
            _popup.PopupClient(Loc.GetString("rmc-xeno-catalytic-tail-stab-buff"), xeno, xeno, PopupType.Medium);
            _aura.GiveAura(xeno, xeno.Comp.BuffColor, xeno.Comp.BuffDuration, 1);
        }

        TryChangeStacks(args.Hit, xeno, (int)-(stacks * xeno.Comp.ProportialStacksToRemoveMultiplier));
    }

    private void OnCatalyticBuffGetArmor(Entity<CatalyticBuffComponent> xeno, ref CMGetArmorEvent args)
    {
        if (!xeno.Comp.Running)
            return;

        args.XenoArmor += xeno.Comp.Armor;
    }

    private void OnCatalyticBuffRefreshSpeed(Entity<CatalyticBuffComponent> xeno, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!xeno.Comp.Running)
            return;

        args.ModifySpeed(xeno.Comp.SpeedMultiplier, xeno.Comp.SpeedMultiplier);
    }

    public int GetStacks(EntityUid uid)
    {
        if (!TryComp<ParatoxinAffectedComponent>(uid, out var paratoxin))
            return 0;

        return paratoxin.Stacks;
    }

    public bool TryChangeStacks(EntityUid uid, EntityUid? source, int amount, TimeSpan? time = null, bool useGraceTime = true, bool popup = false)
    {
        if ((amount <= 0 && !HasComp<ParatoxinAffectedComponent>(uid)) || !HasComp<MarineComponent>(uid))
            return false;

        if (HasComp<SynthComponent>(uid) || HasComp<XenoComponent>(uid) || _skills.HasSkill(uid, ResistSkill, ResistLevel))
        {
            if (popup && source != null)
            {
                var immuneMsg = Loc.GetString("cm-xeno-paralyzing-slash-immune", ("target", Identity.Name(uid, EntityManager, source)));
                _popup.PopupEntity(immuneMsg, uid, source.Value, PopupType.SmallCaution);
            }
            return false;
        }

        if (!EnsureComp<ParatoxinAffectedComponent>(uid, out var paratoxin))
            paratoxin.NextEffectTime = _timing.CurTime + paratoxin.EffectEvery;

        paratoxin.Stacks = Math.Min(paratoxin.Stacks + amount, paratoxin.MaxStacks);
        paratoxin.NextDecrementTime = _timing.CurTime + (useGraceTime ? paratoxin.DecrementGraceTime : paratoxin.DecrementEvery);
        Dirty(uid, paratoxin);

        if (paratoxin.Stacks <= 0)
        {
            RemCompDeferred<ParatoxinAffectedComponent>(uid);
            return true;
        }

        _appearance.SetData(uid, ParatoxinVisuals.Stacks, paratoxin.Stacks);

        return true;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;

        // Buff runs on client for movement prediction reasons
        var queryBuff = EntityQueryEnumerator<CatalyticBuffComponent>();
        while (queryBuff.MoveNext(out var uid, out var buff))
        {
            if (time < buff.ExpiresAt)
                continue;

            RemCompDeferred<CatalyticBuffComponent>(uid);
            _armor.UpdateArmorValue(uid);
            _speed.RefreshMovementSpeedModifiers(uid);
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("rmc-xeno-catalytic-tail-stab-buff-expire"), uid, uid, PopupType.SmallCaution);
        }

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<ParatoxinAffectedComponent, DamageableComponent>();

        while (query.MoveNext(out var uid, out var paratoxin, out var damage))
        {
            if (time < paratoxin.NextEffectTime)
                continue;

            if (!damage.DamagePerGroup.TryGetValue(paratoxin.DamageGroup, out var oxy))
                continue;

            if (oxy < 50 && _proto.TryIndex(paratoxin.DamageType, out var damageType))
            {
                var damageAmount = FixedPoint2.Min([paratoxin.MaxDamage - oxy, paratoxin.DamagePerStack * paratoxin.Stacks, paratoxin.MaxDamagePerEffect]);

                var doDamage = new DamageSpecifier(damageType, damageAmount);

                _damage.TryChangeDamage(uid, doDamage, true);
            }

            paratoxin.NextEffectTime = time + paratoxin.EffectEvery;

            if (time < paratoxin.NextDecrementTime)
                continue;

            TryChangeStacks(uid, null, -1, time, false);
        }

        var querySlashes = EntityQueryEnumerator<ParatoxinCoatedSlashesComponent>();
        while (querySlashes.MoveNext(out var uid, out var slashes))
        {
            if (time < slashes.ExpiresAt)
                continue;

            RemCompDeferred<ParatoxinCoatedSlashesComponent>(uid);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-paratoxin-slashes-expire"), uid, uid, PopupType.SmallCaution);
        }
    }
}
