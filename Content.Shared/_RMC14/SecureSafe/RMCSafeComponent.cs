using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.SecureSafe;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCSafeComponent : Component
{
    [DataField("code1")]
    public int Code1 = 1;

    [DataField("code2")]
    public int Code2 = 1;

    /// The current state of dial 1 in the UI. Networked so the UI can display it.
    [DataField("dial1"), AutoNetworkedField]
    public int Dial1 = 1;

    /// The current state of dial 2 in the UI. Networked so the UI can display it.
    [DataField("dial2"), AutoNetworkedField]
    public int Dial2 = 1;

    /// If set to a valid prototype, the safe will spawn with code on map initialization
    [DataField]
    public EntProtoId? AutoPrintPaperPrototype;

    [DataField]
    public string AutoPrintPaperName = "Scrawled Note";

    [DataField]
    public string AutoPrintPaperDesc = "A piece of paper with some numbers hastily scribbled on it.";

    /// The formatted string to print on the paper. {0} is replaced by Dial 1, and {1} is replaced by Dial 2.
    [DataField]
    public string AutoPrintFormat = "The code is: {0} - {1}";

    /// If set, the safe will attempt to give the combination paper directly to a player with this job.
    [DataField]
    public string? AutoPrintToJob;
}
