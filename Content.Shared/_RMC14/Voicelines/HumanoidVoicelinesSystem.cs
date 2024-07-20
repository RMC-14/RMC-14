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

    private readonly Dictionary<ProtoId<SpeciesPrototype>, CVarDef> _cVars = new()
    {
        [ArachnidSpecies] = RMCCVars.CMPlayVoicelinesArachnid,
        [DionaSpecies] = RMCCVars.CMPlayVoicelinesDiona,
        [DwarfSpecies] = RMCCVars.CMPlayVoicelinesDwarf,
        [FelinidSpecies] = RMCCVars.CMPlayVoicelinesFelinid,
        [HumanSpecies] = RMCCVars.CMPlayVoicelinesHuman,
        [MothSpecies] = RMCCVars.CMPlayVoicelinesMoth,
        [ReptilianSpecies] = RMCCVars.CMPlayVoicelinesReptilian,
        [SlimeSpecies] = RMCCVars.CMPlayVoicelinesSlime,
    };

    private EntityQuery<HumanoidAppearanceComponent> _humanoidAppearanceQuery;

    public override void Initialize()
    {
        _humanoidAppearanceQuery = GetEntityQuery<HumanoidAppearanceComponent>();
    }

    public bool ShouldPlayVoiceline(Entity<HumanoidAppearanceComponent?> vocalizer, ICommonSession forPlayer)
    {
        if (!_humanoidAppearanceQuery.Resolve(vocalizer, ref vocalizer.Comp, false) ||
            !_cVars.TryGetValue(vocalizer.Comp.Species, out var play))
        {
            return true;
        }

        return _config.GetClientCVar<bool>(forPlayer.Channel, play.Name);
    }
}
