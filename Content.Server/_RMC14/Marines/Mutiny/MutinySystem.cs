using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Marines.Mutiny;

public sealed class MutinySystem : SharedMutinySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddMakeMutineerVerb);
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
                        EnsureComp<MutineerComponent>(args.Target);
                    },
                    Impact = LogImpact.High,
                    Message = "Make mutineer",
                };
                args.Verbs.Add(mutineer);
            }
        }
    }

    protected override void MutineerAdded(Entity<MutineerComponent> ent, ref ComponentAdd args)
    {
        if (!TryComp<MarineComponent>(ent, out var marineComponent))
            return;

        if (TryComp<ActorComponent>(ent, out var actor))
        {
            _chatManager.DispatchServerMessage(actor.PlayerSession, Loc.GetString("mutineer-status-added"));
            _chatManager.SendAdminAnnouncement($"Player {actor.PlayerSession.Name} was made a mutineer.");
        }

        Dirty(ent);
    }

    protected override void MutineerRemoved(Entity<MutineerComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp<MarineComponent>(ent, out var marineComponent))
            return;

        if (TryComp<ActorComponent>(ent, out var actorComponent))
        {
            _chatManager.DispatchServerMessage(actorComponent.PlayerSession, Loc.GetString("mutineer-status-removed"));
            _chatManager.SendAdminAnnouncement($"Player {actorComponent.PlayerSession.Name} is no longer a mutineer.");
        }

        Dirty(ent);
    }
}
