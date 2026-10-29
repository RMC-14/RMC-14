using System.Numerics;
using Content.Shared._RMC14.Hallucination;
using Content.Shared._RMC14.Hallucination.Events;
using Content.Shared._RMC14.Marines.Squads;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem
{
    private static readonly EntProtoId C4Visual = "RMCHallucinationC4";
    private static readonly EntProtoId FireVisual = "RMCHallucinationFire";
    private static readonly EntProtoId GrenadeVisual = "RMCHallucinationGrenade";
    private static readonly EntProtoId ExplosionVisual = "CMExplosionEffectGrenade";
    private static readonly EntProtoId ExplosionShockWaveVisual = "RMCExplosionEffectGrenadeShockWave";
    private static readonly EntProtoId CasImpactVisual = "RMCHallucinationCasImpact";
    private static readonly TimeSpan ExplosionTime = TimeSpan.FromSeconds(0.6);
    private static readonly TimeSpan ExplosionShockWaveTime = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan GrenadeThrowTime = TimeSpan.FromSeconds(0.4);
    private static readonly TimeSpan GrenadeFuse = TimeSpan.FromSeconds(4);

    private static readonly TimeSpan OrbitalAlertDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan OrbitalTravelDelay = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan OrbitalWarnTwoDelay = TimeSpan.FromSeconds(16);
    private static readonly TimeSpan OrbitalWarnThreeDelay = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan OrbitalImpactDelay = TimeSpan.FromSeconds(24);
    private const int OrbitalBlasts = 5;

    private static readonly TimeSpan MortarImpactWarnDelay = TimeSpan.FromSeconds(2.5);
    private static readonly TimeSpan MortarImpactDelay = TimeSpan.FromSeconds(4.5);

    private static readonly TimeSpan CasFirstShotDelay = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan CasShotDelay = TimeSpan.FromSeconds(0.1);
    private static readonly TimeSpan CasImpactTime = TimeSpan.FromSeconds(0.5);
    private const int CasShots = 10;
    private const int CasSoundEveryShots = 3;
    private const float CasSpread = 3;

    private void InitializeDanger()
    {
        SubscribeLocalEvent<RMCHallucinationDangerFlashComponent, RMCHallucinateEvent>(OnDangerFlash);
        SubscribeLocalEvent<RMCHallucinationFakeGrenadeComponent, RMCHallucinateEvent>(OnFakeGrenade);
        SubscribeLocalEvent<RMCHallucinationFakeOrbitalStrikeComponent, RMCHallucinateEvent>(OnFakeOrbitalStrike);
        SubscribeLocalEvent<RMCHallucinationFakeMortarComponent, RMCHallucinateEvent>(OnFakeMortar);
        SubscribeLocalEvent<RMCHallucinationFakeCasComponent, RMCHallucinateEvent>(OnFakeCas);
        SubscribeLocalEvent<RMCHallucinationLightFlickerComponent, RMCHallucinateEvent>(OnLightFlicker);
    }

    private void OnDangerFlash(Entity<RMCHallucinationDangerFlashComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = DangerFlash(args.Victim);
    }

    private void OnFakeGrenade(Entity<RMCHallucinationFakeGrenadeComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeGrenade(args.Victim);
    }

    private void OnFakeOrbitalStrike(Entity<RMCHallucinationFakeOrbitalStrikeComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeOrbitalStrike(args.Victim);
    }

    private void OnFakeMortar(Entity<RMCHallucinationFakeMortarComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeMortar(args.Victim);
    }

    private void OnFakeCas(Entity<RMCHallucinationFakeCasComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeCas(args.Victim);
    }

    private void OnLightFlicker(Entity<RMCHallucinationLightFlickerComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = LightFlicker(args.Victim);
    }

    private bool DangerFlash(Entity<RMCHallucinatingComponent> ent)
    {
        var time = _timing.CurTime;
        if (time < ent.Comp.ImageEnd || !TryPickFloorInView(ent, out var coords))
            return false;

        var duration = RandomTime(ent.Comp.FlashDurationMin, ent.Comp.FlashDurationMax);
        ent.Comp.ImageEnd = time + duration;

        var c4 = _random.Prob(0.5f);
        SendToVictim(ent, new RMCHallucinationVisualEvent(c4 ? C4Visual : FireVisual, null, GetNetCoordinates(coords), duration));

        if (c4)
        {
            for (var beep = TimeSpan.Zero; beep < duration; beep += ent.Comp.C4BeepEvery)
            {
                AddStep(ent, beep, new RMCHallucinationStep { Sound = ent.Comp.C4BeepSound, Coordinates = coords });
            }
        }

        return true;
    }

    private bool FakeGrenade(Entity<RMCHallucinatingComponent> ent)
    {
        if (Transform(ent).GridUid is not { } grid ||
            !TryComp(grid, out MapGridComponent? gridComp) ||
            !TryGetLocalPosition(ent, grid, out var victim))
        {
            return false;
        }

        var landing = victim + _random.NextAngle().ToVec();
        if (IsBlocked((grid, gridComp), victim, landing - victim))
            landing = victim;

        var thrownFrom = landing + _random.NextAngle().ToVec() * 3;
        if (IsBlocked((grid, gridComp), landing, thrownFrom - landing))
            thrownFrom = landing;

        var landingCoords = new EntityCoordinates(grid, landing);
        SendToVictim(ent, new RMCHallucinationMovingVisualEvent(
            GrenadeVisual,
            null,
            GetNetCoordinates(new EntityCoordinates(grid, thrownFrom)),
            GetNetCoordinates(landingCoords),
            TimeSpan.Zero,
            GrenadeThrowTime,
            GrenadeFuse,
            true));

        _audio.PlayStatic(ent.Comp.GrenadeSound, ent, landingCoords);
        AddStep(ent,
            GrenadeThrowTime + GrenadeFuse,
            new RMCHallucinationStep { Sound = ent.Comp.ExplosionSound, Coordinates = landingCoords, Explosion = true });

        return true;
    }

    private bool FakeOrbitalStrike(Entity<RMCHallucinatingComponent> ent)
    {
        if (_timing.CurTime < ent.Comp.StrikeEnd ||
            !HasComp<SquadMemberComponent>(ent) ||
            !_planet.IsOnPlanet(Transform(ent)) ||
            ent.Comp.OrbitalWarheads.Count == 0 ||
            !_prototypes.TryIndex(_random.Pick(ent.Comp.OrbitalWarheads), out var warhead))
        {
            return false;
        }

        var target = OffsetFromVictim(ent, 4, 10);
        ent.Comp.StrikeEnd = _timing.CurTime + OrbitalImpactDelay + TimeSpan.FromSeconds(2);

        AddStep(ent, OrbitalAlertDelay, new RMCHallucinationStep
        {
            Sound = ent.Comp.OrbitalAlertSound,
            Message = Loc.GetString("rmc-ob-launch-detected"),
        });

        if (_area.TryGetArea(target, out _, out var area))
        {
            AddStep(ent, OrbitalAlertDelay, new RMCHallucinationStep
            {
                Message = Loc.GetString("rmc-ob-launch-area", ("warhead", warhead.Name), ("area", area.Name)),
            });
        }

        AddStep(ent, OrbitalTravelDelay, new RMCHallucinationStep
        {
            Coordinates = target,
            Sound = ent.Comp.OrbitalTravelSound,
            Warning = "rmc-ob-warning-one",
            WarningAbove = "rmc-ob-warning-above-one",
            WarningChat = true,
        });

        AddStep(ent, OrbitalWarnTwoDelay, new RMCHallucinationStep
        {
            Coordinates = target,
            Warning = "rmc-ob-warning-two",
            WarningAbove = "rmc-ob-warning-above-two",
            WarningChat = true,
        });

        AddStep(ent, OrbitalWarnThreeDelay, new RMCHallucinationStep
        {
            Coordinates = target,
            Warning = "rmc-ob-warning-three",
            WarningAbove = "rmc-ob-warning-above-three",
            WarningChat = true,
        });

        AddStep(ent, OrbitalImpactDelay, new RMCHallucinationStep
        {
            Coordinates = target,
            Sound = ent.Comp.BigExplosionSound,
            Explosion = true,
        });

        for (var i = 1; i < OrbitalBlasts; i++)
        {
            AddStep(ent, OrbitalImpactDelay + TimeSpan.FromSeconds(0.15 * i), new RMCHallucinationStep
            {
                Coordinates = target.Offset(_random.NextAngle().ToVec() * _random.NextFloat(1.5f, 3f)),
                Explosion = true,
            });
        }

        return true;
    }

    private bool FakeMortar(Entity<RMCHallucinatingComponent> ent)
    {
        if (_timing.CurTime < ent.Comp.StrikeEnd || !_planet.IsOnPlanet(Transform(ent)))
            return false;

        var shells = _random.Next(1, 4);
        var start = TimeSpan.Zero;
        for (var i = 0; i < shells; i++)
        {
            var target = OffsetFromVictim(ent, 3, 8);
            AddStep(ent, start, new RMCHallucinationStep
            {
                Coordinates = target,
                Sound = ent.Comp.MortarTravelSound,
                Warning = "rmc-mortar-shell-warning",
                WarningAbove = "rmc-mortar-shell-warning-above",
            });

            AddStep(ent, start + MortarImpactWarnDelay, new RMCHallucinationStep
            {
                Coordinates = target,
                Warning = "rmc-mortar-shell-impact-warning",
                WarningAbove = "rmc-mortar-shell-impact-warning-above",
            });

            AddStep(ent, start + MortarImpactDelay, new RMCHallucinationStep
            {
                Coordinates = target,
                Sound = ent.Comp.ExplosionSound,
                Explosion = true,
            });

            start += RandomTime(1, 2);
        }

        ent.Comp.StrikeEnd = _timing.CurTime + start + MortarImpactDelay;
        return true;
    }

    private bool FakeCas(Entity<RMCHallucinatingComponent> ent)
    {
        if (_timing.CurTime < ent.Comp.StrikeEnd || !_planet.IsOnPlanet(Transform(ent)))
            return false;

        var target = OffsetFromVictim(ent, 4, 8);
        AddStep(ent, TimeSpan.Zero, new RMCHallucinationStep { Coordinates = target, Sound = ent.Comp.CasFireSound });

        for (var i = 0; i < CasShots; i++)
        {
            var spread = new Vector2(_random.NextFloat(-CasSpread, CasSpread), _random.NextFloat(-CasSpread, CasSpread));
            AddStep(ent, CasFirstShotDelay + CasShotDelay * i, new RMCHallucinationStep
            {
                Coordinates = target.Offset(spread),
                Sound = i % CasSoundEveryShots == 0 ? ent.Comp.CasImpactSound : null,
                Visual = CasImpactVisual,
                VisualDuration = CasImpactTime,
            });
        }

        ent.Comp.StrikeEnd = _timing.CurTime + CasFirstShotDelay + CasShotDelay * CasShots;
        return true;
    }

    private bool LightFlicker(Entity<RMCHallucinatingComponent> ent)
    {
        var time = _timing.CurTime;
        if (time < ent.Comp.LightsEnd)
            return false;

        var duration = RandomTime(ent.Comp.LightsDurationMin, ent.Comp.LightsDurationMax);
        ent.Comp.LightsEnd = time + duration;
        SendToVictim(ent, new RMCHallucinationLightsEvent(duration, ent.Comp.ViewRange + 2));

        if (_random.Prob(ent.Comp.LightsHissChance))
        {
            AddStep(ent, duration, new RMCHallucinationStep
            {
                Coordinates = OffsetFromVictim(ent, 2, 4),
                Sound = ent.Comp.HissSound,
            });
        }

        return true;
    }

    private void FakeExplosion(Entity<RMCHallucinatingComponent> ent, EntityCoordinates coords)
    {
        var netCoords = GetNetCoordinates(coords);
        SendToVictim(ent, new RMCHallucinationVisualEvent(ExplosionShockWaveVisual, null, netCoords, ExplosionShockWaveTime));
        SendToVictim(ent, new RMCHallucinationVisualEvent(ExplosionVisual, null, netCoords, ExplosionTime));
    }
}
