using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._RMC14.Rules;
using Content.Server._RMC14.Xenonids.Hive;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Mind;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Vendors;
using Content.Shared._RMC14.Weapons.Ranged.Whitelist;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Admin;

public sealed class RMCAdminEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;

    private readonly RMCAdminSystem _rmcAdmin;
    private readonly SharedCMAutomatedVendorSystem _automatedVendor;
    private readonly XenoHiveSystem _hive;
    private readonly MindSystem _mind;
    private readonly SquadSystem _squad;
    private readonly SharedTransformSystem _transform;
    private readonly XenoSystem _xeno;

    private NetEntity _target;

    public RMCAdminEui(EntityUid target)
    {
        IoCManager.InjectDependencies(this);

        _rmcAdmin = _entities.System<RMCAdminSystem>();
        _automatedVendor = _entities.System<SharedCMAutomatedVendorSystem>();
        _hive = _entities.System<XenoHiveSystem>();
        _mind = _entities.System<MindSystem>();
        _squad = _entities.System<SquadSystem>();
        _transform = _entities.System<SharedTransformSystem>();
        _xeno = _entities.System<XenoSystem>();

        _target = _entities.GetNetEntity(target);
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

    public static RMCAdminEuiState CreateState(IEntityManager entities, Guid tacticalMapLines)
    {
        var squadSys = entities.System<SquadSystem>();
        var hives = new List<Hive>();
        var hiveQuery = entities.EntityQueryEnumerator<HiveComponent, MetaDataComponent>();
        while (hiveQuery.MoveNext(out var uid, out _, out var metaData))
        {
            hives.Add(new Hive(entities.GetNetEntity(uid), metaData.EntityName));
        }

        var squads = new List<Squad>();
        foreach (var squadProto in squadSys.SquadPrototypes)
        {
            var exists = false;
            var members = 0;
            if (squadSys.TryGetSquad(squadProto, out var squad))
            {
                exists = true;
                members = squadSys.GetSquadMembersAlive(squad);
            }

            squads.Add(new Squad(squadProto, exists, members));
        }

        var mobState = entities.System<MobStateSystem>();
        var xenos = new List<Xeno>();
        var xenoQuery = entities.EntityQueryEnumerator<ActorComponent, XenoComponent, MetaDataComponent>();
        while (xenoQuery.MoveNext(out var uid, out _, out _, out var metaData))
        {
            if (metaData.EntityPrototype is not { } proto)
                continue;

            if (mobState.IsDead(uid))
                continue;

            xenos.Add(new Xeno(proto));
        }

        var marines = 0;
        var marinesQuery = entities.EntityQueryEnumerator<ActorComponent, MarineComponent>();
        while (marinesQuery.MoveNext(out var uid, out _, out _))
        {
            if (mobState.IsDead(uid))
                continue;

            marines++;
        }

        var rmcAdmin = entities.System<RMCAdminSystem>();
        var history = rmcAdmin.LinesDrawn.Reverse().Select(l => (l.Id, l.Actor, l.Round)).ToList();
        var lines = rmcAdmin.LinesDrawn.FirstOrDefault(l => l.Item1 == tacticalMapLines);
        return new RMCAdminEuiState(hives, squads, xenos, marines, history, lines);
    }

    public override EuiStateBase GetNewState()
    {
        var state = CreateState(_entities, default);

        _entities.TryGetEntity(_target, out var target);
        var specialistSkills = new List<(string Name, bool Present)>();
        var comps = _reflection.FindTypesWithAttribute<SpecialistSkillComponentAttribute>().ToArray();
        foreach (var comp in comps)
        {
            if (!comp.TryGetCustomAttribute(out SpecialistSkillComponentAttribute? attribute))
            {
                DebugTools.Assert($"Attribute {nameof(SpecialistSkillComponentAttribute)} not found on component {comp}");
                continue;
            }

            var present = _entities.HasComponent(target, comp);
            specialistSkills.Add((attribute.Name, present));
        }

        var points = 0;
        var extraPoints = new Dictionary<string, int>();
        if (_entities.TryGetComponent(target, out CMVendorUserComponent? vendorUser))
        {
            points = vendorUser.Points;

            if (vendorUser.ExtraPoints is { } extra)
                extraPoints = extra.ToDictionary();
        }

        return new RMCAdminEuiTargetState(
            state.Hives,
            state.Squads,
            state.Xenos,
            state.Marines,
            state.TacticalMapHistory,
            state.TacticalMapLines,
            specialistSkills,
            points,
            extraPoints
        );
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!HasPermission())
            return;

        switch (msg)
        {
            case RMCAdminSetVendorPointsMsg setPoints:
            {
                if (!_entities.TryGetEntity(_target, out var target))
                    return;

                var comp = _entities.EnsureComponent<CMVendorUserComponent>(target.Value);
                var points = Math.Max(0, setPoints.Points);
                _automatedVendor.SetPoints((target.Value, comp), points);
                StateDirty();
                break;
            }
            case RMCAdminSetSpecialistVendorPointsMsg setPoints:
            {
                if (!_entities.TryGetEntity(_target, out var target))
                    return;

                var comp = _entities.EnsureComponent<CMVendorUserComponent>(target.Value);
                var type = SharedCMAutomatedVendorSystem.SpecialistPoints;
                var points = Math.Max(0, setPoints.Points);
                _automatedVendor.SetExtraPoints((target.Value, comp), type, points);
                StateDirty();
                break;
            }
            case RMCAdminAddSpecSkillMsg addSpecSkill:
            {
                if (!_entities.TryGetEntity(_target, out var target) ||
                    !TryGetSpecSkillComponent(addSpecSkill.Component, out var comp))
                {
                    return;
                }

                if (!_entities.HasComponent(target.Value, comp.GetType()))
                    _entities.AddComponent(target.Value, comp);

                StateDirty();
                break;
            }
            case RMCAdminRemoveSpecSkillMsg removeSpecSkill:
            {
                if (!_entities.TryGetEntity(_target, out var target) ||
                    !TryGetSpecSkillComponent(removeSpecSkill.Component, out var comp))
                {
                    return;
                }

                if (!_entities.RemoveComponent(target.Value, comp.GetType()))
                    return;

                StateDirty();
                break;
            }
            case RMCAdminCreateSquadMsg createSquad:
            {
                _squad.TryEnsureSquad(createSquad.SquadId, out _);
                StateDirty();
                break;
            }
            case RMCAdminAddToSquadMsg addToSquad:
            {
                if (!_entities.TryGetEntity(_target, out var target) ||
                    !_squad.TryEnsureSquad(addToSquad.SquadId, out var squad))
                {
                    break;
                }

                _squad.AssignSquad(target.Value, (squad, squad), null);
                StateDirty();
                break;
            }
            case RMCAdminChangeHiveMsg changeHive:
            {
                if (_entities.TryGetEntity(_target, out var target) &&
                    _entities.TryGetEntity(changeHive.Hive.Id, out var hive))
                {
                    _xeno.MakeXeno(target.Value);
                    _hive.SetHive(target.Value, hive.Value);
                }

                break;
            }
            case RMCAdminCreateHiveMsg createHive:
            {
                var hive = _hive.CreateHive(createHive.Name);
                // automatically set the xeno's hive to the one you just created
                if (_entities.TryGetEntity(_target, out var target))
                    _hive.SetHive(target.Value, hive);
                StateDirty();
                break;
            }
            case RMCAdminTransformHumanoidMsg transformHumanoid:
            {
                if (_entities.GetEntity(_target) is not { Valid: true } entity)
                    break;

                var humanoid = _rmcAdmin.RandomizeMarine(entity, transformHumanoid.SpeciesId);
                if (_mind.TryGetMind(entity, out var mindId, out var mind))
                {
                    _mind.TransferTo(mindId, humanoid, mind: mind);
                    _mind.UnVisit(mindId, mind);
                }

                _entities.DeleteEntity(entity);
                _entities.EnsureComponent<MarineComponent>(humanoid);
                _target = _entities.GetNetEntity(humanoid);
                StateDirty();
                break;
            }
            case RMCAdminTransformXenoMsg transformXeno:
            {
                if (_entities.GetEntity(_target) is not { Valid: true } entity)
                    break;

                var coordinates = _transform.GetMoverCoordinates(entity);
                var newXeno = _entities.SpawnAttachedTo(transformXeno.XenoId, coordinates);
                _hive.SetSameHive(entity, newXeno);

                if (_mind.TryGetMind(entity, out var mindId, out var mind))
                {
                    _mind.TransferTo(mindId, newXeno, mind: mind);
                    _mind.UnVisit(mindId, mind);
                }

                _entities.DeleteEntity(entity);
                _target = _entities.GetNetEntity(newXeno);
                StateDirty();
                break;
            }
        }
    }

    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        if (!HasPermission())
        {
            Close();
            return;
        }

        StateDirty();
    }

    private bool HasPermission()
    {
        return _admin.HasAdminFlag(Player, AdminFlags.Fun);
    }

    private bool TryGetSpecSkillComponent(string name, [NotNullWhen(true)] out IComponent? comp)
    {
        comp = null;
        var comps = _reflection.FindTypesWithAttribute<SpecialistSkillComponentAttribute>().ToArray();
        foreach (var type in comps)
        {
            if (!type.TryGetCustomAttribute(out SpecialistSkillComponentAttribute? attribute))
            {
                DebugTools.Assert($"Attribute {nameof(SpecialistSkillComponentAttribute)} not found on component {type}");
                continue;
            }

            if (attribute.Name != name)
                continue;

            if (!_compFactory.TryGetRegistration(type, out var registration))
                continue;

            comp = _compFactory.GetComponent(registration);
            return true;
        }

        return false;
    }
}
