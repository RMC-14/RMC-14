

namespace Content.Shared._CM14.Weapons;

public sealed class UniqueActionEvent : HandledEntityEventArgs
{
    public readonly EntityUid UserUid;
    
    
    public UniqueActionEvent(EntityUid userUid)
    {
        UserUid = userUid;
    }
}
