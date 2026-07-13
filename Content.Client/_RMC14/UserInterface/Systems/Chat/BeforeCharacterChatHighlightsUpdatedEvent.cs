using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client._RMC14.UserInterface.Systems.Chat;

[ByRefEvent]
public sealed class BeforeCharacterChatHighlightsUpdatedEvent(CharacterData data, string highlights) : EntityEventArgs
{
    public readonly CharacterData Data = data;

    public string Highlights = highlights;
}
