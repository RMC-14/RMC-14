using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(LysisCentrifugeSystem))]
public sealed partial class LysisCentrifugeComponent : Component
{
    [DataField]
    public int DegradationMin = 20;

    [DataField]
    public int DegradationMax = 60;

    [DataField, AutoNetworkedField]
    public int MaxDegradation = 100;

    [DataField]
    public string DiscSlotId = "centrifuge_disc";

    [DataField]
    public string SeedSlotId = "centrifuge_seed";

    [DataField]
    public SoundSpecifier? ScanSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    [DataField]
    public SoundSpecifier? ExtractSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg");

    [DataField]
    public SoundSpecifier? FailSound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg");

    [AutoNetworkedField]
    public Dictionary<PlantGeneType, string> ObfuscationCodes = new();

    [AutoNetworkedField]
    public int Degradation;

    [AutoNetworkedField]
    public string? GenomeName;

    [AutoNetworkedField]
    public RMCSeedStats? SeedStats;

    [ViewVariables]
    public List<IComponent>? LoadedSnapshot;
}
