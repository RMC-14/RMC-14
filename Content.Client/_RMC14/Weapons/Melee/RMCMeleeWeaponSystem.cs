using Content.Client.Gameplay;
using Content.Client.Weapons.Melee;
using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee;
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

    private EntityQuery<DamageableComponent> _damageableQuery;

    public override void Initialize()
    {
        base.Initialize();

        _damageableQuery = GetEntityQuery<DamageableComponent>();

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


    public EntityUid? GetAttackTarget(EntityUid attacker, MapCoordinates mousePos, bool disarm, MeleeWeaponComponent? weapon = null)
    {
        if (_stateManager.CurrentState is not GameplayStateBase screen)
            return null;

        var clickables = screen.GetClickableEntities(mousePos);

        EntityUid? firstTarget = null;
        EntityUid? firstPriorityTarget = null;

        foreach (var clickable in clickables)
        {
            // ignore non-damageable entities
            if (!_damageableQuery.HasComp(clickable))
                continue;

            var ev = new ReceivingMeleeAttackAttemptEvent(attacker, weapon, disarm);
            RaiseLocalEvent(clickable, ref ev);

            // ignore entities that can't receive an attack
            if (ev.Cancelled)
                continue;

            firstTarget ??= clickable;

            // prioritize non-deferred entity
            if (!ev.Deferred)
            {
                firstPriorityTarget ??= clickable;
                break;
            }
        }

        return firstPriorityTarget ?? firstTarget;
    }
}
