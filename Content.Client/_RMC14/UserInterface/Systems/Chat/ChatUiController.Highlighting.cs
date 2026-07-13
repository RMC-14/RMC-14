using System.Linq;
using Content.Shared._RMC14.Xenonids.Name;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController
{
    private void RMCHighlights(CharacterData data, ref string newHighlights)
    {
        if (!EntityManager.TryGetComponent<XenoNameComponent>(data.Entity, out var xenoName))
            return;

        // Add the xeno's combined custom name and number.
        var prefix = xenoName.Prefix.Length == 0 ? "XX" : xenoName.Prefix;
        newHighlights += $"\n@{prefix}{xenoName.Postfix}";
        newHighlights += $"\n@{xenoName.Number}";

        // Remove the full xeno name since people won't tend to say "Young Drone (XX-123)" when talking to you.
        newHighlights = newHighlights.Replace($"@{data.EntityName}\n", "");

        // Remove any badly formatted highlights from the system mistaking the xeno name for a lizard name.
        if (data.EntityName.Count(c => c == '-') > 1)
        {
            var hyphenSplit = data.EntityName.Split('-');
            newHighlights = newHighlights.Replace($"@{hyphenSplit[0]}\n@{hyphenSplit[^1]}\n", "");
        }
    }
}
