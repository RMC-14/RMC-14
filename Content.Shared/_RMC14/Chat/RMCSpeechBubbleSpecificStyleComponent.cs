using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chat;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCSpeechBubbleSpecificStyleComponent : Component
{
    /// <remarks>
    ///     See StyleNano.cs
    /// </remarks>
    [DataField, AutoNetworkedField]
    public string SpeechStyleClass = "megaphoneSpeech";
}
