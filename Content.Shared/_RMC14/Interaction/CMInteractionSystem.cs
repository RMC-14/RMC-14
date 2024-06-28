using Content.Shared.Interaction.Events;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Interaction;

public sealed class CMInteractionSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InteractedBlacklistComponent, GettingInteractedWithAttemptEvent>(OnBlacklistInteractionAttempt);
    }

    private void OnBlacklistInteractionAttempt(Entity<InteractedBlacklistComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (args.Cancelled || ent.Comp.Blacklist == null)
            return;

        if (_whitelist.IsValid(ent.Comp.Blacklist, args.Uid))
            args.Cancelled = true;
    }
}
