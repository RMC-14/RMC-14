using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TacMapLiveUpdateSystem))]
public sealed partial class TacMapLiveUpdateComponent : Component;
