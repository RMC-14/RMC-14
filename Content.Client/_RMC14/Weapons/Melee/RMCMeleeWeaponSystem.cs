using Content.Client.Gameplay;
using Content.Client.Weapons.Melee;
using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.Weapons.Melee;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;

namespace Content.Client._RMC14.Weapons.Melee;

public sealed partial class RMCMeleeWeaponSystem : SharedRMCMeleeWeaponSystem
{
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private MeleeWeaponSystem _melee = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private TransformSystem _transform = default!;

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

        if (_map.TryFindGridAt(mousePos, out var gridUid, out _))
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
            var attackerEv = new CheckMeleeTargetEvent(clickable, weapon, disarm);
            RaiseLocalEvent(attacker, ref attackerEv);

            // Attacker decided target should be skipped
            if (attackerEv.Skip)
                continue;

            var targetEv = new CheckMeleeAttackerEvent(attacker, weapon, disarm);
            RaiseLocalEvent(clickable, ref targetEv);

            // Target decided it should be skipped
            if (targetEv.Skip)
                continue;

            firstTarget ??= clickable;

            // Either attacker or target decided it should be deferred
            if (attackerEv.Defer || targetEv.Defer)
                continue;

            firstPriorityTarget ??= clickable;
            break;
        }

        return firstPriorityTarget ?? firstTarget;
    }
}
