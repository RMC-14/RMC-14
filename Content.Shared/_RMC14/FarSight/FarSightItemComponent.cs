using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.FarSight;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(FarSightSystem))]
public sealed partial class FarSightItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionToggleFarSight";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public Vector2 Zoom = new(1.13f, 1.13f);
}
