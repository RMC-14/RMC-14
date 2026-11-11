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
        args.Handled = FakeGrenade(args.Victim, ent.Comp);
    }

    private void OnFakeOrbitalStrike(Entity<RMCHallucinationFakeOrbitalStrikeComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeOrbitalStrike(args.Victim, ent.Comp);
    }

    private void OnFakeMortar(Entity<RMCHallucinationFakeMortarComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeMortar(args.Victim, ent.Comp);
    }

    private void OnFakeCas(Entity<RMCHallucinationFakeCasComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeCas(args.Victim, ent.Comp);
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

    private bool FakeGrenade(Entity<RMCHallucinatingComponent> ent, RMCHallucinationFakeGrenadeComponent grenade)
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
            grenade.ThrowTime,
            grenade.Fuse,
            ent.Comp.GrenadeSpinSpeed));

        _audio.PlayStatic(ent.Comp.GrenadeSound, ent, landingCoords);
        AddStep(ent,
            grenade.ThrowTime + grenade.Fuse,
            new RMCHallucinationStep { Sound = ent.Comp.ExplosionSound, Coordinates = landingCoords, Explosion = true });

        return true;
    }

    private bool FakeOrbitalStrike(Entity<RMCHallucinatingComponent> ent, RMCHallucinationFakeOrbitalStrikeComponent orbital)
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
        ent.Comp.StrikeEnd = _timing.CurTime + orbital.ImpactDelay + TimeSpan.FromSeconds(2);

        AddStep(ent, orbital.AlertDelay, new RMCHallucinationStep
        {
            Sound = ent.Comp.OrbitalAlertSound,
            Message = Loc.GetString("rmc-ob-launch-detected"),
        });

        if (_area.TryGetArea(target, out _, out var area))
        {
            AddStep(ent, orbital.AlertDelay, new RMCHallucinationStep
            {
                Message = Loc.GetString("rmc-ob-launch-area", ("warhead", warhead.Name), ("area", area.Name)),
            });
        }

        AddStep(ent, orbital.TravelDelay, new RMCHallucinationStep
        {
            Coordinates = target,
            Sound = ent.Comp.OrbitalTravelSound,
            Warning = "rmc-ob-warning-one",
            WarningAbove = "rmc-ob-warning-above-one",
            WarningChat = true,
        });

        AddStep(ent, orbital.WarnTwoDelay, new RMCHallucinationStep
        {
            Coordinates = target,
            Warning = "rmc-ob-warning-two",
            WarningAbove = "rmc-ob-warning-above-two",
            WarningChat = true,
        });

        AddStep(ent, orbital.WarnThreeDelay, new RMCHallucinationStep
        {
            Coordinates = target,
            Warning = "rmc-ob-warning-three",
            WarningAbove = "rmc-ob-warning-above-three",
            WarningChat = true,
        });

        AddStep(ent, orbital.ImpactDelay, new RMCHallucinationStep
        {
            Coordinates = target,
            Sound = ent.Comp.BigExplosionSound,
            Explosion = true,
        });

        for (var i = 1; i < orbital.Blasts; i++)
        {
            AddStep(ent, orbital.ImpactDelay + TimeSpan.FromSeconds(0.15 * i), new RMCHallucinationStep
            {
                Coordinates = target.Offset(_random.NextAngle().ToVec() * _random.NextFloat(1.5f, 3f)),
                Explosion = true,
            });
        }

        return true;
    }

    private bool FakeMortar(Entity<RMCHallucinatingComponent> ent, RMCHallucinationFakeMortarComponent mortar)
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

            AddStep(ent, start + mortar.ImpactWarnDelay, new RMCHallucinationStep
            {
                Coordinates = target,
                Warning = "rmc-mortar-shell-impact-warning",
                WarningAbove = "rmc-mortar-shell-impact-warning-above",
            });

            AddStep(ent, start + mortar.ImpactDelay, new RMCHallucinationStep
            {
                Coordinates = target,
                Sound = ent.Comp.ExplosionSound,
                Explosion = true,
            });

            start += RandomTime(1, 2);
        }

        ent.Comp.StrikeEnd = _timing.CurTime + start + mortar.ImpactDelay;
        return true;
    }

    private bool FakeCas(Entity<RMCHallucinatingComponent> ent, RMCHallucinationFakeCasComponent cas)
    {
        if (_timing.CurTime < ent.Comp.StrikeEnd || !_planet.IsOnPlanet(Transform(ent)))
            return false;

        var target = OffsetFromVictim(ent, 4, 8);
        AddStep(ent, TimeSpan.Zero, new RMCHallucinationStep { Coordinates = target, Sound = ent.Comp.CasFireSound });

        for (var i = 0; i < cas.Shots; i++)
        {
            var spread = new Vector2(_random.NextFloat(-cas.Spread, cas.Spread), _random.NextFloat(-cas.Spread, cas.Spread));
            AddStep(ent, cas.FirstShotDelay + cas.ShotDelay * i, new RMCHallucinationStep
            {
                Coordinates = target.Offset(spread),
                Sound = i % cas.SoundEveryShots == 0 ? ent.Comp.CasImpactSound : null,
                Visual = CasImpactVisual,
                VisualDuration = cas.ImpactTime,
            });
        }

        ent.Comp.StrikeEnd = _timing.CurTime + cas.FirstShotDelay + cas.ShotDelay * cas.Shots;
        return true;
    }

    private bool LightFlicker(Entity<RMCHallucinatingComponent> ent)
    {
        var time = _timing.CurTime;
        if (time < ent.Comp.LightsEnd)
            return false;

        var duration = RandomTime(ent.Comp.LightsDurationMin, ent.Comp.LightsDurationMax);
        ent.Comp.LightsEnd = time + duration;
        SendToVictim(ent, new RMCHallucinationLightsEvent(
            duration,
            ent.Comp.ViewRange + 2,
            ent.Comp.LightsOnMin,
            ent.Comp.LightsOnMax,
            ent.Comp.LightsOffMin,
            ent.Comp.LightsOffMax,
            ent.Comp.LightsBrownoutDepth));

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
        SendToVictim(ent, new RMCHallucinationVisualEvent(ExplosionShockWaveVisual, null, netCoords, ent.Comp.ExplosionShockWaveTime));
        SendToVictim(ent, new RMCHallucinationVisualEvent(ExplosionVisual, null, netCoords, ent.Comp.ExplosionTime));
    }
}
