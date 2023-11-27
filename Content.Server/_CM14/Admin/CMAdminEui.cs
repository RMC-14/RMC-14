using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._CM14.Admin;
using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Hive;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Shared.Player;

namespace Content.Server._CM14.Admin;

public sealed class CMAdminEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    private readonly XenoHiveSystem _hive;
    private readonly XenoSystem _xeno;

    private readonly NetEntity _target;

    public CMAdminEui(EntityUid target)
    {
        IoCManager.InjectDependencies(this);

        _hive = _entities.System<XenoHiveSystem>();
        _xeno = _entities.System<XenoSystem>();

        _target = _entities.GetNetEntity(target);
    }

    public static bool CanUse(IAdminManager admin, ICommonSession player)
    {
        return admin.HasAdminFlag(player, AdminFlags.Fun);
    }

    public override void Opened()
    {
        _admin.OnPermsChanged += OnAdminPermsChanged;
        StateDirty();
    }

    public override void Closed()
    {
        _admin.OnPermsChanged -= OnAdminPermsChanged;
    }

    public override EuiStateBase GetNewState()
    {
        var hives = new List<Hive>();
        var query = _entities.EntityQueryEnumerator<HiveComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var metaData))
        {
            hives.Add(new Hive(_entities.GetNetEntity(uid), metaData.EntityName));
        }

        return new CMAdminEuiState(_target, hives);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case CMAdminChangeHiveMessage changeHive:
            {
                if (_entities.TryGetEntity(_target, out var target) &&
                    _entities.TryGetEntity(changeHive.Hive.Id, out var hive))
                {
                    _xeno.MakeXeno(target.Value);
                    _xeno.SetHive(target.Value, hive.Value);
                }

                break;
            }
            case CMAdminCreateHiveMessage createHive:
            {
                _hive.CreateHive(createHive.Name);
                StateDirty();
                break;
            }
        }
    }

    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        if (!_admin.HasAdminFlag(Player, AdminFlags.Fun))
        {
            Close();
            return;
        }

        StateDirty();
    }
}
