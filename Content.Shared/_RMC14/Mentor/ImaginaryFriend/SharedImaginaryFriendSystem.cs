using Content.Shared.Actions;

namespace Content.Shared._RMC14.Mentor.ImaginaryFriend;

public abstract class SharedImaginaryFriendSystem : EntitySystem
{
    [Dependency] protected readonly SharedActionsSystem Actions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ImaginaryFriendComponent, MapInitEvent>(OnFriendMapInit);
    }

    private void OnFriendMapInit(Entity<ImaginaryFriendComponent> ent, ref MapInitEvent args)
    {
        Actions.AddAction(ent, ref ent.Comp.ToggleVisibilityActionEntity, ent.Comp.ToggleVisibility);
        Actions.AddAction(ent, ref ent.Comp.StopBeingFriendsActionEntity, ent.Comp.StopBeingFriends);
    }

    public bool TryTransferFriends(EntityUid oldImaginer, EntityUid newImaginer, HasImaginaryFriendComponent? hasFriend = null)
    {
        if (!Resolve(oldImaginer, ref hasFriend, false))
            return false;

        foreach (var friend in hasFriend.Friends)
        {
            BecomeImaginaryFriend(newImaginer, friend);
        }

        return true;
    }

    public virtual void BecomeImaginaryFriend(EntityUid imaginer, EntityUid newFriend, bool defaultCharacter = true)
    {

    }
}
