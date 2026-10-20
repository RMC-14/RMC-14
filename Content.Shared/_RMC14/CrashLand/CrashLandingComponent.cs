using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.CrashLand;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CrashLandingComponent : Component
{
    [DataField, AutoNetworkedField]
    public float RemainingTime = 1f;

    [DataField, AutoNetworkedField]
    public bool DoDamage;

    [DataField]
    public Vector2 OriginalSpriteOffset;

    [DataField, AutoNetworkedField]
    public Dictionary<string, int> OriginalLayers = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, int> OriginalMasks = new();
}
