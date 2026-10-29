using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.Dodge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoActiveDodgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 SpeedMult = FixedPoint2.New(0.25);

    [DataField, AutoNetworkedField]
    public int SwiftStepsMod = -3;

    [DataField]
    public FixedPoint2 CrowdSpeedAddMult = FixedPoint2.New(0.25);

    [DataField, AutoNetworkedField]
    public float CrowdRange = 1.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt;

    [DataField, AutoNetworkedField]
    public bool InCrowd = false;

    [DataField, AutoNetworkedField]
    public bool CheckCrowd = false;

    [DataField, AutoNetworkedField]
    public TimeSpan AfterImageDuration = TimeSpan.FromSeconds(0.3);

    [DataField]
    public TimeSpan TimeBetweenOffsets = TimeSpan.FromSeconds(0.1);

    [DataField]
    public TimeSpan NextOffsetChange;

    [DataField]
    public (Vector2 WorldPosition, Angle WorldAngle)? LastPosition;

    [DataField]
    public List<RMCAfterImage> AfterImages = new();

    [DataField]
    public float AfterImageOpacityMult = 0.75f;
}

public struct RMCAfterImage
{
    public readonly Vector2 WorldPosition;
    public readonly Angle WorldAngle;
    public readonly TimeSpan DisappearTime;
    public readonly Vector2 Offset;

    public RMCAfterImage(Vector2 position, Angle angle, TimeSpan end, Vector2 offset)
    {
        WorldPosition = position;
        WorldAngle = angle;
        DisappearTime = end;
        Offset = offset;
    }
}
