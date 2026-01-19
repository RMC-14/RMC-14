using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Megaphone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMegaphoneSystem))]
public sealed partial class RMCMegaphoneComponent : Component
{
    /// <summary>
    /// Voice range multiplier for megaphone (normal voice range is 10).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float VoiceRange = 15f;
}
