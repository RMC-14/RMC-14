using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    private void InitializeRanged()
    {
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableAlteredEvent>(OnRangedModsAltered);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableGetExamineDataEvent>(OnRangedModsGetExamineData);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableRelayedEvent<GetFireModesEvent>>(OnRangedGetFireModes);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableRelayedEvent<GetFireModeValuesEvent>>(OnRangedModsGetFireModeValues);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableRelayedEvent<GetDamageFalloffEvent>>(OnRangedModsGetDamageFalloff);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableRelayedEvent<GetGunDamageModifierEvent>>(OnRangedModsGetGunDamage);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableRelayedEvent<GetWeaponAccuracyEvent>>(OnRangedModsGetWeaponAccuracy);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableRelayedEvent<GunGetAmmoSpreadEvent>>(OnRangedModsGetScatterFlat);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableRelayedEvent<GunRefreshModifiersEvent>>(OnRangedModsRefreshModifiers);
    }

    private void OnRangedModsGetExamineData(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableGetExamineDataEvent args)
    {
        foreach (var modSet in attachable.Comp.Modifiers)
        {
            var key = GetExamineKey(modSet.Conditions);

            if (!args.Data.ContainsKey(key))
                args.Data[key] = new (modSet.Conditions, GetEffectStrings(modSet));
            else
                args.Data[key].effectStrings.AddRange(GetEffectStrings(modSet));
        }
    }

    private List<string> GetEffectStrings(AttachableWeaponRangedModifierSet modSet)
    {
        var result = new List<string>();

        if (modSet.AccuracyAddMult != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-ranged-accuracy",
                ("colour", modifierExamineColour), ("sign", modSet.AccuracyAddMult > 0 ? '+' : ""), ("accuracy", modSet.AccuracyAddMult)));

        if (modSet.ScatterFlat != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-ranged-scatter",
                ("colour", modifierExamineColour), ("sign", modSet.ScatterFlat > 0 ? '+' : ""), ("scatter", modSet.ScatterFlat)));

        if (modSet.BurstScatterAddMult != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-ranged-burst-scatter",
                ("colour", modifierExamineColour), ("sign", modSet.BurstScatterAddMult > 0 ? '+' : ""), ("burstScatterMult", modSet.BurstScatterAddMult)));

        if (modSet.ShotsPerBurstFlat != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-ranged-shots-per-burst",
                ("colour", modifierExamineColour), ("sign", modSet.ShotsPerBurstFlat > 0 ? '+' : ""), ("shots", modSet.ShotsPerBurstFlat)));

        if (modSet.FireDelayFlat != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-ranged-fire-delay",
                ("colour", modifierExamineColour), ("sign", modSet.FireDelayFlat > 0 ? '+' : ""), ("fireDelay", modSet.FireDelayFlat)));

        if (modSet.RecoilFlat != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-ranged-recoil",
                ("colour", modifierExamineColour), ("sign", modSet.RecoilFlat > 0 ? '+' : ""), ("recoil", modSet.RecoilFlat)));

        if (modSet.DamageAddMult != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-ranged-damage",
                ("colour", modifierExamineColour), ("sign", modSet.DamageAddMult > 0 ? '+' : ""), ("damage", modSet.DamageAddMult)));

        if (modSet.ProjectileSpeedFlat != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-ranged-projectile-speed",
                ("colour", modifierExamineColour), ("sign", modSet.ProjectileSpeedFlat > 0 ? '+' : ""), ("projectileSpeed", modSet.ProjectileSpeedFlat)));

        if (modSet.DamageFalloffAddMult != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-ranged-damage-falloff",
                ("colour", modifierExamineColour), ("sign", modSet.DamageFalloffAddMult > 0 ? '+' : ""), ("falloff", modSet.DamageFalloffAddMult)));

        if (modSet.RangeFlat != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-ranged-range",
                ("colour", modifierExamineColour), ("sign", modSet.RangeFlat > 0 ? '+' : ""), ("falloff", modSet.RangeFlat)));

        return result;
    }

    private void OnRangedModsAltered(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch(args.Alteration)
        {
            case AttachableAlteredType.AppearanceChanged:
                break;

            case AttachableAlteredType.DetachedDeactivated:
                break;

            default:
                _cmGunSystem.RefreshGunDamageMultiplier(args.Holder);

                if (attachable.Comp.FireModeMods != null)
                    _rmcSelectiveFireSystem.RefreshFireModes(args.Holder, true);

                _rmcSelectiveFireSystem.RefreshModifiableFireModeValues(args.Holder);
                break;
        }
    }

    private void OnRangedModsRefreshModifiers(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableRelayedEvent<GunRefreshModifiersEvent> args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
                continue;

            args.Args.ShotsPerBurst = Math.Max(args.Args.ShotsPerBurst + modSet.ShotsPerBurstFlat, 1);
            args.Args.CameraRecoilScalar = Math.Max(args.Args.CameraRecoilScalar + modSet.RecoilFlat, 0);
            args.Args.MinAngle = Angle.FromDegrees(Math.Max(args.Args.MinAngle.Degrees + modSet.ScatterFlat, 0.0));
            args.Args.MaxAngle = Angle.FromDegrees(Math.Max(args.Args.MaxAngle.Degrees + modSet.ScatterFlat, args.Args.MinAngle));
            args.Args.ProjectileSpeed += modSet.ProjectileSpeedFlat;

            // Fire delay doesn't work quite like SS14 fire rate, so we're having to do maths:
            // Fire rate is shots per second. Fire delay is the interval between shots. They are inversely proportionate to each other.
            // First we divide 1 second by the fire rate to get our current fire delay, then we add the delay modifier, then we divide 1 by the result again to get the modified fire rate.
            var fireDelayMod = args.Args.Gun.Comp.SelectedMode == SelectiveFire.Burst ? modSet.FireDelayFlat / 2f : modSet.FireDelayFlat;
            var fireRate = 1f / (1f / args.Args.FireRate + fireDelayMod);
            if (float.IsInfinity(fireRate))
                continue;

            args.Args.FireRate = fireRate;
        }
    }

    private void OnRangedGetFireModes(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableRelayedEvent<GetFireModesEvent> args)
    {
        if (attachable.Comp.FireModeMods == null)
            return;

        foreach (var modSet in attachable.Comp.FireModeMods)
        {
            if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
                continue;

            args.Args.Modes |= modSet.ExtraFireModes;
            args.Args.Set = modSet.SetFireMode;
        }
    }

    private void OnRangedModsGetDamageFalloff(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableRelayedEvent<GetDamageFalloffEvent> args)
    {
        foreach (var modSet in attachable.Comp.Modifiers)
        {
            if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
                continue;

            args.Args.FalloffMultiplier += modSet.DamageFalloffAddMult;
            args.Args.Range += modSet.RangeFlat;
        }
    }

    private void OnRangedModsGetGunDamage(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableRelayedEvent<GetGunDamageModifierEvent> args)
    {
        foreach (var modSet in attachable.Comp.Modifiers)
        {
            if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
                continue;

            args.Args.Multiplier += modSet.DamageAddMult;
        }
    }

    private void OnRangedModsGetWeaponAccuracy(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableRelayedEvent<GetWeaponAccuracyEvent> args)
    {
        foreach (var modSet in attachable.Comp.Modifiers)
        {
            if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
                continue;

            args.Args.AccuracyMultiplier += modSet.AccuracyAddMult;
            args.Args.Range += modSet.RangeFlat;
        }
    }

    private void OnRangedModsGetFireModeValues(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableRelayedEvent<GetFireModeValuesEvent> args)
    {
        foreach (var modSet in attachable.Comp.Modifiers)
        {
            if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
                continue;

            args.Args.BurstScatterMult += modSet.BurstScatterAddMult;
        }
    }

    private void OnRangedModsGetScatterFlat(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableRelayedEvent<GunGetAmmoSpreadEvent> args)
    {
        foreach (var modSet in attachable.Comp.Modifiers)
        {
            if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
                continue;

            args.Args.Spread += Angle.FromDegrees(modSet.ScatterFlat) / 2;
            if (args.Args.Spread < 0)
                args.Args.Spread = 0;
        }
    }
}
