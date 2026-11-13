using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Megaphone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMegaphoneSystem))]
public sealed partial class RMCMegaphoneComponent : Component
{
    /// <summary>
    /// Multiplier applied to the base voice range (normal voice range is 10).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float VoiceRangeMultiplier = 1.5f;
}
