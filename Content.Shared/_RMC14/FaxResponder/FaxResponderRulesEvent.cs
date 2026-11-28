using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.FaxResponder;

/// <summary>
///     Sent to a player when they spawn as a fax responder, to show them the responder rules popup.
/// </summary>
[Serializable, NetSerializable]
public sealed class FaxResponderRulesEvent : EntityEventArgs;
