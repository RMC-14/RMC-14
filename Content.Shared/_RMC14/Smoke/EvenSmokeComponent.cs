using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Smoke;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCSmokeSystem))]
public sealed partial class EvenSmokeComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Spawn;

    [DataField]
    public int Range = 2;
}
