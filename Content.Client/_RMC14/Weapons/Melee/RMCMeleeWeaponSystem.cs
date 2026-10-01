using Content.Client.Gameplay;
using Content.Client.Weapons.Melee;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;

namespace Content.Client._RMC14.Weapons.Melee;

public sealed class RMCMeleeWeaponSystem : SharedRMCMeleeWeaponSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MeleeWeaponSystem _melee = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private EntityQuery<XenoComponent> _xenoQuery;
    private EntityQuery<MarineComponent> _marineQuery;
    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<DamageableComponent> _damageableQuery;

    private bool _damageYourself;

    public override void Initialize()
    {
        base.Initialize();

        _xenoQuery = GetEntityQuery<XenoComponent>();
        _marineQuery = GetEntityQuery<MarineComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();
        _damageableQuery = GetEntityQuery<DamageableComponent>();

        Subs.CVar(_config, RMCCVars.RMCDamageYourself, c => _damageYourself = c, true);

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


    public EntityUid? GetAttackTarget(EntityUid attacker, MapCoordinates mousePos)
    {
        if (_stateManager.CurrentState is not GameplayStateBase screen)
            return null;

        var deferMarines = _marineQuery.HasComp(attacker);
        var deferXenos = _xenoQuery.HasComp(attacker);
        var allowSelf = deferMarines && _damageYourself; // only marines can damage themselves

        var clickables = screen.GetClickableEntities(mousePos);

        EntityUid? firstTarget = null;
        EntityUid? firstPriorityTarget = null;

        foreach (var clickable in clickables)
        {
            // ignore self unless you can harm yourself
            if (clickable == attacker && !allowSelf)
                continue;

            // ignore non-damageable entities
            if (!_damageableQuery.HasComp(clickable))
                continue;

            firstTarget ??= clickable;

            // The target becomes a priority target UNLESS:
            // a) we defer marines and it is a marine
            // b) we defer xenos and it is a xeno
            // c) it is a door that is opening, open, or closing
            if (deferMarines && _marineQuery.HasComp(clickable)
                || deferXenos && _xenoQuery.HasComp(clickable)
                || _doorQuery.TryGetComponent(clickable, out var door) && door.State is DoorState.Opening or DoorState.Open or DoorState.Closing)
            {
                continue;
            }

            firstPriorityTarget ??= clickable;
            break;
        }

        return firstPriorityTarget ?? firstTarget;
    }
}
