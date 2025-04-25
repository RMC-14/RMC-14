using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.MotionDetector;

[RegisterComponent, NetworkedComponent]
[Access(typeof(MotionDetectorSystem))]
public sealed partial class MotionDetectorTrackedComponent : Component
{
    [DataField]
    public TimeSpan LastMove;

    [DataField]
    public bool IsQueenEye;
}
