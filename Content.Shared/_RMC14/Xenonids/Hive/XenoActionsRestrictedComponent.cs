using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hive;

/// <summary>
///     Marks a xeno caste as fully locked out of hive-permission-gated actions
///     (harming, construction, deconstruction, unnesting), regardless of what
///     the hive's permissions are currently set to. Not attached to any caste
///     by default - intended to be attached downstream to castes that should
///     never be trusted with these actions (e.g. a griefable low-trust caste).
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoHiveSystem))]
public sealed partial class XenoActionsRestrictedComponent : Component;
