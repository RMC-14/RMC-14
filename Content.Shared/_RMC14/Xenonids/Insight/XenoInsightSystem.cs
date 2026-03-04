using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Xenonids.DeployTraps;
using Content.Shared.Actions;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Xenonids.Insight;

public sealed class XenoInsightSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public int GetInsight(EntityUid uid)
    {
        if (!TryComp<XenoInsightComponent>(uid, out var insight))
            return 0;

        return insight.Insight;
    }

    public void IncrementInsight(Entity<XenoInsightComponent?> xeno, int amount)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return;

        var wasMaxed = xeno.Comp.Insight >= xeno.Comp.MaxInsight;
        xeno.Comp.Insight += amount;
        xeno.Comp.Insight = Math.Min(xeno.Comp.Insight, xeno.Comp.MaxInsight);
        Dirty(xeno);

        if (!wasMaxed && xeno.Comp.Insight >= xeno.Comp.MaxInsight)
            InsightEmpower((xeno.Owner, xeno.Comp));
    }

    public void InsightEmpower(Entity<XenoInsightComponent> xeno)
    {
        if (TryComp(xeno.Owner, out XenoDeployTrapsComponent? deployTraps))
        {
            deployTraps.Empowered = true;
            foreach (var action in _actions.GetActions(xeno.Owner))
            {
                if (_actions.GetEvent(action) is XenoDeployTrapsActionEvent)
                    _actions.SetIcon(action.AsNullable(), deployTraps.ActionIconEmpowered);
            }
        }

        if (xeno.Comp.Emote is { } emote)
            _emote.TryEmoteWithChat(xeno.Owner, emote, false, null, false, true);

        _popup.PopupClient(Loc.GetString("rmc-xeno-insight-empower"), xeno, xeno, PopupType.Medium);
    }
}

