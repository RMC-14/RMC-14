using System.Linq;
using Content.Client.Options.UI;
using Content.Shared._RMC14.Audio.Mute;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client._RMC14.UserInterface.Options;

public sealed class OptionMutedSoundsCVar : BaseOptionCVar<string>
{
    private readonly Dictionary<string, CheckBox> _checkBoxes;

    protected override string Value
    {
        get => RMCMutedSoundsSystem.Serialize(_checkBoxes
            .Where(pair => !pair.Value.Pressed)
            .Select(pair => pair.Key)
            .Order());
        set
        {
            var muted = RMCMutedSoundsSystem.Parse(value).ToHashSet();
            foreach (var (id, checkBox) in _checkBoxes)
            {
                checkBox.Pressed = !muted.Contains(id);
            }
        }
    }

    public OptionMutedSoundsCVar(
        OptionsTabControlRow controller,
        IConfigurationManager cfg,
        CVarDef<string> cVar,
        Dictionary<string, CheckBox> checkBoxes)
        : base(controller, cfg, cVar)
    {
        _checkBoxes = checkBoxes;
        foreach (var checkBox in checkBoxes.Values)
        {
            checkBox.OnToggled += _ => ValueChanged();
        }
    }

    protected override bool IsValueEqual(string a, string b)
    {
        var mutedA = RMCMutedSoundsSystem.Parse(a).Where(_checkBoxes.ContainsKey).ToHashSet();
        var mutedB = RMCMutedSoundsSystem.Parse(b).Where(_checkBoxes.ContainsKey).ToHashSet();
        return mutedA.SetEquals(mutedB);
    }
}
