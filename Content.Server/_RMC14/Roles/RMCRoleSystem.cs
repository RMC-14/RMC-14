using Content.Shared._RMC14.Roles;
using Content.Shared.GameTicking;

namespace Content.Server._RMC14.Roles;

public sealed class RMCRoleSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var original = EnsureComp<OriginalRoleComponent>(ev.Mob);
        original.Job = ev.JobId;
        Dirty(ev.Mob, original);
    }
}
