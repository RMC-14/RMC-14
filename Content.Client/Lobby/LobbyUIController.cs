using System.Collections.Generic;
using System.Linq;
using Content.Client._RMC14.Lobby;
using Content.Client.Guidebook;
using Content.Client.Humanoid;
using Content.Client.Inventory;
using Content.Client.Lobby.UI;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.Station;
using Content.Shared._RMC14.Armor;
using Content.Shared.CCVar;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby;

public sealed class LobbyUIController : UIController, IOnStateEntered<LobbyState>, IOnStateExited<LobbyState>
{
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IFileDialogManager _dialogManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly JobRequirementsManager _requirements = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [UISystemDependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [UISystemDependency] private readonly ClientInventorySystem _inventory = default!;
    [UISystemDependency] private readonly StationSpawningSystem _spawn = default!;
    [UISystemDependency] private readonly GuidebookSystem _guide = default!;
    [UISystemDependency] private readonly CMArmorSystem _armorSystem = default!;

    private CharacterSetupGui? _characterSetup;
    private HumanoidProfileEditor? _profileEditor;
    private RMCJobPrioritiesWindow? _jobPrioritiesWindow; // RMC14
    private CharacterSetupGuiSavePanel? _savePanel;

    /// <summary>
    /// Event invoked when any character or job selection or job priority is changed.
    /// Basically anything that might change round start character/job selection.
    /// </summary>
    public event Action? OnAnyCharacterOrJobChange;

    /// <summary>
    /// This is the characher preview panel in the chat. This should only update if their character updates.
    /// </summary>
    private LobbyCharacterPreviewPanel? PreviewPanel => GetLobbyPreview();

    /// <summary>
    /// This is the modified profile currently being edited.
    /// </summary>
    private HumanoidCharacterProfile? EditedProfile => _profileEditor?.Profile;

    private int? EditedSlot => _profileEditor?.CharacterSlot;

    public override void Initialize()
    {
        base.Initialize();
        _prototypeManager.PrototypesReloaded += OnProtoReload;
        _preferencesManager.OnServerDataLoaded += PreferencesDataLoaded;
        _preferencesManager.OnPreferencesUpdated += PreferencesUpdated; // RMC14
        _requirements.Updated += OnRequirementsUpdated;
        OnAnyCharacterOrJobChange += RefreshLobbyPreview;

        _configurationManager.OnValueChanged(CCVars.FlavorText, args =>
        {
            _profileEditor?.RefreshFlavorText();
        });

        _configurationManager.OnValueChanged(CCVars.GameRoleTimers, _ => RefreshEditors());
        _configurationManager.OnValueChanged(CCVars.GameRoleWhitelist, _ => RefreshEditors());
    }

    private LobbyCharacterPreviewPanel? GetLobbyPreview()
    {
        if (_stateManager.CurrentState is LobbyState lobby)
        {
            return lobby.Lobby?.CharacterPreview;
        }

        return null;
    }

    private void OnRequirementsUpdated()
    {
        _profileEditor?.RefreshAntags();
        _profileEditor?.RefreshJobs();
        _jobPrioritiesWindow?.Refresh(); // RMC14
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (_profileEditor != null)
        {
            if (obj.WasModified<AntagPrototype>())
            {
                _profileEditor.RefreshAntags();
            }

            if (obj.WasModified<JobPrototype>() ||
                obj.WasModified<DepartmentPrototype>())
            {
                _profileEditor.RefreshJobs();
            }

            if (obj.WasModified<LoadoutPrototype>() ||
                obj.WasModified<LoadoutGroupPrototype>() ||
                obj.WasModified<RoleLoadoutPrototype>())
            {
                _profileEditor.RefreshLoadouts();
            }

            if (obj.WasModified<SpeciesPrototype>())
            {
                _profileEditor.RefreshSpecies();
            }

            if (obj.WasModified<TraitPrototype>())
            {
                _profileEditor.RefreshTraits();
            }
        }
        OnAnyCharacterOrJobChange?.Invoke();
    }

    // RMC14
    private void PreferencesUpdated()
    {
        if (_stateManager.CurrentState is not LobbyState)
            return;

        RefreshLobbyPreview();
        _characterSetup?.ReloadCharacterPickers();
    }

    private void PreferencesDataLoaded()
    {
        PreviewPanel?.SetLoaded(true);

        if (_stateManager.CurrentState is not LobbyState)
            return;

        if (_characterSetup != null)
            _characterSetup.SelectedCharacterSlot = null;
        ReloadCharacterSetup();
    }

    public void OnStateEntered(LobbyState state)
    {
        var previewPanel = state.Lobby?.CharacterPreview;
        if (previewPanel != null)
        {
            previewPanel.JobPrioritiesButton.OnPressed -= OnJobPrioritiesPressed; // RMC14
            previewPanel.JobPrioritiesButton.OnPressed += OnJobPrioritiesPressed; // RMC14
            previewPanel.SetLoaded(_preferencesManager.ServerDataLoaded);
        }

        if (_characterSetup != null)
            _characterSetup.SelectedCharacterSlot = null;
        ReloadCharacterSetup();
    }

    public void OnStateExited(LobbyState state)
    {
        var previewPanel = state.Lobby?.CharacterPreview;
        if (previewPanel != null)
        {
            previewPanel.JobPrioritiesButton.OnPressed -= OnJobPrioritiesPressed; // RMC14
            previewPanel.SetLoaded(false);
        }

        _jobPrioritiesWindow?.Close(); // RMC14

        if (_stateManager.CurrentState is LobbyState lobby)
        {
            lobby.Lobby?.CharacterSetupState.RemoveAllChildren();
        }
    }

    /// <summary>
    /// Reloads every single character setup control.
    /// </summary>
    public void ReloadCharacterSetup()
    {
        RefreshLobbyPreview();
        var (characterGui, profileEditor) = EnsureGui();
        characterGui.ReloadCharacterPickers();
        profileEditor.ResetToDefault();
    }

    /// <summary>
    /// Refreshes the character preview in the lobby chat.
    /// </summary>
    private void RefreshLobbyPreview()
    {
        PreviewPanel?.Refresh();
        _jobPrioritiesWindow?.Refresh(); // RMC14
    }

    private void RefreshEditors()
    {
        _profileEditor?.RefreshAntags();
        _profileEditor?.RefreshJobs();
        _profileEditor?.RefreshLoadouts();
        _jobPrioritiesWindow?.Refresh(); // RMC14
    }

    // RMC14 start
    public void SetJobPriorities(Dictionary<ProtoId<JobPrototype>, JobPriority> priorities)
    {
        _preferencesManager.UpdateJobPriorities(priorities);
        _profileEditor?.RefreshPreviewForPriorityUpdate();
        _characterSetup?.ReloadCharacterPickers();
        OnAnyCharacterOrJobChange?.Invoke();
    }

    public void OpenJobPrioritiesWindow()
    {
        if (_jobPrioritiesWindow != null)
        {
            _jobPrioritiesWindow.MoveToFront();
            return;
        }

        _jobPrioritiesWindow = new RMCJobPrioritiesWindow();
        _jobPrioritiesWindow.PrioritiesChanged += SetJobPriorities;
        _jobPrioritiesWindow.SettingsChanged += _preferencesManager.UpdatePlayerSettings;
        _jobPrioritiesWindow.OnClose += () => _jobPrioritiesWindow = null;
        _jobPrioritiesWindow.OpenCentered();
    }

    private void OnJobPrioritiesPressed(BaseButton.ButtonEventArgs args)
    {
        OpenJobPrioritiesWindow();
    }
    // RMC14 end

    private void SaveProfile()
    {
        DebugTools.Assert(EditedProfile != null);

        if (EditedProfile == null || EditedSlot == null)
            return;

        var fixedProfile = EditedProfile.Clone();
        if (_preferencesManager.Preferences!.TryGetHumanoidInSlot(EditedSlot.Value, out var humanoid))
            fixedProfile = EditedProfile.WithEnabled(humanoid?.Enabled ?? true);

        _preferencesManager.UpdateCharacter(fixedProfile, EditedSlot.Value);
        OnAnyCharacterOrJobChange?.Invoke();
        _profileEditor?.SetProfile(EditedSlot.Value);
        ReloadCharacterSetup();
    }

    private void CloseProfileEditor()
    {
        if (_profileEditor == null)
            return;

        _profileEditor.SetProfile(null, null);
        _profileEditor.Visible = false;

        if (_stateManager.CurrentState is LobbyState lobbyGui)
        {
            lobbyGui.SwitchState(LobbyGui.LobbyGuiState.Default);
        }
        RefreshLobbyPreview();
    }

    private void OpenSavePanel(Action saveAction)
    {
        if (_savePanel is { IsOpen: true })
            return;

        _savePanel = new CharacterSetupGuiSavePanel();

        _savePanel.SaveButton.OnPressed += _ =>
        {
            saveAction?.Invoke();

            _savePanel.Close();

            CloseProfileEditor();
        };

        _savePanel.NoSaveButton.OnPressed += _ =>
        {
            _savePanel.Close();

            CloseProfileEditor();
        };

        _savePanel.OpenCentered();
    }

    private (CharacterSetupGui, HumanoidProfileEditor) EnsureGui()
    {
        if (_characterSetup != null && _profileEditor != null)
        {
            _characterSetup.Visible = true;
            _profileEditor.Visible = true;
            return (_characterSetup, _profileEditor);
        }

        _profileEditor = new HumanoidProfileEditor(
            _preferencesManager,
            _configurationManager,
            EntityManager,
            _dialogManager,
            LogManager,
            _playerManager,
            _prototypeManager,
            _resourceCache,
            _requirements,
            _markings);

        _profileEditor.OnOpenGuidebook += _guide.OpenHelp;

        _characterSetup = new CharacterSetupGui(_profileEditor); // RMC14

        _characterSetup.CloseButton.OnPressed += _ =>
        {
            // Open the save panel if we have unsaved changes.
            if( _profileEditor.Visible && _profileEditor.Profile != null && _profileEditor.IsDirty)
            {
                OpenSavePanel(SaveProfile);

                return;
            }

            // Reset sliders etc.
            CloseProfileEditor();
        };

        _profileEditor.Save += SaveProfile;

        _characterSetup.SelectCharacter += args =>
        {
            _preferencesManager.SelectCharacter(args);
            _profileEditor.SetProfile(args);
            if (_characterSetup != null)
                _characterSetup.SelectedCharacterSlot = args;
            ReloadCharacterSetup();
        };

        _characterSetup.DeleteCharacter += args =>
        {
            _preferencesManager.DeleteCharacter(args);

            // Reload everything
            if (EditedSlot == args)
            {
                ReloadCharacterSetup();
            }
            else
            {
                // Only need to reload character pickers
                _characterSetup?.ReloadCharacterPickers();
            }

            OnAnyCharacterOrJobChange?.Invoke();
        };

        _characterSetup.SetCharacterEnable += args =>
        {
            _preferencesManager.SetCharacterEnable(args.Item1, args.Item2);
            OnAnyCharacterOrJobChange?.Invoke();
            _characterSetup?.ReloadCharacterPickers();
        };

        if (_stateManager.CurrentState is LobbyState lobby)
        {
            lobby.Lobby?.CharacterSetupState.AddChild(_characterSetup);
        }

        return (_characterSetup, _profileEditor);
    }

    /// <summary>
    /// Loads a profile onto a temporary dummy entity for lobby preview.
    /// </summary>
    public EntityUid LoadProfileEntity(HumanoidCharacterProfile? profile, JobPrototype? job, bool jobClothes)
    {
        EntityUid dummy;

        EntProtoId? previewEntity = null;
        if (profile != null && jobClothes)
        {
            job ??= GetPreferredJob(profile);
            if (job != null)
                previewEntity = job.JobPreviewEntity ?? (EntProtoId?) job.JobEntity;
        }

        if (previewEntity != null)
            return EntityManager.SpawnEntity(previewEntity, MapCoordinates.Nullspace);

        if (profile != null)
        {
            var doll = _prototypeManager.Index<SpeciesPrototype>(profile.Species).DollPrototype;
            dummy = EntityManager.SpawnEntity(doll, MapCoordinates.Nullspace);
        }
        else
        {
            var doll = _prototypeManager.Index<SpeciesPrototype>(SharedHumanoidAppearanceSystem.DefaultSpecies).DollPrototype;
            dummy = EntityManager.SpawnEntity(doll, MapCoordinates.Nullspace);
        }

        _humanoid.LoadProfile(dummy, profile);

        if (profile != null && jobClothes)
        {
            job ??= GetPreferredJob(profile);
            if (job != null)
            {
                GiveDummyJobClothes(dummy, profile, job);

                var loadoutRoleId = ResolveLoadoutRoleId(job);
                if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(loadoutRoleId))
                {
                    var loadout = profile.GetLoadoutOrDefault(
                        loadoutRoleId,
                        _playerManager.LocalSession,
                        profile.Species,
                        EntityManager,
                        _prototypeManager);

                    GiveDummyLoadout(dummy, loadout);
                }
            }
        }

        return dummy;
    }

