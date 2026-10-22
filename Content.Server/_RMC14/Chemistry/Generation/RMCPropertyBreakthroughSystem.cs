using System.Text;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Intel;
using Content.Shared.Paper;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Chemistry.Generation;

/// <summary>
/// Writes revealed recipe onto groundside spawned RMCIntelResearchPaper
/// </summary>
public sealed class RMCPropertyBreakthroughSystem : EntitySystem
{
    [Dependency] private readonly RMCChemicalGeneratorSystem _generator = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly string[] LegendaryPropertyIds = ["Hypergenetic", "Boosting", "Regulating", "Optimized"];
    private static readonly ProtoId<ChemGeneratorPropertyPrototype> CipheringId = "Ciphering";

    private const float LegendaryWeight = 5f;
    private const float CipheringWeight = 3f;

    public override void Initialize()
    {
        SubscribeLocalEvent<IntelResearchPaperComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<IntelResearchPaperComponent> paper, ref MapInitEvent args)
    {
        var propertyId = _random.Prob(LegendaryWeight / (LegendaryWeight + CipheringWeight))
            ? _random.Pick(LegendaryPropertyIds)
            : CipheringId.Id;

        if (!_generator.TryGetLegendaryRecipe(propertyId, out var pieces) ||
            !_prototype.TryIndex<ChemGeneratorPropertyPrototype>(propertyId, out var property))
        {
            return;
        }

        var sb = new StringBuilder();
        RMCChemPaperFormat.AppendHeader(sb, "Official Weston-Yamada Document", "Property Breakthrough Report");

        sb.AppendLine();
        sb.AppendLine($"[bold][head=3]Breakthrough: {property.ID}[/head][/bold]");
        sb.AppendLine();
        sb.AppendLine("Our analysts have decoded the following property combination:");
        sb.AppendLine();

        foreach (var pieceId in pieces)
        {
            if (_prototype.TryIndex<ChemGeneratorPropertyPrototype>(pieceId, out var piece))
                sb.AppendLine($"- {piece.ID} ({piece.Code})");
            else
                sb.AppendLine($"- {pieceId}");
        }

        sb.AppendLine();
        RMCChemPaperFormat.AppendFooter(sb, "Synthesize a reagent with all listed properties present simultaneously to produce this breakthrough.");

        _paper.SetContent(paper.Owner, RMCChemPaperFormat.Wrap(sb));
    }
}
