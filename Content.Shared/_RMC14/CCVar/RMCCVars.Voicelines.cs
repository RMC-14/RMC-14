using Robust.Shared.Configuration;

namespace Content.Shared._RMC14.CCVar;

public sealed partial class RMCCVars
{
    public static readonly CVarDef<bool> RMCPlayVoicelinesYourself =
        CVarDef.Create("rmc.play_voicelines_yourself", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesArachnid =
        CVarDef.Create("rmc.play_voicelines_arachnid", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesDiona =
        CVarDef.Create("rmc.play_voicelines_diona", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesDwarf =
        CVarDef.Create("rmc.play_voicelines_dwarf", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesFelinid =
        CVarDef.Create("rmc.play_voicelines_felinid", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesHuman =
        CVarDef.Create("rmc.play_voicelines_human", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesMoth =
        CVarDef.Create("rmc.play_voicelines_moth", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesReptilian =
        CVarDef.Create("rmc.play_voicelines_reptilian", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesSlime =
        CVarDef.Create("rmc.play_voicelines_slime", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesAvali =
        CVarDef.Create("rmc.play_voicelines_avali", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesVulpkanin =
        CVarDef.Create("rmc.play_voicelines_vulpkanin", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesRodentia =
        CVarDef.Create("rmc.play_voicelines_rodentia", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesFeroxi =
        CVarDef.Create("rmc.play_voicelines_feroxi", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCPlayVoicelinesSkrell =
        CVarDef.Create("rmc.play_voicelines_skrell", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);
}
