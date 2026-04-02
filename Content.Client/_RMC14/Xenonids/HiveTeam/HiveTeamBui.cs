using Content.Shared._RMC14.Xenonids.HiveTeam;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.HiveTeam;

[UsedImplicitly]
public sealed class HiveTeamBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;
    private HiveTeamWindow? _window;

    public HiveTeamBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();
        EnsureWindow();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not HiveTeamBuiState s)
            return;

        _window = EnsureWindow();
        _window.UpdateState(s, GetTexture, OnSetLeader, OnRemoveLeader, OnAddMember, OnRemoveMember, OnSetRole);
    }

    private HiveTeamWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        _window = this.CreateWindow<HiveTeamWindow>();
        return _window;
    }

    private Texture? GetTexture(EntProtoId? id)
    {
        if (id == null || !_prototype.TryIndex(id.Value, out var proto))
            return null;
        return _sprite.Frame0(proto);
    }

    private void OnSetLeader(int teamIndex, NetEntity xeno) =>
        SendPredictedMessage(new HiveTeamSetLeaderMsg(teamIndex, xeno));

    private void OnRemoveLeader(int teamIndex) =>
        SendPredictedMessage(new HiveTeamRemoveLeaderMsg(teamIndex));

    private void OnAddMember(int teamIndex, NetEntity xeno) =>
        SendPredictedMessage(new HiveTeamAddMemberMsg(teamIndex, xeno));

    private void OnRemoveMember(int teamIndex, NetEntity xeno) =>
        SendPredictedMessage(new HiveTeamRemoveMemberMsg(teamIndex, xeno));

    private void OnSetRole(int teamIndex, int role) =>
        SendPredictedMessage(new HiveTeamSetRoleMsg(teamIndex, role));
}
