using System.Collections.Generic;
using System.Linq;
using Content.Client._RMC14.Announce;
using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Options.UI.Tabs;

public sealed partial class AccessibilityTab
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private AnnouncementLayoutEditorWindow? _announcementLayoutEditorWindow;

    private void RegisterAnnouncementOptions()
    {
        AddPerAnnouncementOverrides();
        RegisterAnnouncementLayoutEditor();
    }

    private void AddPerAnnouncementOverrides()
    {
        var presets = GetRootPresets()
            .OrderBy(preset => preset.Name)
            .ToList();

        if (presets.Count == 0)
        {
            AnnouncementPresetOverridesLabel.Visible = false;
            return;
        }

        foreach (var preset in presets)
        {
            var availablePreferences = GetAvailablePreferences(preset);
            if (availablePreferences.Count == 0)
                continue;

            var dropDown = new OptionDropDown
            {
                Title = preset.Name
            };

            AnnouncementPresetOverridesContainer.AddChild(dropDown);
            Control.AddOption(new AnnouncementPresetOverrideOption(
                Control,
                _cfg,
                dropDown,
                preset.ID,
                availablePreferences));
        }
    }

    private List<AnnouncementPresetPrototype> GetRootPresets()
    {
        return _prototypeManager.EnumeratePrototypes<AnnouncementPresetPrototype>()
            .Where(preset => preset.VisibleInSettings)
            .ToList();
    }

    private List<AnnouncementDisplayPreference> GetAvailablePreferences(AnnouncementPresetPrototype preset)
    {
        var list = new List<AnnouncementDisplayPreference>
        {
            AnnouncementDisplayPreference.Stylized
        };

        if (preset.Presentations.Default != null)
            list.Add(AnnouncementDisplayPreference.Default);

        if (preset.Presentations.Simplified != null)
            list.Add(AnnouncementDisplayPreference.Simplified);

        list.Add(AnnouncementDisplayPreference.Disabled);

        return list;
    }

    private void RegisterAnnouncementLayoutEditor()
    {
        AnnouncementLayoutEditorButton.OnPressed += _ =>
        {
            if (_announcementLayoutEditorWindow != null &&
                !_announcementLayoutEditorWindow.Disposed)
            {
                _announcementLayoutEditorWindow.OpenCentered();
                return;
            }

            _announcementLayoutEditorWindow = new AnnouncementLayoutEditorWindow();
            _announcementLayoutEditorWindow.OnClose += () => _announcementLayoutEditorWindow = null;
            _announcementLayoutEditorWindow.OpenCentered();
        };

        AnnouncementLayoutResetButton.OnPressed += _ =>
        {
            _cfg.SetCVar(RMCCVars.RMCAnnouncementLayout, string.Empty);
            _cfg.SetCVar(RMCCVars.RMCAnnouncementLayoutOverrides, string.Empty);
            UpdateAnnouncementLayoutSummary();
        };

        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementLayout, _ => UpdateAnnouncementLayoutSummary(), true);
        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementLayoutOverrides, _ => UpdateAnnouncementLayoutSummary(), true);
    }

    private void UpdateAnnouncementLayoutSummary()
    {
        var globalOverride = AnnouncementLayoutOverrides.ParseSingle(_cfg.GetCVar(RMCCVars.RMCAnnouncementLayout));
        var overrides = AnnouncementLayoutOverrides.Parse(_cfg.GetCVar(RMCCVars.RMCAnnouncementLayoutOverrides));

        AnnouncementLayoutSummaryLabel.Text = Loc.GetString(
            "rmc-ui-options-announcements-layout-summary",
            ("global", globalOverride != null
                ? Loc.GetString("rmc-ui-options-announcements-layout-summary-custom")
                : Loc.GetString("rmc-ui-options-announcements-layout-summary-default")),
            ("count", overrides.Count));
    }

    private sealed class AnnouncementPresetOverrideOption : BaseOption
    {
        private readonly IConfigurationManager _cfg;
        private readonly OptionDropDown _dropDown;
        private readonly string _presetId;
        private readonly HashSet<AnnouncementDisplayPreference> _availablePreferences;
        private readonly Dictionary<AnnouncementDisplayPreference, int> _entryIds = new();
        private readonly int _inheritId;
        private Dictionary<string, AnnouncementDisplayPreference> _cachedOverrides = new();

        private AnnouncementDisplayPreference? SelectedPreference
        {
            get
            {
                if (_dropDown.Button.SelectedMetadata is not int value || value < 0)
                    return null;

                var preference = (AnnouncementDisplayPreference) value;
                return _availablePreferences.Contains(preference) ? preference : null;
            }
            set
            {
                if (value is { } preference && _entryIds.TryGetValue(preference, out var id))
                {
                    _dropDown.Button.SelectId(id);
                    return;
                }

                _dropDown.Button.SelectId(_inheritId);
            }
        }

        public AnnouncementPresetOverrideOption(
            OptionsTabControlRow controller,
            IConfigurationManager cfg,
            OptionDropDown dropDown,
            string presetId,
            IReadOnlyCollection<AnnouncementDisplayPreference> availablePreferences) : base(controller)
        {
            _cfg = cfg;
            _dropDown = dropDown;
            _presetId = presetId;
            _availablePreferences = new HashSet<AnnouncementDisplayPreference>(availablePreferences);

            _dropDown.Button.AddItem(Loc.GetString("rmc-ui-options-announcements-style-inherit"), 0);
            _dropDown.Button.SetItemMetadata(_dropDown.Button.GetIdx(0), -1);
            _inheritId = 0;

            var nextId = 1;
            foreach (var preference in availablePreferences)
            {
                var key = preference switch
                {
                    AnnouncementDisplayPreference.Stylized => "rmc-ui-options-announcements-style-stylized",
                    AnnouncementDisplayPreference.Default => "rmc-ui-options-announcements-style-default",
                    AnnouncementDisplayPreference.Simplified => "rmc-ui-options-announcements-style-simplified",
                    AnnouncementDisplayPreference.Disabled => "rmc-ui-options-announcements-style-disabled",
                    _ => null
                };

                if (key == null)
                    continue;

                _dropDown.Button.AddItem(Loc.GetString(key), nextId);
                _dropDown.Button.SetItemMetadata(_dropDown.Button.GetIdx(nextId), (int) preference);
                _entryIds[preference] = nextId;
                nextId++;
            }

            _dropDown.Button.OnItemSelected += args =>
            {
                _dropDown.Button.SelectId(args.Id);
                ValueChanged();
            };

            _cachedOverrides = AnnouncementPreferenceOverrides.Parse(_cfg.GetCVar(RMCCVars.RMCAnnouncementStyleOverrides));
            _cfg.OnValueChanged(RMCCVars.RMCAnnouncementStyleOverrides, OnOverridesChanged);
        }

        private void OnOverridesChanged(string serialized)
        {
            _cachedOverrides = AnnouncementPreferenceOverrides.Parse(serialized);
        }

        public override void LoadValue()
        {
            SelectedPreference = GetStoredPreference();
        }

        public override void SaveValue()
        {
            var overrides = new Dictionary<string, AnnouncementDisplayPreference>(_cachedOverrides);
            if (SelectedPreference is { } preference)
                overrides[_presetId] = preference;
            else
                overrides.Remove(_presetId);

            _cfg.SetCVar(RMCCVars.RMCAnnouncementStyleOverrides, AnnouncementPreferenceOverrides.Serialize(overrides));
        }

        public override void ResetToDefault()
        {
            SelectedPreference = null;
        }

        public override bool IsModified()
        {
            return SelectedPreference != GetStoredPreference();
        }

        public override bool IsModifiedFromDefault()
        {
            return SelectedPreference != null;
        }

        private AnnouncementDisplayPreference? GetStoredPreference()
        {
            if (!_cachedOverrides.TryGetValue(_presetId, out var preference))
                return null;

            return _availablePreferences.Contains(preference) ? preference : null;
        }
    }
}
