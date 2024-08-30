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
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override bool CanLoadPopup(
        Entity<MortarComponent> mortar,
        Entity<MortarShellComponent> shell,
        EntityUid user,
        out TimeSpan travelTime,
        out EntityCoordinates coordinates)
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

        coordinates = _transform.GetMoverCoordinates(mortar);
        var target = mortar.Comp.Target + mortar.Comp.Offset + mortar.Comp.Dial;
        if (target == Vector2i.Zero)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-not-aimed", ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        if (!_rmcMap.TryGetTileDef(coordinates, out var def) ||
            def.ID == ContentTileDefinition.SpaceID)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-not-aimed", ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        if (!_area.TryGetArea(coordinates, out _, out var area))
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-target-not-area"), user, user, SmallCaution);
            return false;
        }

        if (area.LandingZone)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-target-is-lz"), user, user, SmallCaution);
            return false;
        }

        if (!area.Mortar)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-target-covered"), user, user, SmallCaution);
            return false;
        }

        _rmcPlanet.TryGetOffset(coordinates, out var offset);
        if (_rmcPlanet.IsOnPlanet(coordinates))
        {
            travelTime = shell.Comp.TravelDelay;
            var deviation = shell.Comp.PlanetDeviation;
            var xDeviation = _random.Next(-deviation, deviation + 1);
            var yDeviation = _random.Next(-deviation, deviation + 1);
            offset += (xDeviation, yDeviation);
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

        target -= offset;
        coordinates = coordinates.Offset(target);

        if (_container.TryGetContainer(mortar, mortar.Comp.ContainerId, out var container) &&
            !_container.CanInsert(shell, container))
        {
            _popup.PopupClient(Loc.GetString("rmc-mortar-cant-insert", ("shell", shell), ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        return true;
    }
}
