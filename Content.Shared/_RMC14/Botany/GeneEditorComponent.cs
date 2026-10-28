using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(GeneEditorSystem))]
public sealed partial class GeneEditorComponent : Component
{
    [DataField]
    public string DiscSlotId = "gene_editor_disc";

    [DataField]
    public string SeedSlotId = "gene_editor_seed";

    [DataField]
    public int EditCountAddMin = 5;

    [DataField]
    public int EditCountAddMax = 10;

    [DataField, AutoNetworkedField]
    public int MaxEditCount = 100;

    [DataField]
    public SoundSpecifier? ApplySound = new SoundPathSpecifier("/Audio/Machines/genetics.ogg");

    [DataField]
    public SoundSpecifier? FailSound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg");

    [DataField]
    public SoundSpecifier? EjectSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [AutoNetworkedField]
    public string? DiscSource;

    [AutoNetworkedField]
    public List<string> DiscGeneLabels = new();

    [AutoNetworkedField]
    public int SeedEditCount;

    [AutoNetworkedField]
    public RMCSeedStats? SeedStats;
}
