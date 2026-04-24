using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.Mentor.ImaginaryFriend;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Clothing;
using Content.Shared.Eye;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Mentor.ImaginaryFriend;

public sealed class ImaginaryFriendSystem : SharedImaginaryFriendSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly EyeSystem _eye = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;

    private static readonly EntProtoId ImaginaryFriendPrototype = "RMCImaginaryFriendHumanoid";
    private static readonly EntProtoId XenoImaginaryFriendPrototype = "RMCImaginaryFriendXeno";

    private static readonly ProtoId<StartingGearPrototype> XenoImaginaryFriendGear = "RMCMobXippyGear";
    private static readonly ProtoId<JobPrototype> ImaginaryFriendJobPrototype = "CMSeniorEnlistedAdvisor";

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
        var canSeeFriends = false;
        foreach (var friend in ent.Comp.Friends)
        {
            if (TerminatingOrDeleted(friend))
                continue;

            canSeeFriends = true;
        }

        if (!canSeeFriends)
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

    public void OpenImaginaryFriendConfirmWindow(ICommonSession session, EntityUid target)
    {
        _euiManager.OpenEui(new BecomeImaginaryFriendEui(this, target, session), session);
    }

    public override void BecomeImaginaryFriend(EntityUid imaginer, EntityUid newFriend, bool defaultCharacter = true)
    {
        if (TerminatingOrDeleted(imaginer))
            return;

        if (!_mind.TryGetMind(newFriend, out var mindId, out _))
            return;

        EnsureComp<HasImaginaryFriendComponent>(imaginer, out var hasFriend);

        var targetIsXeno = HasComp<XenoComponent>(imaginer);
        var coordinates = _transform.GetMoverCoordinates(newFriend);
        var prototype = targetIsXeno ? XenoImaginaryFriendPrototype : ImaginaryFriendPrototype;

        var friend = EntityManager.SpawnEntity(prototype, coordinates);
        _transform.AttachToGridOrMap(friend, Transform(friend));

        TryComp(newFriend, out ActorComponent? friendActor);
        var friendSession = friendActor?.PlayerSession;

        if (!targetIsXeno && friendSession != null)
        {
            if (!defaultCharacter)
            {
                var characters = _preferencesManager.GetPreferences(friendSession.UserId).Characters;
                foreach (var (_, profile) in characters)
                {
                    if (profile is not HumanoidCharacterProfile humanoid)
                        continue;

                    var jobs = humanoid.JobPriorities;
                    var highJob = jobs.FirstOrDefault(x => x.Value == JobPriority.High).Key;

                    if (highJob != ImaginaryFriendJobPrototype)
                        continue;

                    if (TryComp(friend, out HumanoidAppearanceComponent? humanoidAppearance))
                    {
                        humanoidAppearance.Species = humanoid.Species;
                        humanoidAppearance.Sex = humanoid.Sex;
                        humanoidAppearance.Age = humanoid.Age;
                        humanoidAppearance.Gender = humanoid.Gender;
                        Dirty(friend, humanoidAppearance);
                    }

                    _humanoid.LoadProfile(friend, humanoid);
                    _metaData.SetEntityName(friend, humanoid.Name);

                    if (_prototypeManager.TryIndex(highJob, out var jobProto))
                    {
                        var jobLoadoutId = LoadoutSystem.GetJobPrototype(jobProto.ID);

                        if (_prototypeManager.TryIndex(jobLoadoutId, out RoleLoadoutPrototype? roleProto))
                        {
                            humanoid.Loadouts.TryGetValue(jobLoadoutId, out var loadout);

                            if (loadout == null)
                            {
                                loadout = new RoleLoadout(jobLoadoutId);
                                loadout.SetDefault(humanoid, null, _prototypeManager);
                            }

                            _stationSpawning.EquipRoleLoadout(friend, loadout, roleProto);
                        }
                    }
                    break;
                }
            }
            EquipStartingGear(friend);
        }
        else
        {
            var startingGear = _prototypeManager.Index(XenoImaginaryFriendGear);
            _stationSpawning.EquipStartingGear(friend, startingGear, raiseEvent: false);
        }

        _mind.UnVisit(mindId);
        _mind.TransferTo(mindId, friend, createGhost: false);

        hasFriend.Friends.Add(friend);
        Dirty(imaginer, hasFriend);

        var imaginaryFriend = EnsureComp<ImaginaryFriendComponent>(friend);
        imaginaryFriend.Imaginer = imaginer;
        Dirty(friend, imaginaryFriend);

        _visibility.AddLayer(friend, (int)VisibilityFlags.ImaginaryFriend, false);
        _visibility.RemoveLayer(friend, (int)VisibilityFlags.Ghost, false);
        _visibility.RefreshVisibility(friend);

        _eye.RefreshVisibilityMask(imaginer);

        if (friendSession != null)
            _chat.DispatchServerMessage(friendSession, Loc.GetString("rmc-mentor-imaginary-friend-became", ("target", imaginer)));
    }

    private void RemoveImaginaryFriend(HasImaginaryFriendComponent hasImaginaryFriend)
    {
        foreach (var friend in hasImaginaryFriend.Friends)
        {
            if (TerminatingOrDeleted(friend))
                continue;

            QueueDel(friend);
        }
    }

    private void RemoveImaginaryFriend(EntityUid friend, ImaginaryFriendComponent imaginaryFriend)
    {
        if (imaginaryFriend.Imaginer is not { } imaginer)
            return;

        if (!TryComp(imaginer, out HasImaginaryFriendComponent? hasFriend))
            return;

        hasFriend.Friends.Remove(friend);

        if (hasFriend.Friends.Count == 0)
            RemComp<HasImaginaryFriendComponent>(imaginer);
        else
            Dirty(imaginer, hasFriend);

        if (TerminatingOrDeleted(friend))
            return;

        QueueDel(friend);
    }

    private void EquipStartingGear(EntityUid friend)
    {
        if (!_prototypeManager.TryIndex(ImaginaryFriendJobPrototype, out var jobProto))
            return;

        if (jobProto.StartingGear != null)
        {
            var startingGear = _prototypeManager.Index<StartingGearPrototype>(jobProto.StartingGear);
            _stationSpawning.EquipStartingGear(friend, startingGear, raiseEvent: false);
        }

        var ev = new StartingGearEquippedEvent(friend);
        RaiseLocalEvent(friend, ref ev);
    }
}
