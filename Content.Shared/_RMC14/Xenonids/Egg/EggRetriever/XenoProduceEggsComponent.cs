using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Egg.EggRetriever;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoGenerateEggsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = false;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaDrain = FixedPoint2.New(15);

    [DataField, AutoNetworkedField]
    public TimeSpan DrainEvery = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan EggEvery = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan? NextDrain;

    [DataField, AutoNetworkedField]
    public TimeSpan? NextEgg;
}
