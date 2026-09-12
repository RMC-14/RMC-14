using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.NPC.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Hive;

[UsedImplicitly]
public sealed class HiveAllianceBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SharedXenoHiveSystem _hiveSystem;
    private HiveAllianceWindow? _window;

    public HiveAllianceBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _hiveSystem = EntMan.System<SharedXenoHiveSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<HiveAllianceWindow>();
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

        var factions = new List<(ProtoId<NpcFactionPrototype> Id, string Name, bool Allied)>();
        foreach (var faction in HiveAlliableFactions.All)
        {
            var name = _prototype.TryIndex(faction, out var proto) ? proto.ID : faction.Id;
            factions.Add((faction, name, _hiveSystem.IsFactionAllied(hive, faction)));
        }

        var hives = new List<(NetEntity Entity, string Name, bool Allied)>();
        var query = EntMan.AllEntityQueryEnumerator<HiveComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var otherHive, out var meta))
        {
            if (uid == hive.Owner)
                continue;

            hives.Add((EntMan.GetNetEntity(uid), meta.EntityName, _hiveSystem.IsHiveAllied(hive, uid)));
        }

        _window.UpdateState(factions, hives, OnSetFactionAlly, OnSetHiveAlly);
    }

    private void OnSetFactionAlly(ProtoId<NpcFactionPrototype> faction, bool allied) =>
        SendPredictedMessage(new HiveAllianceSetFactionMsg(faction, allied));

    private void OnSetHiveAlly(NetEntity hive, bool allied) =>
        SendPredictedMessage(new HiveAllianceSetHiveMsg(hive, allied));
}
