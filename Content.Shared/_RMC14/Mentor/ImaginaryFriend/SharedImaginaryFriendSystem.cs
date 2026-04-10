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
}
