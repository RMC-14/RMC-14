using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._RMC14.Marines;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Marines.Icons;

public sealed class JobIconPickerSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly EuiManager _eui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MarineComponent, GetVerbsEvent<Verb>>(AddPickerVerb);
    }

    private void AddPickerVerb(Entity<MarineComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.VarEdit))
            return;

        var target = GetNetEntity(ent.Owner);
        args.Verbs.Add(new Verb
        {
            Text = "Set Job Icon",
            Category = VerbCategory.Debug,
            Act = () => _eui.OpenEui(new JobIconPickerEui(target), player),
            Impact = LogImpact.Low,
        });
    }
}
