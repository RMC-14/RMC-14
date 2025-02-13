using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction.FloorResin;

/// <summary>
/// Blocks resin surge from spawning sticky resin ontop of it
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StickyResinSurgeBlockerComponent : Component;
