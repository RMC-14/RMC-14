using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Egg;

/// <summary>
/// Allows a xeno to stash eggs into an internal inventory and bring them back out
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoEggRetrieverComponent : Component
{
    public EntProtoId EggPrototype = "XenoEgg";

    [DataField, AutoNetworkedField]
    public int MaxEggs = 8;

    [DataField, AutoNetworkedField]
    public int CurEggs = 0;

    [DataField, AutoNetworkedField]
    public TimeSpan InsertEggsDoafter = TimeSpan.FromSeconds(0.75);
}
