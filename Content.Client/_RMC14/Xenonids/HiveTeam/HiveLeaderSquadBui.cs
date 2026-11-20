using System.Linq;
using Content.Shared._RMC14.Xenonids.HiveTeam;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.HiveTeam;

[UsedImplicitly]
public sealed class HiveLeaderSquadBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;
    private HiveLeaderSquadWindow? _window;

    public HiveLeaderSquadBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<HiveLeaderSquadWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not HiveLeaderSquadBuiState s)
            return;

        if (_window == null)
            return;

        if (s.MyTeam == null)
            return;

        var roleName = myEntry.Role >= 0 && myEntry.Role < HiveTeamsComponent.RoleNames.Length
            ? Loc.GetString(HiveTeamsComponent.RoleNames[myEntry.Role])
            : "?";

        var allXenos = BuildAllXenos(hive.Owner);
        var pickerXenos = BuildPickerXenos(allXenos, teams);
        _window.UpdateState(myEntry, myIndex, roleName, allXenos, pickerXenos, GetTexture, OnAnnounce, OnAddMember, OnRemoveMember);
    }

    private List<(NetEntity Entity, string Name, EntProtoId? ProtoId)> BuildAllXenos(EntityUid hiveOwner)
    {
        var result = new List<(NetEntity Entity, string Name, EntProtoId? ProtoId)>();
        var query = EntMan.AllEntityQueryEnumerator<XenoComponent, HiveMemberComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var member, out var meta))
        {
            if (member.Hive != hiveOwner)
                continue;
            if (_mobState.IsDead(uid))
                continue;
            result.Add((Entity: EntMan.GetNetEntity(uid), Name: meta.EntityName, ProtoId: meta.EntityPrototype?.ID));
        }
        return result;
        var allXenos = s.AllXenos.Select(x => (x.Entity, x.Name, x.ProtoId)).ToList();
        var pickerXenos = BuildPickerXenos(allXenos, s.AllTeams);
        _window.UpdateState(s.MyTeam, s.TeamIndex, s.RoleName, allXenos, pickerXenos, GetTexture, OnAnnounce, OnAddMember, OnRemoveMember);
    }

    private static List<(NetEntity Entity, string Name, EntProtoId? ProtoId)> BuildPickerXenos(
        List<(NetEntity Entity, string Name, EntProtoId? ProtoId)> allXenos,
        List<HiveTeamEntryState> allTeams)
    {
        var assigned = new HashSet<NetEntity>();
        foreach (var team in allTeams)
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

    private void OnAnnounce(string message) =>
        SendPredictedMessage(new HiveLeaderSquadAnnounceMsg(message));

    private void OnAddMember(NetEntity xeno) =>
        SendPredictedMessage(new HiveLeaderAddMemberMsg(xeno));

    private void OnRemoveMember(NetEntity xeno) =>
        SendPredictedMessage(new HiveLeaderRemoveMemberMsg(xeno));
}
