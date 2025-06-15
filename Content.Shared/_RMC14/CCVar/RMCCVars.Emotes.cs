using Robust.Shared.Configuration;

namespace Content.Shared._RMC14.CCVar;

public sealed partial class RMCCVars
{
    public static readonly CVarDef<bool> RMCPlayEmotesYourself =
        CVarDef.Create("rmc.play_emotes_yourself", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesArachnid =
        CVarDef.Create("rmc.play_emotes_arachnid", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesDiona =
        CVarDef.Create("rmc.play_emotes_diona", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesDwarf =
        CVarDef.Create("rmc.play_emotes_dwarf", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesFelinid =
        CVarDef.Create("rmc.play_emotes_felinid", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesHuman =
        CVarDef.Create("rmc.play_emotes_human", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesMoth =
        CVarDef.Create("rmc.play_emotes_moth", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesReptilian =
        CVarDef.Create("rmc.play_emotes_reptilian", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesSlime =
        CVarDef.Create("rmc.play_emotes_slime", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesAvali =
        CVarDef.Create("rmc.play_emotes_avali", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesVulpkanin =
        CVarDef.Create("rmc.play_emotes_vulpkanin", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesRodentia =
        CVarDef.Create("rmc.play_emotes_rodentia", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesFeroxi =
        CVarDef.Create("rmc.play_emotes_feroxi", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayEmotesSkrell =
        CVarDef.Create("rmc.play_emotes_skrell", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);
}
