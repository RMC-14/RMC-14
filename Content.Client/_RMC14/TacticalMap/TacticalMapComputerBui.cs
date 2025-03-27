using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.TacticalMap;

[UsedImplicitly]
public sealed class TacticalMapComputerBui(EntityUid owner, Enum uiKey) : RMCPopOutBui<TacticalMapWindow>(owner, uiKey)
{
    [Dependency] private readonly IPlayerManager _player = default!;

    protected override TacticalMapWindow? Window { get; set; }
    private bool _refreshed;

    protected override void Open()
    {
        base.Open();
        Window = this.CreatePopOutableWindow<TacticalMapWindow>();

        TabContainer.SetTabTitle(Window.MapTab, "Map");
        TabContainer.SetTabVisible(Window.MapTab, true);

        var computer = EntMan.GetComponentOrNull<TacticalMapComputerComponent>(Owner);
        var skills = EntMan.System<SkillsSystem>();
        if (computer != null &&
            _player.LocalEntity is { } player &&
            skills.HasSkill(player, computer.Skill, computer.SkillLevel))
        {
            TabContainer.SetTabTitle(Window.CanvasTab, "Canvas");
            TabContainer.SetTabVisible(Window.CanvasTab, true);
        }
        else
        {
            TabContainer.SetTabVisible(Window.CanvasTab, false);
        }

        if (computer != null &&
            EntMan.TryGetComponent(computer.Map, out AreaGridComponent? areaGrid))
        {
            Window.UpdateTexture((computer.Map.Value, areaGrid));
        }

        Refresh();

        Window.UpdateCanvasButton.OnPressed += _ => SendPredictedMessage(new TacticalMapUpdateCanvasMsg(Window.Canvas.Lines));
    }

    public void Refresh()
    {
        if (Window == null)
            return;

        var lineLimit = EntMan.System<TacticalMapSystem>().LineLimit;
        Window.SetLineLimit(lineLimit);
        UpdateBlips();

        if (EntMan.TryGetComponent(Owner, out TacticalMapComputerComponent? computer))
        {
            Window.LastUpdateAt = computer.LastAnnounceAt;
            Window.NextUpdateAt = computer.NextAnnounceAt;
        }

        Window.Map.Lines.Clear();

        var lines = EntMan.GetComponentOrNull<TacticalMapLinesComponent>(Owner);
        if (lines != null)
            Window.Map.Lines.AddRange(lines.MarineLines);

        if (_refreshed)
            return;

        if (lines != null)
            Window.Canvas.Lines.AddRange(lines.MarineLines);

        _refreshed = true;
    }

    private void UpdateBlips()
    {
        if (Window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out TacticalMapComputerComponent? computer))
        {
            Window.UpdateBlips(null);
            return;
        }

        var blips = new TacticalMapBlip[computer.Blips.Count];
        var i = 0;

        foreach (var blip in computer.Blips.Values)
        {
            blips[i++] = blip;
        }

        Window.UpdateBlips(blips);
    }
}
