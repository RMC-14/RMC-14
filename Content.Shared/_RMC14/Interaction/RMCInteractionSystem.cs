using Content.Shared.Interaction.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Interaction;

public sealed class RMCInteractionSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InteractedBlacklistComponent, GettingInteractedWithAttemptEvent>(OnBlacklistInteractionAttempt);
        SubscribeLocalEvent<InsertBlacklistComponent, ContainerGettingInsertedAttemptEvent>(OnInsertBlacklistContainerInsertedAttempt);
    }

    private void OnBlacklistInteractionAttempt(Entity<InteractedBlacklistComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (args.Cancelled || ent.Comp.Blacklist == null)
            return;

        if (_whitelist.IsValid(ent.Comp.Blacklist, args.Uid))
            args.Cancelled = true;
    }

    private void OnInsertBlacklistContainerInsertedAttempt(Entity<InsertBlacklistComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        if (args.Cancelled || ent.Comp.Blacklist is not { } blacklist)
            return;

        if (_whitelist.IsValid(blacklist, args.EntityUid))
            args.Cancel();
    }
}
