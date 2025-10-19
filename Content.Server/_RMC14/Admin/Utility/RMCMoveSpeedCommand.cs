using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Admin.Utility;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
internal sealed class MoveSpeedCommand : ToolshedCommand
{
    private MovementSpeedModifierSystem? _moveSpeed;

    [CommandImplementation]
    public void MoveSpeed([PipedArgument] IEnumerable<EntityUid> input, float sprintSpeed, float? walkSpeed = null)
    {
        _moveSpeed ??= Sys<MovementSpeedModifierSystem>();

        foreach (var entity in input)
        {
            if (!EntityManager.TryGetComponent<MovementSpeedModifierComponent>(entity, out var moveSpeedModifier))
                throw new ArgumentException("MovementSpeedModifier component not found for entity!");
            _moveSpeed.ChangeBaseSpeed(entity,
                walkSpeed ?? moveSpeedModifier.BaseWalkSpeed,
                sprintSpeed,
                moveSpeedModifier.Acceleration);
        }
    }
}
