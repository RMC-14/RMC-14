using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Stasis;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMStasisBagSystem))]
public sealed partial class CMStasisBagComponent : Component
{
    // TODO RMC14 make upstream metabolism modifiers not shit, this just makes metabolism 1000 times slower instead of stopping it
    [DataField, AutoNetworkedField]
    public int MetabolismMultiplier = 1000;

    [DataField, AutoNetworkedField]
    public TimeSpan StasisMaxTime = TimeSpan.FromMinutes(15);

    [DataField, AutoNetworkedField]
    public TimeSpan StasisLeft = TimeSpan.FromMinutes(15);

    [DataField, AutoNetworkedField]
    public EntProtoId UsedBag = "RMCStasisBagUsed";
}