    private JobPrototype? GetPreferredJob(HumanoidCharacterProfile profile)
    {
        IReadOnlyDictionary<ProtoId<JobPrototype>, JobPriority> globalPriorities = _preferencesManager.Preferences?.JobPriorities ?? new Dictionary<ProtoId<JobPrototype>, JobPriority>(); // RMC14
        var candidates = profile.JobPreferences
            .Where(jobId => _prototypeManager.HasIndex<JobPrototype>(jobId))
            .Select(jobId => _prototypeManager.Index<JobPrototype>(jobId))
            .ToList();

        if (candidates.Count == 0)
            return null;

        JobPriority GetEffectivePriority(JobPrototype job)
        {
            return globalPriorities.GetValueOrDefault(job.ID, JobPriority.Never);
        }

        candidates.Sort((a, b) =>
        {
            var priorityCompare = GetEffectivePriority(b).CompareTo(GetEffectivePriority(a));
            if (priorityCompare != 0)
                return priorityCompare;

            return string.CompareOrdinal(a.ID, b.ID);
        });

        return candidates[0];
    }

    private string ResolveLoadoutRoleId(JobPrototype job)
    {
        var sourceJob = ResolveLoadoutSourceJob(job) ?? job;
        return LoadoutSystem.GetJobPrototype(sourceJob.ID);
    }

