using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

/// <summary>
/// Entity always visible on map for faction(s)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapAlwaysVisibleComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool VisibleToMarines = false;

    [DataField, AutoNetworkedField]
    public bool VisibleToXenos = false;

    [DataField, AutoNetworkedField]
    public bool VisibleAsXenoStructure = false;
}
