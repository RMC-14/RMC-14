using Content.Shared._RMC14.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Voicelines;

public sealed class HumanoidVoicelinesSystem : EntitySystem
{
    [Dependency] private readonly INetConfigurationManager _config = default!;

    private static readonly ProtoId<SpeciesPrototype> ArachnidSpecies = "Arachnid";
    private static readonly ProtoId<SpeciesPrototype> DionaSpecies = "Diona";
    private static readonly ProtoId<SpeciesPrototype> DwarfSpecies = "Dwarf";
    private static readonly ProtoId<SpeciesPrototype> FelinidSpecies = "Felinid";
    private static readonly ProtoId<SpeciesPrototype> HumanSpecies = "Human";
    private static readonly ProtoId<SpeciesPrototype> MothSpecies = "Moth";
    private static readonly ProtoId<SpeciesPrototype> ReptilianSpecies = "Reptilian";
    private static readonly ProtoId<SpeciesPrototype> SlimeSpecies = "SlimePerson";
    private static readonly ProtoId<SpeciesPrototype> AvaliSpecies = "Avali";
    private static readonly ProtoId<SpeciesPrototype> VulpkaninSpecies = "Vulpkanin";
    private static readonly ProtoId<SpeciesPrototype> RodentiaSpecies = "Rodentia";
    private static readonly ProtoId<SpeciesPrototype> FeroxiSpecies = "Feroxi";
    private static readonly ProtoId<SpeciesPrototype> SkrellSpecies = "Skrell";

    private readonly Dictionary<ProtoId<SpeciesPrototype>, CVarDef> _voicelineCVars = new()
    {
        [ArachnidSpecies] = RMCCVars.RMCPlayVoicelinesArachnid,
        [DionaSpecies] = RMCCVars.RMCPlayVoicelinesDiona,
        [DwarfSpecies] = RMCCVars.RMCPlayVoicelinesDwarf,
        [FelinidSpecies] = RMCCVars.RMCPlayVoicelinesFelinid,
        [HumanSpecies] = RMCCVars.RMCPlayVoicelinesHuman,
        [MothSpecies] = RMCCVars.RMCPlayVoicelinesMoth,
        [ReptilianSpecies] = RMCCVars.RMCPlayVoicelinesReptilian,
        [SlimeSpecies] = RMCCVars.RMCPlayVoicelinesSlime,
        [AvaliSpecies] = RMCCVars.RMCPlayVoicelinesAvali,
        [VulpkaninSpecies] = RMCCVars.RMCPlayVoicelinesVulpkanin,
        [RodentiaSpecies] = RMCCVars.RMCPlayVoicelinesRodentia,
        [FeroxiSpecies] = RMCCVars.RMCPlayVoicelinesFeroxi,
        [SkrellSpecies] = RMCCVars.RMCPlayVoicelinesSkrell,
    };

    private readonly Dictionary<ProtoId<SpeciesPrototype>, CVarDef> _emoteCVars = new()
    {
        [ArachnidSpecies] = RMCCVars.RMCPlayEmotesArachnid,
        [DionaSpecies] = RMCCVars.RMCPlayEmotesDiona,
        [DwarfSpecies] = RMCCVars.RMCPlayEmotesDwarf,
        [FelinidSpecies] = RMCCVars.RMCPlayEmotesFelinid,
        [HumanSpecies] = RMCCVars.RMCPlayEmotesHuman,
        [MothSpecies] = RMCCVars.RMCPlayEmotesMoth,
        [ReptilianSpecies] = RMCCVars.RMCPlayEmotesReptilian,
        [SlimeSpecies] = RMCCVars.RMCPlayEmotesSlime,
        [AvaliSpecies] = RMCCVars.RMCPlayEmotesAvali,
        [VulpkaninSpecies] = RMCCVars.RMCPlayEmotesVulpkanin,
        [RodentiaSpecies] = RMCCVars.RMCPlayEmotesRodentia,
        [FeroxiSpecies] = RMCCVars.RMCPlayEmotesFeroxi,
        [SkrellSpecies] = RMCCVars.RMCPlayEmotesSkrell,
    };

    private EntityQuery<HumanoidAppearanceComponent> _humanoidAppearanceQuery;

    public override void Initialize()
    {
        _humanoidAppearanceQuery = GetEntityQuery<HumanoidAppearanceComponent>();
    }

    public bool ShouldPlayVoiceline(Entity<HumanoidAppearanceComponent?> vocalizer, ICommonSession forPlayer)
    {
        if (forPlayer.AttachedEntity == vocalizer &&
            !_config.GetClientCVar(forPlayer.Channel, RMCCVars.RMCPlayVoicelinesYourself))
        {
            return false;
        }

        if (!_humanoidAppearanceQuery.Resolve(vocalizer, ref vocalizer.Comp, false) ||
            !_voicelineCVars.TryGetValue(vocalizer.Comp.Species, out var play))
        {
            return true;
        }

        return _config.GetClientCVar<bool>(forPlayer.Channel, play.Name);
    }

    public bool ShouldPlayEmote(Entity<HumanoidAppearanceComponent?> vocalizer, ICommonSession forPlayer)
    {
        if (forPlayer.AttachedEntity == vocalizer &&
            !_config.GetClientCVar(forPlayer.Channel, RMCCVars.RMCPlayEmotesYourself))
        {
            return false;
        }

        if (!_humanoidAppearanceQuery.Resolve(vocalizer, ref vocalizer.Comp, false) ||
            !_emoteCVars.TryGetValue(vocalizer.Comp.Species, out var play))
        {
            return true;
        }

        return _config.GetClientCVar<bool>(forPlayer.Channel, play.Name);
    }
}
