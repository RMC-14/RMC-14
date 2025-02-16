using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared._RMC14.SelectableSounds;

/// <summary>
/// A component which lets you toggle sounds on things like an EmitSoundOnUseComponent with a verb
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCSelectableSoundsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<LocId, SoundSpecifier> Sounds = new();
}
