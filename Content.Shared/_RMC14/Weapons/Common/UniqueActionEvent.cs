namespace Content.Shared._RMC14.Weapons.Common;

public sealed class UniqueActionEvent(EntityUid userUid) : HandledEntityEventArgs
{
    public readonly EntityUid UserUid = userUid;
}
