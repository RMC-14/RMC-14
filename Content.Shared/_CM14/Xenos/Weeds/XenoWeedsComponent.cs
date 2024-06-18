using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenos.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class XenoWeedsComponent : Component
{
    [DataField]
    public int Range = 5;

    [DataField]
    public float SpeedMultiplierXeno = 1.05f;

    [DataField]
    public float SpeedMultiplierOutsider = 0.6f; // TODO CM14

    [DataField, AutoNetworkedField]
    public bool IsSource = true;

    [DataField, AutoNetworkedField]
    public EntityUid? Source;

    [DataField]
    public EntProtoId Spawns = "XenoWeeds";

    [DataField, AutoNetworkedField]
    public List<EntityUid> Spread = new();

    [DataField, AutoNetworkedField]
    public TimeSpan MinRandomDelete = TimeSpan.FromSeconds(9);

    [DataField, AutoNetworkedField]
    public TimeSpan MaxRandomDelete = TimeSpan.FromSeconds(10);
}
