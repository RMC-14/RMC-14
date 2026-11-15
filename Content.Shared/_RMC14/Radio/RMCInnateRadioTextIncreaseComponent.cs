using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Radio;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCInnateRadioTextIncreaseComponent : Component
{
    /// <summary>
    ///     Determines how much larger the radio message size will be.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int RadioTextIncrease { get; set; } = 0;

    [DataField, AutoNetworkedField]
    public bool Instrinsic = false;
}
