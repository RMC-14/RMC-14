using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs;
using Content.Shared.Projectiles;
using Robust.Shared.Prototypes;
using Content.Shared.Rejuvenate;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit;

public sealed partial class XenoSpitSystem : EntitySystem
{

    private static readonly ProtoId<ReagentPrototype> AcidRemovedBy = "Water";
    public void InitializeAcid()
    {
        SubscribeLocalEvent<UserAcidedComponent, ComponentRemove>(OnUserAcidedRemove);
        SubscribeLocalEvent<UserAcidedComponent, ShowFireAlertEvent>(OnUserAcidedShowFireAlert);
        SubscribeLocalEvent<UserAcidedComponent, VaporHitEvent>(OnUserAcidedVaporHit);
        SubscribeLocalEvent<UserAcidedComponent, MobStateChangedEvent>(OnUserAcidedMobStateChanged);
        SubscribeLocalEvent<UserAcidedComponent, CMGetArmorEvent>(OnUserAcidedGetArmor, after: [typeof(CMArmorSystem)]);
        SubscribeLocalEvent<UserAcidedComponent, RejuvenateEvent>(OnUserAcidedRejuvenate);

        SubscribeLocalEvent<XenoAcidOnHitComponent, ProjectileHitEvent>(OnAcidHitEvent, after: [typeof(CMClusterGrenadeSystem)]);
    }
    private void UpdateAppearance(Entity<UserAcidedComponent> acided)
    {
        var effect = acided.Comp.Appearance;
        _appearance.SetData(acided, UserAcidedVisuals.Acided, effect);
    }

    public void Resist(Entity<UserAcidedComponent?> player)
    {
        if (!Resolve(player, ref player.Comp, false))
            return;

        if (!_actionBlocker.CanInteract(player, null))
            return;

        _stun.TryParalyze(player.Owner, player.Comp.ResistDuration, true);
        player.Comp.ExpiresAt -= player.Comp.ExtinguishAmount;
        if (player.Comp.ExpiresAt <= _timing.CurTime)
        {
            _popup.PopupEntity(Loc.GetString("rmc-acid-resist"), player, player);
            RemCompDeferred<UserAcidedComponent>(player);
        }
        else
            _popup.PopupEntity(Loc.GetString("rmc-acid-resist-partial"), player, player);
    }

    private void OnUserAcidedRemove(Entity<UserAcidedComponent> ent, ref ComponentRemove args)
    {
        _appearance.SetData(ent, UserAcidedVisuals.Acided, UserAcidedEffects.None);
    }

    private void OnUserAcidedRejuvenate(Entity<UserAcidedComponent> ent, ref RejuvenateEvent args)
    {
        RemCompDeferred<UserAcidedComponent>(ent);
    }

    private void OnUserAcidedShowFireAlert(Entity<UserAcidedComponent> ent, ref ShowFireAlertEvent args)
    {
        args.Show = true;
    }

    private void OnUserAcidedVaporHit(Entity<UserAcidedComponent> ent, ref VaporHitEvent args)
    {
        if (ent.Comp.AllowVaporHitAfter > _timing.CurTime)
            return;

        var solEnt = args.Solution;
        foreach (var (_, solution) in _solution.EnumerateSolutions((solEnt, solEnt)))
        {
            if (!solution.Comp.Solution.ContainsReagent(AcidRemovedBy, null))
                continue;

            ent.Comp.ExpiresAt -= ent.Comp.ExtinguishAmount;

            if (ent.Comp.ExpiresAt <= _timing.CurTime)
            {
                RemCompDeferred<UserAcidedComponent>(ent);
            }
            else
            {
                ent.Comp.AllowVaporHitAfter = _timing.CurTime + ent.Comp.ExtinguishGracePeriod;
                Dirty(ent);
            }

            break;
        }
    }

    private void OnUserAcidedMobStateChanged(Entity<UserAcidedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemCompDeferred<UserAcidedComponent>(ent);
    }

    public void ProlongAcid(Entity<UserAcidedComponent> acided, TimeSpan addTime)
    {
        var time = _timing.CurTime;
        if (acided.Comp.ExpiresAt == null)
            return;
        acided.Comp.ExpiresAt = TimeSpan.FromSeconds(Math.Min((_timing.CurTime + acided.Comp.MaxDuration).TotalSeconds, (acided.Comp.ExpiresAt.Value + addTime).TotalSeconds));
        Dirty(acided);
    }

    public void ApplyOrExtendAcid(EntityUid ent, ProtoId<XenoAcidPrototype> acid, TimeSpan? extendTime = null)
    {
        if (!_prototypeManager.TryIndex(acid, out var acidProto))
            return;

        extendTime ??= TimeSpan.FromSeconds(10);

        if (TryComp<UserAcidedComponent>(ent, out var acided))
            ProlongAcid((ent, acided), extendTime.Value);
        else
            ApplyAcid(ent, acidProto);
    }

