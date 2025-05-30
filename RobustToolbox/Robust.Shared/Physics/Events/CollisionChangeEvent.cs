using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Physics.Events
{
    /// <summary>
    ///     These events are broadcast (not directed) whenever an entity's ability to collide changes.
    /// </summary>
    [ByRefEvent]
    public readonly struct CollisionChangeEvent
    {
        public readonly EntityUid BodyUid;

        public readonly PhysicsComponent Body;

        public readonly bool CanCollide;

        public CollisionChangeEvent(EntityUid bodyUid, PhysicsComponent body, bool canCollide)
        {
            BodyUid = bodyUid;
            Body = body;
            CanCollide = canCollide;
        }
    }
}
