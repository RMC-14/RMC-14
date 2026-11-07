using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Radio;

public static class RMCIntercomConstants
{
    public static readonly ProtoId<RadioChannelPrototype> Channel = "RMCIntercom";

    public const char KeyCode = 'i';
    public const char LocalizedKeyCode = 'ш';

    public const char RadioChannelSecurePrefix = '#';

    public static bool HasPrefix(string message)
    {
        if (message.Length < 2)
            return false;

        var keyCode = char.ToLowerInvariant(message[1]);
        if (keyCode != KeyCode && keyCode != LocalizedKeyCode)
            return false;

        return message[0] is SharedChatSystem.RadioChannelPrefix
            or SharedChatSystem.RadioChannelAltPrefix
            or RadioChannelSecurePrefix;
    }
}