    public void ApplyAcid(EntityUid ent, XenoAcidPrototype acid)
    {
        var acidComp = EnsureComp<UserAcidedComponent>(ent);
        UpdateAcid((ent, acidComp), acid);
    }

    public void UpdateAcid(Entity<UserAcidedComponent> acided, XenoAcidPrototype newAcid)
    {
        acided.Comp.Damage = newAcid.Damage;
        acided.Comp.ArmorPiercing = newAcid.ArmorPiercing;
        acided.Comp.Tier = newAcid.Tier;
        acided.Comp.MaxDuration = newAcid.MaxDuration;
        acided.Comp.Appearance = newAcid.Appearance;
        acided.Comp.WeakenArmor = newAcid.WeakenArmor;
        acided.Comp.MultiplierThresholds = newAcid.MultiplierThresholds;
        acided.Comp.Upgrade = newAcid.Upgrade;

        if (acided.Comp.NextMultThreshold == null)
            acided.Comp.NextMultThreshold = _timing.CurTime + acided.Comp.MultiplierThresholds[0];

        if (acided.Comp.ExpiresAt == null)
            acided.Comp.ExpiresAt = _timing.CurTime + newAcid.DurationBase;
        else
            ProlongAcid((acided, acided.Comp), newAcid.DurationAdd);

        UpdateAppearance(acided);
        Dirty(acided);
    }

    /// <summary>
    /// Returns true if the acid was enhanced or prolonged
    /// </summary>
    /// <param name="acided"></param>
    /// <param name="maxTier"></param>
    /// <returns></returns>
    public bool EnhanceAcid(Entity<UserAcidedComponent?> acided, int maxTier)
    {
        if (!Resolve(acided, ref acided.Comp, false))
            return false;

        if (acided.Comp.Tier >= maxTier || acided.Comp.Upgrade == null)
        {
            ProlongAcid((acided, acided.Comp), TimeSpan.FromSeconds(10));
            return true;
        }

        if (!_prototypeManager.TryIndex(acided.Comp.Upgrade, out var acidProto))
            return false;

        //Acid stuff
        UpdateAcid((acided, acided.Comp), acidProto);

        Dirty(acided);
        UpdateAppearance((acided, acided.Comp));
        return true;
    }

    public void DoAcidTicks(TimeSpan time)
    {
        var acidedQuery = EntityQueryEnumerator<UserAcidedComponent>();
        while (acidedQuery.MoveNext(out var uid, out var acided))
        {
            if (acided.ExpiresAt != null && time >= acided.ExpiresAt)
            {
                RemCompDeferred<UserAcidedComponent>(uid);
                continue;
            }

            if (time >= acided.NextDamageAt)
            {
                acided.NextDamageAt = time + acided.DamageEvery;
                _damageable.TryChangeDamage(uid, acided.Damage * acided.DamageMultiplier, armorPiercing: acided.ArmorPiercing);
            }

            if (acided.NextMultThreshold == null || time <= acided.NextMultThreshold)
                continue;

            IncrementMultiplier((uid, acided), time);
        }
    }

    private void ReduceNextMultiplierTime(Entity<UserAcidedComponent> ent, TimeSpan timeToReduceBy)
    {
        if (ent.Comp.NextMultThreshold == null)
            return;

        ent.Comp.NextMultThreshold -= timeToReduceBy;

        var time = _timing.CurTime;

        if (time > ent.Comp.NextMultThreshold)
            IncrementMultiplier(ent, time);
    }

    private void IncrementMultiplier(Entity<UserAcidedComponent> ent, TimeSpan time)
    {
        //If our mult is set to 2, look at index 1 etc
        var nextMult = ent.Comp.DamageMultiplier++;
        if (ent.Comp.MultiplierThresholds.Length <= nextMult)
        {
            ent.Comp.NextMultThreshold = null;
            Dirty(ent);
            return;
        }

        ent.Comp.NextMultThreshold = ent.Comp.NextMultThreshold + ent.Comp.MultiplierThresholds[nextMult];

        Dirty(ent);
    }

    private void OnAcidHitEvent(Entity<XenoAcidOnHitComponent> ent, ref ProjectileHitEvent args)
    {
        if (!_entityWhitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, args.Target))
            return;

        if (!HasComp<UserAcidedComponent>(ent))
            ApplyOrExtendAcid(args.Target, ent.Comp.Acid, ent.Comp.ProlongDuration);
        else if (ent.Comp.Enhance)
            EnhanceAcid(args.Target, ent.Comp.MaxTier);
    }

    private void OnUserAcidedGetArmor(Entity<UserAcidedComponent> ent, ref CMGetArmorEvent args)
    {
        args.Bio = Math.Max(0, args.Bio - ent.Comp.WeakenArmor);
        args.Melee = Math.Max(0, args.Melee - ent.Comp.WeakenArmor);
    }
}