    private JobPrototype? ResolveLoadoutSourceJob(JobPrototype job)
    {
        var visited = new HashSet<string>();
        var current = job;

        while (visited.Add(current.ID))
        {
            if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(current.ID)))
                return current;

            if (current.UseLoadoutOfJob is not { } parentId ||
                !_prototypeManager.TryIndex(parentId, out current))
            {
                break;
            }
        }

        return null;
    }

    private void GiveDummyLoadout(EntityUid uid, RoleLoadout? roleLoadout)
    {
        if (roleLoadout == null)
            return;

        foreach (var group in roleLoadout.SelectedLoadouts.Values)
        {
            foreach (var loadout in group)
            {
                if (!_prototypeManager.TryIndex(loadout.Prototype, out var loadoutProto))
                    continue;

                _spawn.EquipStartingGear(uid, loadoutProto);
            }
        }
    }

    private void GiveDummyJobClothes(EntityUid dummy, HumanoidCharacterProfile profile, JobPrototype job)
    {
        if (!_inventory.TryGetSlots(dummy, out var slots))
            return;

        var resolvedLoadoutRoleId = ResolveLoadoutRoleId(job);
        var defaultLoadoutRoleId = LoadoutSystem.GetJobPrototype(job.ID);

        if (profile.Loadouts.TryGetValue(resolvedLoadoutRoleId, out var jobLoadout) ||
            profile.Loadouts.TryGetValue(defaultLoadoutRoleId, out jobLoadout) ||
            profile.Loadouts.TryGetValue(job.ID, out jobLoadout))
        {
            foreach (var loadouts in jobLoadout.SelectedLoadouts.Values)
            {
                foreach (var loadout in loadouts)
                {
                    if (!_prototypeManager.TryIndex(loadout.Prototype, out var loadoutProto))
                        continue;

                    foreach (var slot in slots)
                    {
                        if (_prototypeManager.TryIndex(loadoutProto.StartingGear, out var loadoutGear))
                        {
                            var itemType = ((IEquipmentLoadout) loadoutGear).GetGear(slot.Name);

                            if (_inventory.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                                EntityManager.DeleteEntity(unequippedItem.Value);

                            if (itemType != string.Empty)
                            {
                                var item = EntityManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                _inventory.TryEquip(dummy, item, slot.Name, true, true);
                            }
                        }
                        else
                        {
                            var itemType = ((IEquipmentLoadout) loadoutProto).GetGear(slot.Name);

                            if (_inventory.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                                EntityManager.DeleteEntity(unequippedItem.Value);

                            if (itemType != string.Empty)
                            {
                                var item = EntityManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                _inventory.TryEquip(dummy, item, slot.Name, true, true);
                            }
                        }
                    }
                }
            }
        }

        if (!_prototypeManager.TryIndex(job.StartingGear, out var gear))
            return;

        _prototypeManager.TryIndex(job.DummyStartingGear, out var dummyGear);

        foreach (var slot in slots)
        {
            var itemType = ((IEquipmentLoadout) gear).GetGear(slot.Name);
            if (itemType == string.Empty && dummyGear != null)
                itemType = ((IEquipmentLoadout) dummyGear).GetGear(slot.Name);

            if (_inventory.TryUnequip(dummy, slot.Name, out var unequipped, silent: true, force: true, reparent: false))
                EntityManager.DeleteEntity(unequipped.Value);

            if (itemType == string.Empty)
                continue;

            var item = EntityManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
            if (EntityManager.TryGetComponent<RMCArmorVariantComponent>(item, out var variantComponent))
            {
                var variantItemProtoId = _armorSystem.GetArmorVariant((item, variantComponent), profile.ArmorPreference);
                var variantItem = EntityManager.SpawnEntity(variantItemProtoId, MapCoordinates.Nullspace);
                _inventory.TryEquip(dummy, variantItem, slot.Name, true, true);
                EntityManager.QueueDeleteEntity(item);
                continue;
            }

            _inventory.TryEquip(dummy, item, slot.Name, true, true);
        }
    }
}
