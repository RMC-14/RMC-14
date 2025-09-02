using System.IO;
using System.Linq;
using Content.Client.Actions;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Client.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.Actions
{
    public sealed class ActionHotbarSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceManager _resources = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
        [Dependency] private readonly ActionsSystem _actionsSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public event Action<List<ActionsSystem.SlotAssignment>>? PositionsChanged;
        public event Action? ClearAssignments;
        public event Action<List<ActionsSystem.SlotAssignment>>? AssignSlot;

        private readonly List<ActionsSystem.SlotAssignment> _currentAssignments = new();
        private readonly HashSet<string> _savingInProgress = new();
        private readonly object _saveLock = new();

        private string? _currentJobId = null;
        private bool _autoSaveEnabled = true;
        private bool _isAttaching = false;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MindComponent, RoleAddedEvent>(OnMindRoleAdded);
            SubscribeLocalEvent<MindComponent, RoleRemovedEvent>(OnMindRoleRemoved);
        }

        private void OnMindRoleAdded(EntityUid mindId, MindComponent component, RoleAddedEvent args)
        {
            if (!IsLocalPlayerMind(mindId, component))
                return;

            var newJobId = GetPlayerJobIdFromMind(mindId, component);
            if (newJobId != null && newJobId != _currentJobId)
            {
                _currentJobId = newJobId;
                Timer.Spawn(TimeSpan.FromMilliseconds(500), () => TryAutoLoadHotbar(newJobId));
            }
        }

        private void OnMindRoleRemoved(EntityUid mindId, MindComponent component, RoleRemovedEvent args)
        {
            if (!IsLocalPlayerMind(mindId, component))
                return;

            var newJobId = GetPlayerJobIdFromMind(mindId, component);
            if (newJobId != _currentJobId)
            {
                _currentJobId = newJobId;
                if (newJobId != null)
                    Timer.Spawn(TimeSpan.FromMilliseconds(500), () => TryAutoLoadHotbar(newJobId));
            }
        }



        public void OnPlayerAttached()
        {
            _isAttaching = true;

            var newJobId = GetPlayerJobId();
            if (newJobId != _currentJobId)
            {
                _currentAssignments.Clear();
                _currentJobId = newJobId;
            }

            Timer.Spawn(TimeSpan.FromMilliseconds(500), () =>
            {
                var jobId = GetPlayerJobId();
                if (jobId != null)
                    TryAutoLoadHotbar(jobId);
                _isAttaching = false;
            });
        }

        public void OnPlayerDetached()
        {
            _isAttaching = false;
            _currentAssignments.Clear();
        }

        private bool IsLocalPlayerMind(EntityUid mindId, MindComponent component)
        {
            return _playerManager.LocalEntity != null && component.OwnedEntity == _playerManager.LocalEntity;
        }

        public string? GetPlayerJobId()
        {
            if (_playerManager.LocalEntity is not { } user)
                return null;

            if (_mindSystem.TryGetMind(user, out var mindId, out var mind))
            {
                var jobId = GetPlayerJobIdFromMind(mindId, mind);
                if (jobId != null)
                    return jobId;
            }

            if (TryComp<MetaDataComponent>(user, out var metaData) && metaData.EntityPrototype?.ID != null)
                return metaData.EntityPrototype.ID;

            return null;
        }

        private string? GetPlayerJobIdFromMind(EntityUid mindId, MindComponent mind)
        {
            if (_roleSystem.MindHasRole<JobRoleComponent>((mindId, mind), out var jobRole))
                return jobRole.Value.Comp1.JobPrototype;
            return null;
        }

        public void UpdateCurrentAssignments(List<ActionsSystem.SlotAssignment> assignments)
        {
            _currentAssignments.Clear();
            _currentAssignments.AddRange(assignments);
            PositionsChanged?.Invoke(assignments);

            if (_isAttaching)
                return;

            var currentJobId = GetPlayerJobId();
            if (_autoSaveEnabled && currentJobId != null)
                TryAutoSaveHotbar(currentJobId);
        }

        private void TryAutoSaveHotbar(string jobId)
        {
            lock (_saveLock)
            {
                if (_savingInProgress.Contains(jobId))
                    return;
                _savingInProgress.Add(jobId);
            }

            Timer.Spawn(TimeSpan.FromMilliseconds(50), () =>
            {
                try
                {
                    if (_currentAssignments.Count == 0)
                        return;

                    if (_playerManager.LocalEntity is { } user)
                    {
                        var currentActionIds = _actionsSystem.GetActions(user).Select(a => a.Owner).ToHashSet();
                        var validAssignments = _currentAssignments.Where(a => currentActionIds.Contains(a.ActionId)).ToList();

                        if (validAssignments.Count == 0)
                            return;

                        if (validAssignments.Count != _currentAssignments.Count)
                        {
                            _currentAssignments.Clear();
                            _currentAssignments.AddRange(validAssignments);
                        }
                    }

                    SaveActionAssignments($"hotbars/auto_{jobId}.yml");
                }
                catch (Exception ex)
                {
                    Logger.Error($"ActionHotbarSystem: Failed to auto-save hotbar for {jobId}: {ex}");
                }
                finally
                {
                    lock (_saveLock)
                    {
                        _savingInProgress.Remove(jobId);
                    }
                }
            });
        }

        private bool TryAutoLoadHotbar(string jobId)
        {
            try
            {
                var autoSavePath = $"hotbars/auto_{jobId}.yml";
                var file = new ResPath(autoSavePath).ToRootedPath();

                if (!_resources.UserData.Exists(file))
                    return false;

                _autoSaveEnabled = false;
                LoadActionPositions(autoSavePath, true);
                Timer.Spawn(TimeSpan.FromMilliseconds(100), () => _autoSaveEnabled = true);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"ActionHotbarSystem: Failed to auto-load hotbar for {jobId}: {ex}");
                _autoSaveEnabled = true;
                return false;
            }
        }

        public void SetAutoSaveEnabled(bool enabled)
        {
            _autoSaveEnabled = enabled;
        }

        public void ManualSaveHotbar()
        {
            var jobId = GetPlayerJobId();
            if (jobId != null)
                SaveActionAssignments($"hotbars/manual_{jobId}.yml");
        }

        public void ManualLoadHotbar()
        {
            var jobId = GetPlayerJobId();
            if (jobId != null)
                LoadActionPositions($"hotbars/manual_{jobId}.yml", true);
        }

        public void TriggerAutoSave()
        {
            var currentJobId = GetPlayerJobId();
            if (currentJobId != null && _autoSaveEnabled)
                TryAutoSaveHotbar(currentJobId);
        }

        public List<ActionsSystem.SlotAssignment> GetCurrentAssignments()
        {
            return _currentAssignments.ToList();
        }

        public void SetAssignments(List<ActionsSystem.SlotAssignment> actions)
        {
            _currentAssignments.Clear();
            _currentAssignments.AddRange(actions);
            ClearAssignments?.Invoke();
            AssignSlot?.Invoke(actions);
        }

        public void SaveActionAssignments(string path)
        {
            if (_playerManager.LocalEntity is not { } user)
                return;

            var file = new ResPath(path).ToRootedPath();
            var directory = file.Directory;
            if (!_resources.UserData.Exists(directory))
                _resources.UserData.CreateDir(directory);

            using var writer = _resources.UserData.OpenWriteText(file);
            var currentAssignments = _currentAssignments.Count > 0 ? _currentAssignments.ToList() : GetDefaultAssignments(user);
            var assignmentsByAction = currentAssignments.GroupBy(a => a.ActionId).ToDictionary(g => g.Key, g => g.ToList());
            var actions = _actionsSystem.GetActions(user).ToList();

            foreach (var action in actions)
            {
                if (!TryComp<MetaDataComponent>(action, out var metaData) || metaData.EntityPrototype?.ID == null)
                    continue;

                writer.WriteLine($"- action: \"{metaData.EntityPrototype.ID}\"");

                if (assignmentsByAction.TryGetValue(action.Owner, out var actionAssignments) && actionAssignments.Count > 0)
                {
                    writer.WriteLine("  assignments:");
                    foreach (var assignment in actionAssignments)
                    {
                        writer.WriteLine($"    - hotbar: {assignment.Hotbar}");
                        writer.WriteLine($"      slot: {assignment.Slot}");
                    }
                }
                else
                {
                    writer.WriteLine("  assignments: []");
                }
            }
        }

        private List<ActionsSystem.SlotAssignment> GetDefaultAssignments(EntityUid user)
        {
            var actions = _actionsSystem.GetActions(user).ToArray();
            var assignments = new List<ActionsSystem.SlotAssignment>();
            for (var i = 0; i < actions.Length; i++)
                assignments.Add(new ActionsSystem.SlotAssignment(0, (byte)i, actions[i]));
            return assignments;
        }

        public void LoadActionPositions(string path, bool userData = true)
        {
            if (_playerManager.LocalEntity is not { } user)
                return;

            if (!TryComp<ActionsComponent>(user, out var actionsComp))
                return;

            var file = new ResPath(path).ToRootedPath();

            using TextReader reader = userData
                ? _resources.UserData.OpenText(file)
                : _resources.ContentFileReadText(file);

            var yamlStream = new YamlStream();
            yamlStream.Load(reader);

            if (yamlStream.Documents[0].RootNode.ToDataNode() is not SequenceDataNode sequence)
                return;

            var currentActionsByPrototype = new Dictionary<string, EntityUid>();
            foreach (var actionId in actionsComp.Actions)
            {
                if (!TryComp<MetaDataComponent>(actionId, out var metaData) || metaData.EntityPrototype?.ID == null)
                    continue;
                currentActionsByPrototype[metaData.EntityPrototype.ID] = actionId;
            }

            var loadedAssignments = new List<ActionsSystem.SlotAssignment>();
            var foundActions = new HashSet<string>();

            foreach (var entry in sequence.Sequence)
            {
                if (entry is not MappingDataNode map)
                    continue;

                if (!map.TryGet<ValueDataNode>("action", out var actionNode))
                    continue;

                foundActions.Add(actionNode.Value);

                if (!currentActionsByPrototype.TryGetValue(actionNode.Value, out var currentActionId))
                    continue;

                if (!map.TryGet("assignments", out var assignmentNode) || assignmentNode is not SequenceDataNode assignmentSequence)
                    continue;

                foreach (var assignmentEntry in assignmentSequence.Sequence)
                {
                    if (assignmentEntry is not MappingDataNode assignmentMap)
                        continue;

                    if (!assignmentMap.TryGet<ValueDataNode>("hotbar", out var hotbarNode) ||
                        !assignmentMap.TryGet<ValueDataNode>("slot", out var slotNode))
                        continue;

                    if (byte.TryParse(hotbarNode.Value, out var hotbar) && byte.TryParse(slotNode.Value, out var slot))
                    {
                        loadedAssignments.Add(new ActionsSystem.SlotAssignment(hotbar, slot, currentActionId));
                    }
                }
            }

            if (loadedAssignments.Count == 0)
                return;

            // Checking for gap removal
            var totalActionsInFile = foundActions.Count;
            var validActionsLoaded = loadedAssignments.Select(a => a.ActionId).Distinct().Count();

            if (totalActionsInFile > validActionsLoaded)
            {
                // Missing actions -> need to compact
                var compactedAssignments = new List<ActionsSystem.SlotAssignment>();
                var assignmentsByHotbar = loadedAssignments.GroupBy(x => x.Hotbar);

                foreach (var hotbarGroup in assignmentsByHotbar)
                {
                    var sortedAssignments = hotbarGroup.OrderBy(x => x.Slot).ToList();

                    // Reassign slots starting from 0 to remove gaps
                    for (int i = 0; i < sortedAssignments.Count; i++)
                    {
                        var assignment = sortedAssignments[i];
                        compactedAssignments.Add(new ActionsSystem.SlotAssignment(assignment.Hotbar, (byte)i, assignment.ActionId));
                    }
                }

                SetAssignments(compactedAssignments);
            }
            else
            {
                // No missing actions, use assignments as-is
                SetAssignments(loadedAssignments);
            }
        }
    }
}
