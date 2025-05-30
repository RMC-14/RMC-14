using Content.Shared._RMC14.Dropship.Utility.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCFultonSystem))]
public sealed partial class RMCFultonComponent : Component;
