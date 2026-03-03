using System;
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

    private void RegisterAnnouncementOptions()
    {
        var announcementEntries = new List<OptionDropDownCVar<AnnouncementDisplayPreference>.ValueOption>
        {
            new(AnnouncementDisplayPreference.Stylized, Loc.GetString("rmc-ui-options-announcements-style-stylized")),
            new(AnnouncementDisplayPreference.Default, Loc.GetString("rmc-ui-options-announcements-style-default")),
            new(AnnouncementDisplayPreference.Simplified, Loc.GetString("rmc-ui-options-announcements-style-simplified")),
            new(AnnouncementDisplayPreference.Disabled, Loc.GetString("rmc-ui-options-announcements-style-disabled"))
        };

        Control.AddOptionDropDown(RMCCVars.RMCAnnouncementStyle, AnnouncementStyleDropDown, announcementEntries);
        AddPerAnnouncementOverrides();
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
        var presets = _prototypeManager.EnumeratePrototypes<AnnouncementPresetPrototype>().ToList();
        var variantIds = new HashSet<string>();

        foreach (var preset in presets)
        {
            if (!string.IsNullOrWhiteSpace(preset.StylizedVariant))
                variantIds.Add(preset.StylizedVariant);

            if (!string.IsNullOrWhiteSpace(preset.DefaultVariant))
                variantIds.Add(preset.DefaultVariant);

            if (!string.IsNullOrWhiteSpace(preset.SimplifiedVariant))
                variantIds.Add(preset.SimplifiedVariant);
        }

        return presets
            .Where(preset => !variantIds.Contains(preset.ID) && preset.VisibleInSettings)
            .ToList();
    }

    private List<AnnouncementDisplayPreference> GetAvailablePreferences(AnnouncementPresetPrototype preset)
    {
        var list = new List<AnnouncementDisplayPreference>
        {
            AnnouncementDisplayPreference.Stylized
        };

        if (!string.IsNullOrWhiteSpace(preset.DefaultVariant))
            list.Add(AnnouncementDisplayPreference.Default);

        if (!string.IsNullOrWhiteSpace(preset.SimplifiedVariant))
            list.Add(AnnouncementDisplayPreference.Simplified);

        list.Add(AnnouncementDisplayPreference.Disabled);

        return list;
    }

    private sealed class AnnouncementPresetOverrideOption : BaseOption
    {
        private readonly IConfigurationManager _cfg;
        private readonly OptionDropDown _dropDown;
        private readonly string _presetId;
        private readonly HashSet<AnnouncementDisplayPreference> _availablePreferences;
        private readonly Dictionary<AnnouncementDisplayPreference, int> _entryIds = new();
        private readonly int _inheritId;

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
        }

        public override void LoadValue()
        {
            SelectedPreference = GetStoredPreference();
        }

        public override void SaveValue()
        {
            var overrides = GetStoredOverrides();
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

        private Dictionary<string, AnnouncementDisplayPreference> GetStoredOverrides()
        {
            var serialized = _cfg.GetCVar(RMCCVars.RMCAnnouncementStyleOverrides);
            return AnnouncementPreferenceOverrides.Parse(serialized);
        }

        private AnnouncementDisplayPreference? GetStoredPreference()
        {
            var overrides = GetStoredOverrides();
            if (!overrides.TryGetValue(_presetId, out var preference))
                return null;

            return _availablePreferences.Contains(preference) ? preference : null;
        }
    }
}
