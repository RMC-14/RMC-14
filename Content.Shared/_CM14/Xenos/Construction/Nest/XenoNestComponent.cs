using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Construction.Nest;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoNestSystem))]
public sealed partial class XenoNestComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Candidate;

    [DataField, AutoNetworkedField]
    public EntityUid Nested;
}
