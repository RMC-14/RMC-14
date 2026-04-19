using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent]
[Access(typeof(VehicleSystem))]
public sealed partial class VehicleInteriorComponent : Component
{
    public EntityUid Map = EntityUid.Invalid;
    public MapId MapId = MapId.Nullspace;
    public EntityCoordinates Entry;
    public EntityUid EntryParent = EntityUid.Invalid;
    public EntityUid Grid = EntityUid.Invalid;
    public HashSet<int> EntryLocks = new();
    public HashSet<EntityUid> Passengers = new();
    public HashSet<EntityUid> Xenos = new();
}

[RegisterComponent]
[Access(typeof(VehicleSystem))]
public sealed partial class VehicleInteriorLinkComponent : Component
{
    public EntityUid Vehicle = EntityUid.Invalid;
}

[RegisterComponent]
[Access(typeof(VehicleSystem))]
public sealed partial class VehicleInteriorOccupantComponent : Component
{
    public EntityUid Vehicle = EntityUid.Invalid;
    public bool IsXeno;
}
