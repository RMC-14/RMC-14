using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Camera;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CameraRecoilComponent : Component
{
    public Vector2 CurrentKick { get; set; }
    public float LastKickTime { get; set; }

    /// <summary>
    ///     Basically I needed a way to chain this effect for the attack lunge animation. Sorry!
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Vector2 BaseOffset { get; set; }
}
