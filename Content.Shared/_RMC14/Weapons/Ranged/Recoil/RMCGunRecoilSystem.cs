using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged.Recoil;

public sealed class RMCGunRecoilSystem : EntitySystem
{
    [Dependency] private readonly RMCCameraShakeSystem _cameraShake = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCGunRecoilComponent, GunShotEvent>(OnRecoilGunShot);
        SubscribeLocalEvent<RMCGunRecoilComponent, GunRefreshModifiersEvent>(OnRecoilSkilledRefreshModifiers);
    }

    private void OnRecoilGunShot(Entity<RMCGunRecoilComponent> ent, ref GunShotEvent args)
    {
        if (!TryComp<GunComponent>(ent.Owner, out var gun))
            return;

        var user = args.User;

        UpdateRecoilBuildup(ent, gun);

        ent.Comp.RecoilBuildup = MathF.Min(ent.Comp.Strength + ent.Comp.RecoilBuildup, ent.Comp.MaximumRecoilBuildup);
        Dirty(ent);

        var totalRecoil = 0f;

        if (ent.Comp.HasRecoilBuildup)
            totalRecoil = ent.Comp.RecoilBuildup * 0.1f;

        if (TryComp<WieldableComponent>(ent, out var wieldable) && wieldable.Wielded)
            totalRecoil += ent.Comp.Strength;
        else
            totalRecoil += ent.Comp.StrengthUnwielded;

        var skillAmount = _skills.GetSkill(user, ent.Comp.Skill);

        if (skillAmount <= 0)
            totalRecoil += ent.Comp.UnskilledStrength;
        else
            totalRecoil -= skillAmount * ent.Comp.SkilledStrength;

        if (totalRecoil <= 0)
            return;

        var finalRecoil = (int)Math.Round(totalRecoil);

        if (totalRecoil >= 4)
            _cameraShake.ShakeCamera(user, finalRecoil, finalRecoil);
        else
            _cameraShake.ShakeCamera(user, 1, finalRecoil);
    }

    private void OnRecoilSkilledRefreshModifiers(Entity<RMCGunRecoilComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.CameraRecoilScalar = 0;
    }

    private void UpdateRecoilBuildup(Entity<RMCGunRecoilComponent> ent, GunComponent gun)
    {
        var secondsSinceFired = (float)(_timing.CurTime - gun.LastFire).TotalSeconds;
        secondsSinceFired = MathF.Max(secondsSinceFired - gun.FireRateModified * 0.3f, 0f);
        // Takes into account firerate, so that recoil cannot fall whilst firing.
        // You have to be shooting at a third of the firerate of a gun to not build up --
        // any recoil if the recoil_loss_per_second is greater than the recoil_gain_per_second

        ent.Comp.RecoilBuildup = MathF.Max(ent.Comp.RecoilBuildup - ent.Comp.RecoilLossPerSecond * secondsSinceFired, 0);
        Dirty(ent);
    }
}
