using System.Linq;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveTeam;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.HiveTeam;

[UsedImplicitly]
public sealed class HiveTeamBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedXenoHiveSystem _hiveSystem;
    private readonly MobStateSystem _mobState;
    private HiveTeamWindow? _window;

    public HiveTeamBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
        _hiveSystem = EntMan.System<SharedXenoHiveSystem>();
        _mobState = EntMan.System<MobStateSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<HiveTeamWindow>();
        Refresh();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (_window == null)
            return;

        if (_hiveSystem.GetHive(Owner) is not { } hive)
            return;

        if (!EntMan.TryGetComponent(hive.Owner, out HiveTeamsComponent? teams))
            return;

        var allXenos = BuildAllXenos(hive.Owner);
        var pickerXenos = BuildPickerXenos(allXenos, teams);
        _window.UpdateState(teams, allXenos, pickerXenos, GetTexture, OnSetLeader, OnRemoveLeader, OnAddMember, OnRemoveMember, OnSetRole);
    }

    private List<(NetEntity Entity, string Name, EntProtoId? ProtoId)> BuildAllXenos(EntityUid hiveOwner)
    {
        var result = new List<(NetEntity Entity, string Name, EntProtoId? ProtoId)>();
        var query = EntMan.AllEntityQueryEnumerator<XenoComponent, HiveMemberComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var member, out var meta))
        {
            if (uid == Owner || member.Hive != hiveOwner)
                continue;
            if (_mobState.IsDead(uid))
                continue;
            result.Add((Entity: EntMan.GetNetEntity(uid), Name: meta.EntityName, ProtoId: meta.EntityPrototype?.ID));
        }
        result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return result;
    }

    private static List<(NetEntity Entity, string Name, EntProtoId? ProtoId)> BuildPickerXenos(
        List<(NetEntity Entity, string Name, EntProtoId? ProtoId)> allXenos,
        HiveTeamsComponent teams)
    {
        var assigned = new HashSet<NetEntity>();
        foreach (var team in teams.Teams)
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
