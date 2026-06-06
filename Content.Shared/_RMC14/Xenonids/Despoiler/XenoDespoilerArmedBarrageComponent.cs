using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

/// <summary>
///     Marker added when the Despoiler selects Acid Barrage. While present (replicated to the local client),
///     the client's Use-handler treats LMB-down as a charge-start request and LMB-up as a fire request.
///     Removed by the server after fire or cancel.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class XenoDespoilerArmedBarrageComponent : Component;
