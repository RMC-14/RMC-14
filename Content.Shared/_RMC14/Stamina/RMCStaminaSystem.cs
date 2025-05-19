using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Deafness;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Stun;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared._RMC14.Stamina;

public sealed partial class RMCStaminaSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStutteringSystem _stutter = default!;
    [Dependency] private readonly RMCDazedSystem _daze = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly TemporarySpeedModifiersSystem _temporarySpeed = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedDeafnessSystem _deafness = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCStaminaComponent, ComponentStartup>(OnStaminaStartup);
        SubscribeLocalEvent<RMCStaminaComponent, RefreshMovementSpeedModifiersEvent>(OnStaminaMovementSpeedModify);
        SubscribeLocalEvent<RMCStaminaComponent, RejuvenateEvent>(OnStaminaRejuvenate);

        SubscribeLocalEvent<RMCStaminaDamageOnHitComponent, MeleeHitEvent>(OnStaminaOnHit);

        SubscribeLocalEvent<RMCStaminaDamageOnCollideComponent, ProjectileHitEvent>(OnStaminaOnProjectileHit);
        SubscribeLocalEvent<RMCStaminaDamageOnCollideComponent, ThrowDoHitEvent>(OnStaminaOnThrowHit);
    }

    private void OnStaminaStartup(Entity<RMCStaminaComponent> ent, ref ComponentStartup args)
    {
        SetStaminaAlert(ent);
    }

    private void OnStaminaRejuvenate(Entity<RMCStaminaComponent> ent, ref RejuvenateEvent args)
    {
        var healAmount = ent.Comp.Max - ent.Comp.Current;

        DoStaminaDamage((ent, ent.Comp), healAmount, false);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<RMCStaminaComponent>();

        while (query.MoveNext(out var uid, out var stamina))
        {
            if (stamina.Current == stamina.Max)
                continue;


            if (time >= stamina.NextRegen)
                DoStaminaDamage((uid, stamina), -stamina.RegenPerTick);
            else if (time >= stamina.NextCheck)
                ProcessEffects((uid, stamina));
        }
    }

    public void DoStaminaDamage(Entity<RMCStaminaComponent?> ent, double amount, bool visual = true)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Current = Math.Clamp(ent.Comp.Current - amount, 0, ent.Comp.Max);

        if (visual && amount > 0 && _timing.IsFirstTimePredicted)
        {
            _color.RaiseEffect(Color.Aqua, new List<EntityUid>() { ent }, Filter.Pvs(ent, entityManager: EntityManager));
        }

        //Do effects
        ProcessEffects((ent, ent.Comp));

        ent.Comp.NextRegen = _timing.CurTime + (amount > 0 ? ent.Comp.RestPeriod : ent.Comp.TimeBetweenChecks);
        SetStaminaAlert((ent, ent.Comp));
    }

    private void ProcessEffects(Entity<RMCStaminaComponent> ent)
    {
        ent.Comp.NextCheck = _timing.CurTime + ent.Comp.TimeBetweenChecks;

        int newLevel = 0;

        for (; newLevel < ent.Comp.TierThresholds.Length; newLevel++)
        {
            if (ent.Comp.Current >= ent.Comp.TierThresholds[newLevel])
                break;
        }

        if (newLevel >= 2)
        {
            _status.TryAddStatusEffect<RMCBlindedComponent>(ent, "Blinded", ent.Comp.EffectTime, true);
            _stutter.DoStutter(ent, ent.Comp.EffectTime, true);
        }

        if (newLevel >= 3 && newLevel != ent.Comp.Level)
            _daze.TryDaze(ent, ent.Comp.EffectTime, true, stutter: true);

        if (newLevel >= 4)
        {
            _deafness.TryDeafen(ent, ent.Comp.EffectTime, true, ignoreProtection: true);
            _stun.TryParalyze(ent, ent.Comp.EffectTime, true);
            _status.TryAddStatusEffect(ent, "Muted", ent.Comp.EffectTime, true, "Muted");
            _status.TryAddStatusEffect(ent, "TemporaryBlindness", ent.Comp.EffectTime, true, "TemporaryBlindness");
        }

        var oldLevel = ent.Comp.Level;

        ent.Comp.Level = newLevel;
        Dirty(ent);

        if (newLevel != oldLevel)
            _speed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnStaminaMovementSpeedModify(Entity<RMCStaminaComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Level > ent.Comp.SpeedModifiers.Length || ent.Comp.Level < 0)
            return;

        var multiplier = _temporarySpeed.CalculateSpeedModifier(ent, ent.Comp.SpeedModifiers[ent.Comp.Level]);

        if (multiplier == null)
            return;

        args.ModifySpeed(multiplier.Value, multiplier.Value);
    }

    //Same as stamina code minus eveents
    private void OnStaminaOnHit(Entity<RMCStaminaDamageOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit ||
            !args.HitEntities.Any() ||
            ent.Comp.Damage <= 0f)
        {
            return;
        }

        var stamQuery = GetEntityQuery<RMCStaminaComponent>();
        var toHit = new List<(EntityUid Entity, RMCStaminaComponent Component)>();

        // Split stamina damage between all eligible targets.
        foreach (var hit in args.HitEntities)
        {
            if (!stamQuery.TryGetComponent(hit, out var stam))
                continue;

            toHit.Add((hit, stam));
        }

        var damage = ent.Comp.Damage;

        foreach (var (hit, comp) in toHit)
        {
            DoStaminaDamage(hit, damage / toHit.Count, true);
            _adminLogger.Add(LogType.Stamina, $"{ToPrettyString(hit):target} was dealt {damage} stamina damage from {args.User} with {args.Weapon}.");
        }
    }

    private void OnStaminaOnProjectileHit(Entity<RMCStaminaDamageOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        OnCollide(ent, args.Target);
    }

    private void OnStaminaOnThrowHit(Entity<RMCStaminaDamageOnCollideComponent> ent, ref ThrowDoHitEvent args)
    {
        OnCollide(ent, args.Target);
    }

    private void OnCollide(Entity<RMCStaminaDamageOnCollideComponent> ent, EntityUid target)
    {
        if (!TryComp<RMCStaminaComponent>(target, out var stam))
            return;

        DoStaminaDamage((target, stam), ent.Comp.Damage, true);
    }

    private void SetStaminaAlert(Entity<RMCStaminaComponent> ent)
    {
        _alerts.ShowAlert(ent, ent.Comp.StaminaAlert, (short)((ent.Comp.TierThresholds.Length - 1) - ent.Comp.Level));
    }
}
