using System.Numerics;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Movement;

[RegisterComponent, NetworkedComponent]
public sealed partial class LinearInputMoverComponent : Component
{
    public GameTick LastInputTick;
    public ushort LastInputSubTick;

    public Vector2 CurTickWalkMovement;
    public Vector2 CurTickSprintMovement;

    public MoveButtons HeldMoveButtons = MoveButtons.None;

    /// <summary>
    /// Makes the entity rotate on the spot instead of instantly moving left or right.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool LinearMovement = false;

    /// <summary>
    /// Does our input indicate actual movement, and not just modifiers?
    /// </summary>
    /// <remarks>
    /// This can be useful to filter out input from just pressing the walk button with no directions, for example.
    /// </remarks>
    public bool HasDirectionalMovement => (HeldMoveButtons & MoveButtons.AnyDirection) != MoveButtons.None;

    /// <summary>
    /// Direction to move this tick.
    /// </summary>
    public Vector2 WishDir;

    /// <summary>
    /// Although our movement might be relative to a particular entity we may have an additional relative rotation
    /// e.g. if we've snapped to a different cardinal direction
    /// </summary>
    [ViewVariables]
    public Angle TargetRelativeRotation = Angle.Zero;

    /// <summary>
    /// The current relative rotation. This will lerp towards the <see cref="TargetRelativeRotation"/>.
    /// </summary>
    [ViewVariables]
    public Angle RelativeRotation;

    /// <summary>
    /// Entity our movement is relative to.
    /// </summary>
    public EntityUid? RelativeEntity;

    /// <summary>
    /// If we traverse on / off a grid then set a timer to update our relative inputs.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LerpTarget;

    public const float LerpTime = 1.0f;

    public bool Sprinting => (HeldMoveButtons & MoveButtons.Walk) == 0x0;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanMove = true;
}

[Serializable, NetSerializable]
public sealed class LinearInputMoverComponentState : ComponentState
{
    public MoveButtons HeldMoveButtons;
    public NetEntity? RelativeEntity;
    public Angle TargetRelativeRotation;
    public Angle RelativeRotation;
    public TimeSpan LerpTarget;
    public bool CanMove;
}
