using Content.Shared._RMC14.GameStates;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Mind;

public sealed class RMCMindSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCPvsSystem _rmcPvs = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MindContainerComponent, PlayerAttachedEvent>(OnMindContainerPlayedAttached);
    }

    private void OnMindContainerPlayedAttached(Entity<MindContainerComponent> ent, ref PlayerAttachedEvent args)
    {
        if (!TryComp(ent.Comp.Mind, out MindComponent? mind))
            return;

        foreach (var role in mind.MindRoles)
        {
            _rmcPvs.AddSessionOverride(role, args.Player);
        }
    }
}
