using Content.Shared._RMC14.Xenonids.Psychic;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed class EncephalophrasiveSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly EntProtoId WhisperAction = "ActionXenoPsychicWhisper";

    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(3);

    public void Refresh(EntityUid uid)
    {
        if (_net.IsClient)
            return;

        var comp = EnsureComp<EncephalophrasiveComponent>(uid);
        if (comp.Action == null)
        {
            comp.GrantedCommunicationComponent = !HasComp<XenoPsychicCommunicationComponent>(uid);
            EnsureComp<XenoPsychicCommunicationComponent>(uid);
            comp.Action = _actions.AddAction(uid, WhisperAction);

            _popup.PopupEntity(Loc.GetString("chem-encephalophrasive-headache"), uid, uid);
        }

        comp.ExpiresAt = _timing.CurTime + GracePeriod;
        Dirty(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<EncephalophrasiveComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time < comp.ExpiresAt)
                continue;

            if (comp.Action is { } action)
                _actions.RemoveAction(action);

            if (comp.GrantedCommunicationComponent)
                RemCompDeferred<XenoPsychicCommunicationComponent>(uid);

            RemCompDeferred<EncephalophrasiveComponent>(uid);

            _popup.PopupEntity(Loc.GetString("chem-encephalophrasive-alone"), uid, uid);
        }
    }
}
