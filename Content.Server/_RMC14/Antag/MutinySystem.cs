using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Shared._RMC14.Antag.Components;
using Content.Shared._RMC14.Marines;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Antag;

public sealed class MutinySystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly SharedMarineSystem _marineSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddMakeMutineerVerb);
        SubscribeLocalEvent<MutineerComponent, ComponentAdd>(MutineerAdded);
        SubscribeLocalEvent<MutineerComponent, ComponentRemove>(MutineerRemoved);
    }

    private void AddMakeMutineerVerb(GetVerbsEvent<Verb> args)
    {
        // To the reader. I am sorry for the abhorrent mess that is this method.
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        // Upstream's permission for making antags.
        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        // More boilerplate....
        if (!HasComp<MindContainerComponent>(args.Target) || !TryComp<ActorComponent>(args.Target, out var targetActor))
            return;

        if (TryComp<MarineComponent>(args.Target, out var marine))
        {
            // Must be a marine.
            if (!HasComp<MutineerComponent>(args.Target))
            {
                // Must not already be a mutineer.

                Verb mutineer = new()
                {
                    Text = "Make mutineer",
                    Category = VerbCategory.Antag,
                    Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Interface/cm_job_icons.rsi"),
                        "hudmutineer"),
                    Act = () =>
                    {
                        MakeMutineer(args.Target);
                    },
                    Impact = LogImpact.High,
                    Message = "Make mutineer",
                };
                args.Verbs.Add(mutineer);
            }
        }
    }

    private void MutineerAdded(Entity<MutineerComponent> ent, ref ComponentAdd args)
    {
        if (!MakeMutineer(ent))
            RemComp<MutineerComponent>(ent);
    }

    public bool MakeMutineer(EntityUid mutineer)
    {
        if (!TryComp<MarineComponent>(mutineer, out var marineComponent))
            return false;
        if (!TryComp<ActorComponent>(mutineer, out var actorComponent))
            return false;
        EnsureComp<MutineerComponent>(mutineer, out var mutineerComp);

        mutineerComp.OldIcon = marineComponent.Icon;
        mutineerComp.IsValid = true;

        _marineSystem.SetMarineIcon(mutineer,
            new SpriteSpecifier.Rsi(
                new ResPath("/Textures/_RMC14/Interface/cm_job_icons.rsi"),
                "hudmutineer")
        );

        _chatManager.DispatchServerMessage(actorComponent.PlayerSession, "You have been made a mutineer by a Game Admin. You may now participate in the ongoing mutiny.");
        _chatManager.SendAdminAnnouncement($"Player {actorComponent.PlayerSession.Name} was made a mutineer.");

        return true;
    }

    private void MutineerRemoved(Entity<MutineerComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp<MarineComponent>(ent, out var marineComponent))
            return;

        if (ent.Comp.OldIcon == null)
        {
            _marineSystem.ClearMarineIcon(ent);
        }
        else
        {
            _marineSystem.SetMarineIcon(ent, ent.Comp.OldIcon);
        }

        if (TryComp<ActorComponent>(ent, out var actorComponent))
        {
            _chatManager.DispatchServerMessage(actorComponent.PlayerSession, "The mutiny you were a part of has ended. You are no longer permitted to participate in mutiny activity.");
            _chatManager.SendAdminAnnouncement($"Player {actorComponent.PlayerSession.Name} is no longer a mutineer.");
        }
        else
        {
            if (ent.Comp.IsValid) // Only if the component was added through MakeMutineer and passed its checks.
                _chatManager.SendAdminAnnouncement($"Entity {ent} is no longer a mutineer, but had no players attached.");
        }
    }
}
