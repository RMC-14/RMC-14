using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids;

/// <summary>
/// Hides this xeno from the caste unlock announcements.
/// Use for admeme or unimplemented castes that can't be evolved to.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoSystem))]
public sealed partial class XenoHiddenComponent : Component;
