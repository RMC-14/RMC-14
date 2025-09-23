using Content.Shared._RMC14.Dropship.Utility.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(DropshipUtilitySystem))]
public sealed partial class DropshipEngineComponent : Component;
