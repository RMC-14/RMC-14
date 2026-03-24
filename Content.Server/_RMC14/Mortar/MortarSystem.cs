using System.Numerics;
using Content.Server._RMC14.Dropship;
using Content.Server.Popups;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Mortar;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Rangefinder;
using Content.Shared.Maps;
using Robust.Server.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using static Content.Shared.Popups.PopupType;
using Content.Server.Chat.Systems;

namespace Content.Server._RMC14.Mortar;

public sealed class MortarSystem : SharedMortarSystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DropshipSystem _dropship = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    protected override bool CanLoadPopup(
        Entity<MortarComponent> mortar,
        Entity<MortarShellComponent> shell,
        EntityUid user,
        out TimeSpan travelTime,
        out MapCoordinates coordinates)
    {
        travelTime = default;
        coordinates = default;
        if (!HasSkillPopup(mortar, user, false))
            return false;

        if (!mortar.Comp.Deployed)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-not-deployed", ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        if (mortar.Comp.IsTargeting)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-targeting", ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        var time = _timing.CurTime;
        if (time < mortar.Comp.LastFiredAt + mortar.Comp.FireDelay)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-fire-cooldown", ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        var mortarCoordinates = _transform.GetMapCoordinates(mortar);

        // Get target coordinates based on targeting mode
        if (!TryGetTargetCoordinates(mortar, mortarCoordinates, user, out coordinates))
        {
            return false;
        }

        // Validate target coordinates
        if (!ValidateTargetCoordinates(mortar, shell, coordinates, mortarCoordinates, user, out travelTime))
        {
            return false;
        }

        // Apply random offset
        ApplyRandomOffset(mortar, ref coordinates);

        // Check container capacity
        if (!CheckContainerCapacity(mortar, shell, user))
        {
            return false;
        }

        return true;
    }

    private bool TryGetTargetCoordinates(Entity<MortarComponent> mortar, MapCoordinates mortarCoordinates, EntityUid user, out MapCoordinates coordinates)
    {
        coordinates = default;

        if (mortar.Comp.LaserTargetingMode)
        {
            if (mortar.Comp.LaserTargetCoordinates == null || mortar.Comp.LaserTargetCoordinates == EntityCoordinates.Invalid)
            {
                _popup.PopupEntity(Loc.GetString("rmc-mortar-no-laser-target", ("mortar", mortar)), user, user, SmallCaution);
                return false;
            }
            else if (mortar.Comp.LinkedLaserDesignator == null)
            {
                _popup.PopupEntity(Loc.GetString("rmc-mortar-no-laser-designator", ("mortar", mortar)), user, user, SmallCaution);
                return false;
            }

            coordinates = _transform.ToMapCoordinates(mortar.Comp.LaserTargetCoordinates.Value);
            return true;
        }

        // Regular coordinate targeting mode
        var target = mortar.Comp.Target + mortar.Comp.Offset + mortar.Comp.Dial;
        if (target == Vector2i.Zero)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-not-aimed", ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        coordinates = new MapCoordinates(Vector2.Zero, mortarCoordinates.MapId);
        _rmcPlanet.TryGetOffset(coordinates, out var offset);

        target -= offset;
        coordinates = coordinates.Offset(target);
        return true;
    }

    protected override bool ValidateTargetCoordinates(Entity<MortarComponent> mortar, Entity<MortarShellComponent>? shell, MapCoordinates coordinates, MapCoordinates mortarCoordinates, EntityUid? user, out TimeSpan travelTime)
    {
        travelTime = default;

        // Check if target is on planet or in space
        if (_rmcPlanet.IsOnPlanet(coordinates))
        {
            travelTime = shell?.Comp.TravelDelay ?? default;
        }
        else
        {
            if (!_dropship.AnyHijacked())
            {
                if (user != null)
                    _popup.PopupEntity(Loc.GetString("rmc-mortar-bad-idea"), user.Value, user.Value, SmallCaution);
                return false;
            }

            travelTime = shell?.Comp.WarshipTravelDelay ?? default;
        }

        // Check range
        var distance = (mortarCoordinates.Position - coordinates.Position).Length();
        if (distance < mortar.Comp.MinimumRange)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("rmc-mortar-target-too-close"), user.Value, user.Value, SmallCaution);
            return false;
        }

        if (distance > mortar.Comp.MaximumRange)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("rmc-mortar-target-too-far"), user.Value, user.Value, SmallCaution);
            return false;
        }

        // Check tile validity
        if (!_rmcMap.TryGetTileDef(coordinates, out var def) ||
            def.ID == ContentTileDefinition.SpaceID)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("rmc-mortar-target-not-area"), user.Value, user.Value, SmallCaution);
            return false;
        }

        // Check area validity
        if (!_area.TryGetArea(coordinates, out var area, out _))
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("rmc-mortar-target-not-area"), user.Value, user.Value, SmallCaution);
            return false;
        }

        if (area.Value.Comp.LandingZone)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("rmc-mortar-target-is-lz"), user.Value, user.Value, SmallCaution);
            return false;
        }

        if (!_area.CanMortarFire(_transform.ToCoordinates(coordinates)))
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("rmc-mortar-target-covered"), user.Value, user.Value, SmallCaution);
            return false;
        }

        return true;
    }

    private void ApplyRandomOffset(Entity<MortarComponent> mortar, ref MapCoordinates coordinates)
    {
        // Don't apply random offset for laser targeting mode - it should be precise
        if (mortar.Comp.LaserTargetingMode)
            return;

        if (mortar.Comp.FireRandomOffset is { Length: > 0 } fireRandomOffset)
        {
            var xDeviation = _random.Pick(fireRandomOffset);
            var yDeviation = _random.Pick(fireRandomOffset);
            coordinates = coordinates.Offset(new Vector2(xDeviation, yDeviation));
        }
    }

    private bool CheckContainerCapacity(Entity<MortarComponent> mortar, Entity<MortarShellComponent> shell, EntityUid user)
    {
        if (_container.TryGetContainer(mortar, mortar.Comp.ContainerId, out var container) &&
            !_container.CanInsert(shell, container))
        {
            _popup.PopupClient(Loc.GetString("rmc-mortar-cant-insert", ("shell", shell), ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var laserQuery = EntityQueryEnumerator<ActiveLaserDesignatorComponent>();

        // Update mortar targets based on active laser designators
        while (laserQuery.MoveNext(out var laserUid, out var laserDesignator))
        {
            if (laserDesignator.Target == null)
                continue;

            var mortarQueryPerLaser = EntityQueryEnumerator<MortarComponent>();
            while (mortarQueryPerLaser.MoveNext(out var mortarUid, out var mortar))
            {
                if (!mortar.IsTargeting && mortar.LinkedLaserDesignator == laserUid &&
                    mortar.LaserTargetCoordinates == null &&
                    mortar.LaserTargetingMode &&
                    HasComp<LaserDesignatorTargetComponent>(laserDesignator.Target.Value))
                {
                    // Get the coordinates of the laser target entity
                    var targetCoordinates = _transform.GetMoverCoordinates(laserDesignator.Target.Value);
                    TryUpdateLaserTarget((mortarUid, mortar), targetCoordinates, true, (mortar.LaserTargetDelay != TimeSpan.Zero));
                }
            }
        }

        var mortarQueryForAnnounce = EntityQueryEnumerator<MortarComponent>();
        while (mortarQueryForAnnounce.MoveNext(out var mortarUid, out var mortar))
        {
            if (mortar.NeedAnnouncement)
            {
                bool validCoordinates = false;

                if (mortar.LaserTargetCoordinates != null)
                    validCoordinates = ValidateTargetCoordinates((mortarUid, mortar), null, _transform.ToMapCoordinates(mortar.LaserTargetCoordinates.Value), _transform.GetMapCoordinates(mortarUid), null, out _);

                LocId message = validCoordinates ? "rmc-mortar-beeping" : "rmc-mortar-beeping-warning";

                _chat.TrySendInGameICMessage(mortarUid, Loc.GetString(message), InGameICChatType.Emote, false, ignoreActionBlocker: true);
                mortar.NeedAnnouncement = false;
                Dirty(mortarUid, mortar);
            }
        }
    }
}
