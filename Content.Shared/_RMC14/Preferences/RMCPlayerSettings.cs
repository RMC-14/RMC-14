using Content.Shared._RMC14.Xenonids.Name;
using Content.Shared.Preferences;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Preferences;

[Serializable, NetSerializable]
public sealed record RMCPlayerSettings(
    PreferenceUnavailableMode PreferenceUnavailable,
    string XenoPrefix,
    string XenoPostfix,
    bool PlaytimePerks)
{
    public static readonly RMCPlayerSettings Default = new(
        PreferenceUnavailableMode.SpawnAsOverflow,
        string.Empty,
        string.Empty,
        true);

    public RMCPlayerSettings Validated(ICommonSession session, IDependencyCollection collection)
    {
        var preferenceUnavailable = PreferenceUnavailable switch
        {
            PreferenceUnavailableMode.StayInLobby => PreferenceUnavailableMode.StayInLobby,
            PreferenceUnavailableMode.SpawnAsOverflow => PreferenceUnavailableMode.SpawnAsOverflow,
            _ => PreferenceUnavailableMode.StayInLobby,
        };

        var xenoName = collection.Resolve<IEntityManager>().System<SharedXenoNameSystem>();
        var prefixMax = xenoName.GetMaxXenoPrefixLength(session);
        var postfixMax = xenoName.GetMaxXenoPostfixLength(session);

        var prefix = XenoPrefix.Trim();
        if (prefix.Length > prefixMax)
            prefix = prefix[..prefixMax];

        prefix = ValidateXenoName(prefix, false);

        var postfix = XenoPostfix.Trim();
        if (prefix.Length > 2)
        {
            postfix = string.Empty;
        }
        else
        {
            if (postfix.Length > postfixMax)
                postfix = postfix[..postfixMax];

            postfix = ValidateXenoName(postfix, true);
        }

        return new RMCPlayerSettings(preferenceUnavailable, prefix, postfix, PlaytimePerks);
    }

    private static string ValidateXenoName(string xenoName, bool numberEndingAllowed)
    {
        xenoName = xenoName.ToUpperInvariant();
        for (var i = 0; i < xenoName.Length; i++)
        {
            var c = xenoName[i];
            if (i > 0 && numberEndingAllowed && c >= '0' && c <= '9')
                continue;

            if (c < 'A' || c > 'Z')
                return string.Empty;
        }

        return xenoName;
    }
}
