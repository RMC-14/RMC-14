using System.Numerics;
using Content.Server._RMC14.Dropship;
using Content.Server.Popups;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Mortar;
using Content.Shared._RMC14.Rules;
using Content.Shared.Maps;
using Robust.Server.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using static Content.Shared.Popups.PopupType;

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

        var time = _timing.CurTime;
        if (time < mortar.Comp.LastFiredAt + mortar.Comp.FireDelay)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-fire-cooldown", ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        var target = mortar.Comp.Target + mortar.Comp.Offset + mortar.Comp.Dial;
        if (target == Vector2i.Zero)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-not-aimed", ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        var mortarCoordinates = _transform.GetMapCoordinates(mortar);
        coordinates = new MapCoordinates(Vector2.Zero, mortarCoordinates.MapId);
        _rmcPlanet.TryGetOffset(coordinates, out var offset);

        target -= offset;
        coordinates = coordinates.Offset(target);

        if (_rmcPlanet.IsOnPlanet(coordinates))
        {
            travelTime = shell.Comp.TravelDelay;
        }
        else
        {
            if (!_dropship.AnyHijacked())
            {
                _popup.PopupEntity(Loc.GetString("rmc-mortar-bad-idea"), user, user, SmallCaution);
                return false;
            }

            travelTime = shell.Comp.WarshipTravelDelay;
        }

        if ((mortarCoordinates.Position - coordinates.Position).Length() < mortar.Comp.MinimumRange)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-target-too-close"), user, user, SmallCaution);
            return false;
        }

        if ((mortarCoordinates.Position - coordinates.Position).Length() > mortar.Comp.MaximumRange)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-target-too-far"), user, user, SmallCaution);
            return false;
        }

        if (!_rmcMap.TryGetTileDef(coordinates, out var def) ||
            def.ID == ContentTileDefinition.SpaceID)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-target-not-area"), user, user, SmallCaution);
            return false;
        }

        if (!_area.TryGetArea(coordinates, out var area, out _))
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-target-not-area"), user, user, SmallCaution);
            return false;
        }

        if (area.Value.Comp.LandingZone)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-target-is-lz"), user, user, SmallCaution);
            return false;
        }

        if (!_area.CanMortarFire(_transform.ToCoordinates(coordinates)))
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-target-covered"), user, user, SmallCaution);
            return false;
        }

        if (mortar.Comp.FireRandomOffset is { Length: > 0 } fireRandomOffset)
        {
            var xDeviation = _random.Pick(fireRandomOffset);
            var yDeviation = _random.Pick(fireRandomOffset);
            coordinates = coordinates.Offset(new Vector2(xDeviation, yDeviation));
        }

        if (_container.TryGetContainer(mortar, mortar.Comp.ContainerId, out var container) &&
            !_container.CanInsert(shell, container))
        {
            _popup.PopupClient(Loc.GetString("rmc-mortar-cant-insert", ("shell", shell), ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        return true;
    }
}
