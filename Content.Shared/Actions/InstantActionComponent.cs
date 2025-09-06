using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Actions;

[RegisterComponent, NetworkedComponent]
public sealed partial class InstantActionComponent : BaseActionComponent
{
    public override BaseActionEvent? BaseEvent => Event;

    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField("event")]
    [NonSerialized]
    public InstantActionEvent? Event;

    /// <summary>
    ///     Icon representing this action in the UI.
    /// </summary>
    [DataField("icon")] public SpriteSpecifier? Icon;

    /// <summary>
    ///     Time interval between action uses.
    /// </summary>
    [DataField("useDelay")] public TimeSpan? UseDelay;
}

[Serializable, NetSerializable]
public sealed class InstantActionComponentState : BaseActionComponentState
{
    public InstantActionComponentState(InstantActionComponent component, IEntityManager entManager) : base(component, entManager)
    {
    }
}
