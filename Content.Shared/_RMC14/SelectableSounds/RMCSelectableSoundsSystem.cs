using Content.Shared._RMC14.Sound;
using Content.Shared.Popups;
using Content.Shared.Sound.Components;
using Content.Shared.Verbs;
using Robust.Shared.Collections;

namespace Content.Shared._RMC14.SelectableSounds;

public sealed class RMCSelectableSoundsSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSelectableSoundsComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnGetAltVerbs(Entity<RMCSelectableSoundsComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        var verbs = new ValueList<AlternativeVerb>();

        foreach (var soundEntry in ent.Comp.Sounds)
        {
            var name = Loc.GetString(soundEntry.Key);
            var sound = soundEntry.Value;

            var newVerb = new AlternativeVerb()
            {
                Text = name,
                IconEntity = GetNetEntity(ent.Owner),
                Category = VerbCategory.SelectType,
                Act = () =>
                {
                    if (TryComp<EmitSoundOnUseComponent>(ent.Owner, out var use))
                        use.Sound = sound;

                    if (TryComp<EmitSoundOnActionComponent>(ent.Owner, out var action))
                        action.Sound = sound;

                    var msg = Loc.GetString("rmc-sound-select", ("sound", name));
                    _popup.PopupClient(msg, user, user);
                },
            };

            verbs.Add(newVerb);
        }

        args.Verbs.UnionWith(verbs);
    }
}
