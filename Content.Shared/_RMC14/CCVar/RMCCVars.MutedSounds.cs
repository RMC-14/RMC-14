using Robust.Shared.Configuration;

namespace Content.Shared._RMC14.CCVar;

public sealed partial class RMCCVars
{
    public static readonly CVarDef<string> RMCMutedSounds =
        CVarDef.Create("rmc.muted_sounds", "", CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);
}
