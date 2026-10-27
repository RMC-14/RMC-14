using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hive;

/// <summary>
///     Marks a xeno caste as a "builder" for the purposes of hive permissions
///     (construction, deconstruction and unnesting toggles set by the Queen).
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoHiveSystem))]
public sealed partial class XenoBuilderCasteComponent : Component;
