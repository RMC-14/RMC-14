using System.Linq;
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
        _window = this.CreateWindow<HiveTeamWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not HiveTeamBuiState s)
            return;

        if (_window == null)
            return;

        var allXenos = s.AllXenos.Select(x => (x.Entity, x.Name, x.ProtoId)).ToList();
        var pickerXenos = BuildPickerXenos(allXenos, s.Teams);
        _window.UpdateState(s.Teams, allXenos, pickerXenos, GetTexture, OnSetLeader, OnRemoveLeader, OnAddMember, OnRemoveMember, OnSetRole);
    }

    private static List<(NetEntity Entity, string Name, EntProtoId? ProtoId)> BuildPickerXenos(
        List<(NetEntity Entity, string Name, EntProtoId? ProtoId)> allXenos,
        List<HiveTeamEntryState> teams)
    {
        var assigned = new HashSet<NetEntity>();
        foreach (var team in teams)
        {
            if (team.Leader != null)
                assigned.Add(team.Leader.Value);
            foreach (var m in team.Members)
                assigned.Add(m);
        }
        return allXenos.Where(x => !assigned.Contains(x.Entity)).ToList();
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
