using Content.Server.Mind;
using Content.Shared._RMC14.Mentor.ImaginaryFriend;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Eye;
using Content.Shared.Ghost;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Mentor.ImaginaryFriend;

public sealed class ImaginaryFriendSystem : SharedImaginaryFriendSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly EyeSystem _eye = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;

    public static readonly EntProtoId ImaginaryFriendPrototype = "RMCImaginaryFriend";
    public static readonly EntProtoId XenoImaginaryFriendPrototype = "RMCImaginaryFriendXeno";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HasImaginaryFriendComponent, ComponentShutdown>(OnHasImaginaryFriendShutdown);
        SubscribeLocalEvent<HasImaginaryFriendComponent, GetVisMaskEvent>(OnHasImaginaryFriendVisMask);

        SubscribeLocalEvent<ImaginaryFriendComponent, ImaginaryFriendToggleVisibilityActionEvent>(OnFriendToggleVisibility);
        SubscribeLocalEvent<ImaginaryFriendComponent, ImaginaryFriendStopBeingFriendsActionEvent>(OnStopBeingFriends);
        SubscribeLocalEvent<ImaginaryFriendComponent, ComponentShutdown>(OnFriendShutdown);
    }

    private void OnHasImaginaryFriendShutdown(Entity<HasImaginaryFriendComponent> ent, ref ComponentShutdown args)
    {
        RemoveImaginaryFriend(ent.Comp);
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnHasImaginaryFriendVisMask(Entity<HasImaginaryFriendComponent> ent, ref GetVisMaskEvent args)
    {
        if (TerminatingOrDeleted(ent.Comp.Friend))
            return;

        args.VisibilityMask |= (int)VisibilityFlags.ImaginaryFriend;
    }

    private void OnFriendToggleVisibility(Entity<ImaginaryFriendComponent> ent, ref ImaginaryFriendToggleVisibilityActionEvent args)
    {
        args.Handled = true;

        ent.Comp.Visible = !ent.Comp.Visible;
        Dirty(ent);

        Actions.SetToggled(ent.Comp.ToggleVisibilityActionEntity, ent.Comp.Visible);
        _appearance.SetData(ent, ImaginaryFriendVisuals.Sprite, ent.Comp.Visible);
    }

    private void OnStopBeingFriends(Entity<ImaginaryFriendComponent> ent, ref ImaginaryFriendStopBeingFriendsActionEvent args)
    {
        args.Handled = true;

        RemoveImaginaryFriend(ent, ent.Comp);
    }

    private void OnFriendShutdown(Entity<ImaginaryFriendComponent> ent, ref ComponentShutdown args)
    {
        RemoveImaginaryFriend(ent, ent.Comp);
    }

    public void BecomeImaginaryFriend(EntityUid imaginer, EntityUid newFriend)
    {
        if (!_mind.TryGetMind(newFriend, out var mindId, out var friendMind))
            return;

        if (!HasComp<GhostComponent>(newFriend))
            return;

        if (EnsureComp<HasImaginaryFriendComponent>(imaginer, out var hasFriend))
            return;

        var targetIsXeno = HasComp<XenoComponent>(imaginer);
        var coordinates = _transform.GetMoverCoordinates(imaginer);
        var prototype = targetIsXeno ? XenoImaginaryFriendPrototype : ImaginaryFriendPrototype;

        var friend = EntityManager.SpawnEntity(prototype, coordinates);
        _transform.AttachToGridOrMap(friend, Transform(friend));

        _mind.UnVisit(mindId);
        _mind.Visit(mindId, friend, friendMind);

        hasFriend.Friend = friend;
        Dirty(imaginer, hasFriend);

        var imaginaryFriend = EnsureComp<ImaginaryFriendComponent>(friend);
        imaginaryFriend.Imaginer = imaginer;
        Dirty(friend, imaginaryFriend);

        _visibility.AddLayer(friend, (int)VisibilityFlags.ImaginaryFriend, false);
        _visibility.RemoveLayer(friend, (int)VisibilityFlags.Ghost, false);
        _visibility.RefreshVisibility(friend);

        _eye.RefreshVisibilityMask(imaginer);
    }

    private void RemoveImaginaryFriend(HasImaginaryFriendComponent hasImaginaryFriend)
    {
        if (hasImaginaryFriend.Friend is not { } friend)
            return;

        if (TerminatingOrDeleted(friend))
            return;

        QueueDel(friend);
    }

    private void RemoveImaginaryFriend(EntityUid friend, ImaginaryFriendComponent imaginaryFriend)
    {
        if (imaginaryFriend.Imaginer is not { } imaginer)
            return;

        RemComp<HasImaginaryFriendComponent>(imaginer);

        if (TerminatingOrDeleted(friend))
            return;

        QueueDel(friend);
    }
}
