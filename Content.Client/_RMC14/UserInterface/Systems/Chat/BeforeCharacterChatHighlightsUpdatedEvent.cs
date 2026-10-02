using Content.Client.UserInterface.Systems.Chat;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client._RMC14.UserInterface.Systems.Chat;

/// <summary>
/// Event raised by <see cref="ChatUIController.OnCharacterUpdated(CharacterData)"/> to allow for the default chat highlights
/// for a character's name, role, etc. to be changed by modifying the generated <paramref name="Highlights"/> string.
/// </summary>
/// <param name="Data">Some info about the player's character including their <see cref="EntityUid"/>, name, job role and so on.</param>
/// <param name="Highlights">A series of chat highlight "rules" in a single long line, separated by '<c>\n</c>'s.</param>
[ByRefEvent]
public record struct BeforeCharacterChatHighlightsUpdatedEvent(CharacterData Data, string Highlights);
