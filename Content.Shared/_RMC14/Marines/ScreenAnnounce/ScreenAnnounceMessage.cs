using Content.Shared.Shuttles.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Marines.ScreenAnnounce;

[NetSerializable, Serializable]
public sealed class ScreenAnnounceMessage : EntityEventArgs
{
    public string[] AnnounceText;
    public ScreenAnnounceTarget Target;
    public NetEntity? Squad;
    public ScreenAnnounceArgs ScreenAnnounceArgs;
    public string StartingMessage = string.Empty;

    public ScreenAnnounceMessage(string[] announceText, ScreenAnnounceTarget target, ScreenAnnounceArgs screenAnnounceArgs, string startingMessage, NetEntity? squad = null)
    {
        AnnounceText = announceText;
        Target = target;
        Squad = squad;
        ScreenAnnounceArgs = screenAnnounceArgs;
        StartingMessage = startingMessage;
    }
}

public enum ScreenAnnounceTarget
{
    FirstDeploy,
    SquadOnly,
    AllMarines
}