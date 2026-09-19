namespace Content.Shared.Mind.Components
{
    [RegisterComponent]
    public sealed partial class VisitingMindComponent : Component
    {
        [ViewVariables]
        public EntityUid? MindId;
    }

    public sealed class MindVisitedMessage(EntityUid mindId) : EntityEventArgs
    {
        public EntityUid MindId { get; } = mindId;
    }

    public sealed class MindUnvisitedMessage : EntityEventArgs
    {
    }
}
