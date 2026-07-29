using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Marines.Mutiny;

public sealed partial class MutinyRuleSystem
{
    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor) ||
            !_adminManager.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun) ||
            !HasComp<MindContainerComponent>(args.Target) ||
            !IsDefaultMutinyBody(args.Target, requireAlive: false))
        {
            return;
        }

        MutinyMindComponent? mutinyMind = null;
        if (!_mind.TryGetMind(args.Target, out var mindId, out _) ||
            !TryComp(mindId, out mutinyMind) ||
            !mutinyMind.IsLeader)
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("rmc-mutiny-verb-make-leader"),
                Category = VerbCategory.Antag,
                Icon = new SpriteSpecifier.Rsi(
                    new ResPath("/Textures/_RMC14/Interface/job_icons/Misc/mutiny.rsi"),
                    "hudmutineerleader"),
                Act = () =>
                {
                    if (!TryAddLeader(args.Target, out var error) && error != null)
                        SendBodyPopup(args.User, error, PopupType.SmallCaution);
                },
                Impact = LogImpact.High,
                Message = Loc.GetString("rmc-mutiny-verb-make-leader"),
            });
        }
        else
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("rmc-mutiny-verb-remove-leader"),
                Category = VerbCategory.Antag,
                Icon = new SpriteSpecifier.Rsi(
                    new ResPath("/Textures/_RMC14/Interface/job_icons/Misc/mutiny.rsi"),
                    "hudmutineerleader"),
                Act = () =>
                {
                    if (!TryRemoveLeader(args.Target, out var error) && error != null)
                        SendBodyPopup(args.User, error, PopupType.SmallCaution);
                },
                Impact = LogImpact.High,
                Message = Loc.GetString("rmc-mutiny-verb-remove-leader"),
            });
        }

        if (!TryGetActiveMutiny(out _))
            return;

        if (mutinyMind == null ||
            !mutinyMind.AcceptedRecruit && mutinyMind.Side != MutinySide.Mutineer)
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("rmc-mutiny-verb-make-mutineer"),
                Category = VerbCategory.Antag,
                Icon = new SpriteSpecifier.Rsi(
                    new ResPath("/Textures/_RMC14/Interface/job_icons/Misc/mutiny.rsi"),
                    "hudmutineer"),
                Act = () =>
                {
                    if (!TryMakeMutineer(args.Target, out var error) && error != null)
                        SendBodyPopup(args.User, error, PopupType.SmallCaution);
                },
                Impact = LogImpact.High,
                Message = Loc.GetString("rmc-mutiny-verb-make-mutineer"),
            });
        }
        else if (mutinyMind.AcceptedRecruit || mutinyMind.Side == MutinySide.Mutineer)
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("rmc-mutiny-verb-remove-mutineer"),
                Category = VerbCategory.Antag,
                Icon = new SpriteSpecifier.Rsi(
                    new ResPath("/Textures/_RMC14/Interface/job_icons/Misc/mutiny.rsi"),
                    "hudmutineer"),
                Act = () =>
                {
                    if (!TryRemoveMutineer(args.Target, out var error) && error != null)
                        SendBodyPopup(args.User, error, PopupType.SmallCaution);
                },
                Impact = LogImpact.High,
                Message = Loc.GetString("rmc-mutiny-verb-remove-mutineer"),
            });
        }
    }
}
