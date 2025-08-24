using Content.Client.Weapons.Melee;
using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Stealth;
using Content.Shared._RMC14.Tackle;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Client._RMC14.Weapons.Melee;

public sealed class RMCMeleeWeaponSystem : SharedRMCMeleeWeaponSystem
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MeleeWeaponSystem _melee = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(CMKeyFunctions.CMXenoWideSwing,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity != null)
                        TryPrimaryHeavyAttack();
                }, handle: false))
            .Register<RMCMeleeWeaponSystem>();
    }

    private void TryPrimaryHeavyAttack()
    {
        var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);
        EntityUid grid;

        if (_mapManager.TryFindGridAt(mousePos, out var gridUid, out _))
            grid = gridUid;
        else if (_map.TryGetMap(mousePos.MapId, out var map))
            grid = map.Value;
        else
            return;

        var coordinates = _transform.ToCoordinates(grid, mousePos);

        if (_player.LocalEntity is not { } entity)
            return;

        if (!_melee.TryGetWeapon(entity, out var weaponUid, out var weapon))
            return;

        if (weapon.WidePrimary)
            _melee.ClientHeavyAttack(entity, coordinates, weaponUid, weapon);
    }

    /// <summary>
    /// Gets the closest entity that was sprite clicked.
    /// Prioritizes entities that are not xenos.
    /// </summary>
    /// <param name="clickCoords">Mouse Click Coordinates</param>
    /// <param name="clickedEntities">All Entities that are under the clickCoords</param>
    /// <param name="newTarget"></param>
    /// <returns></returns>
    public bool TryGetAlternativeXenoAttackTarget(MapCoordinates clickCoords, List<EntityUid> clickedEntities, [NotNullWhen(true)] out EntityUid? newTarget)
    {
        newTarget = null;
        var tackleableEnts = clickedEntities;

        var compareDistance = new Comparison<EntityUid>((EntityUid a, EntityUid b) =>
        {
            var coordA = _transform.GetMapCoordinates(a);
            var coordB = _transform.GetMapCoordinates(b);

            var distanceA = (coordA.Position - clickCoords.Position).Length();
            var distanceB = (coordB.Position - clickCoords.Position).Length();

            if (distanceA > distanceB)
            {
                return 1;
            }
            else if (distanceA < distanceB)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        });

        List<EntityUid> xenoTargets = new();
        List<EntityUid> nonXenoTargets = new();

        foreach (var ent in tackleableEnts)
        {
            if (!HasComp<TackleableComponent>(ent))
            {
                continue;
            }

            if (HasComp<XenoComponent>(ent))
            {
                xenoTargets.Add(ent);
                continue;
            }

            nonXenoTargets.Add(ent);
        }

        // Prioritze nonXenoTargets for targeting
        if (nonXenoTargets.Count > 0)
        {
            nonXenoTargets.Sort(compareDistance);
            newTarget = nonXenoTargets.First();
            return true;
        }

        if (xenoTargets.Count > 0)
        {
            xenoTargets.Sort(compareDistance);
            newTarget = xenoTargets.First();
            return true;
        }
        return false;
    }
}
