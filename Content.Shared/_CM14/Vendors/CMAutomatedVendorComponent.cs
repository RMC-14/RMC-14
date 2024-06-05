using System.Numerics;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedCMAutomatedVendorSystem))]
public sealed partial class CMAutomatedVendorComponent : Component
{
    // TODO CM14 make this EntProtoId<T>? instead of string?
    [DataField, AutoNetworkedField]
    public string? PointsType;

    [DataField, AutoNetworkedField]
    public ProtoId<JobPrototype>? Job;

    [DataField, AutoNetworkedField]
    public List<CMVendorSection> Sections = new();

    [DataField, AutoNetworkedField]
    public Vector2 MinOffset = new(-0.2f, -0.2f);

    [DataField, AutoNetworkedField]
    public Vector2 MaxOffset = new (0.2f, 0.2f);
}
