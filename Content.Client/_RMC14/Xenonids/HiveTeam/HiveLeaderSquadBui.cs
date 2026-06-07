using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveLeader;
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
    private readonly SharedXenoHiveSystem _hiveSystem;
    private HiveLeaderSquadWindow? _window;

    public HiveLeaderSquadBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
        _hiveSystem = EntMan.System<SharedXenoHiveSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<HiveLeaderSquadWindow>();
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

        var netOwner = EntMan.GetNetEntity(Owner);
        HiveTeamEntry? myEntry = null;
        var myIndex = 0;
        for (var i = 0; i < teams.Teams.Count; i++)
        {
            if (teams.Teams[i].Leader == netOwner)
            {
                myEntry = teams.Teams[i];
                myIndex = i;
                break;
            }
        }

        if (myEntry == null)
            return;

        var roleName = myEntry.Role >= 0 && myEntry.Role < HiveTeamsComponent.RoleNames.Length
            ? HiveTeamsComponent.RoleNames[myEntry.Role]
            : "?";

        var allXenos = BuildAllXenos(hive.Owner);
        _window.UpdateState(myEntry, myIndex, roleName, allXenos, GetTexture, OnAnnounce, OnAddMember, OnRemoveMember);
    }

    private List<(NetEntity Entity, string Name, EntProtoId? ProtoId)> BuildAllXenos(EntityUid hiveOwner)
    {
        var result = new List<(NetEntity Entity, string Name, EntProtoId? ProtoId)>();
        var query = EntMan.AllEntityQueryEnumerator<XenoComponent, HiveMemberComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var member, out var meta))
        {
            if (member.Hive != hiveOwner)
                continue;
            result.Add((Entity: EntMan.GetNetEntity(uid), Name: meta.EntityName, ProtoId: meta.EntityPrototype?.ID));
        }
        return result;
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
