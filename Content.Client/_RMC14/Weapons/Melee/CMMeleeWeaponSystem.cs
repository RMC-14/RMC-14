using Content.Client.Gameplay;
using Content.Client.Weapons.Melee;
using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Weapons.Melee;

public sealed class CMMeleeWeaponSystem : SharedCMMeleeWeaponSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MeleeWeaponSystem _melee = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatModeSystem = default!;

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
            .Register<CMMeleeWeaponSystem>();
    }

    private void TryPrimaryHeavyAttack()
    {
        var mousePos = _eye.PixelToMap(_inputManager.MouseScreenPosition);
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
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } playerEntity)
            return;
        
        if (!TryComp(playerEntity, out HandsComponent? handsComponent) || handsComponent.ActiveHandEntity == null)
            return;
        
        EntityUid weaponUid = handsComponent.ActiveHandEntity.Value;
        
        if (!TryComp(weaponUid, out MeleeWeaponComponent? meleeComponent) || !TryComp(weaponUid, out AltFireMeleeComponent? altMeleeComponent))
            return;
        
        if (!_combatModeSystem.IsInCombatMode(playerEntity) || !_actionBlockerSystem.CanAttack(playerEntity, weapon: (weaponUid, meleeComponent)))
        {
            meleeComponent.Attacking = false;
            return;
        }
        
        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);

        if ((meleeComponent.AutoAttack || (_inputSystem.CmdStates.GetState(EngineKeyFunctions.Use) != BoundKeyState.Down && altDown != BoundKeyState.Down)) &&
            meleeComponent.Attacking)
        {
            RaisePredictiveEvent(new StopAttackEvent(GetNetEntity(weaponUid)));
        }
        
        if (altDown != BoundKeyState.Down)
            return;

        if (meleeComponent.Attacking || meleeComponent.NextAttack > _timing.CurTime)
            return;

        var mousePos = _eye.PixelToMap(_inputManager.MouseScreenPosition);

        if (mousePos.MapId == MapId.Nullspace)
            return;

        EntityCoordinates coordinates;

        if (_mapManager.TryFindGridAt(mousePos, out var gridUid, out _))
            coordinates = EntityCoordinates.FromMap(gridUid, mousePos, _transform, EntityManager);
        else
            coordinates = EntityCoordinates.FromMap(_mapManager.GetMapEntityId(mousePos.MapId), mousePos, _transform, EntityManager);
        
        EntityUid? target = _stateManager.CurrentState is GameplayStateBase screen ? screen.GetClickedEntity(mousePos) : null;
        
        var attackerPos = _transform.GetMapCoordinates(playerEntity);
        
        if(mousePos.MapId != attackerPos.MapId)
            return;
        
        switch (altMeleeComponent.AttackType)
        {
            case AltFireAttackType.Light:
                if ((attackerPos.Position - mousePos.Position).Length() > meleeComponent.Range)
                    return;
                
                RaisePredictiveEvent(new LightAttackEvent(GetNetEntity(target), GetNetEntity(weaponUid), GetNetCoordinates(coordinates)));
                break;
            
            case AltFireAttackType.Heavy:
                _melee.ClientHeavyAttack(playerEntity, coordinates, weaponUid, meleeComponent);
                break;
            
            case AltFireAttackType.Disarm:
                if ((attackerPos.Position - mousePos.Position).Length() > meleeComponent.Range)
                    return;
                
                RaisePredictiveEvent(new DisarmAttackEvent(GetNetEntity(target), GetNetCoordinates(coordinates)));
                break;
        }
    }
}
