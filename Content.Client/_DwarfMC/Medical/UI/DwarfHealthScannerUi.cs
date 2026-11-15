using Content.Client._RMC14.Medical.Scanner;
using Content.Shared._DwarfMC.Medical.DwarfMed;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;

namespace Content.Client._DwarfMC.Medical.UI;

public static class DwarfHealthScannerUi
{
    private sealed class State
    {
        public readonly DwarfHealthScannerPanel Panel = new();
        public readonly List<DwarfBodyPartScan> Parts = new();
        public readonly List<DwarfOrganScan> Organs = new();
        public BodyZone SelectedZone = BodyZone.Chest;
        public NetEntity? LastTarget;
        public bool Attached;
    }

    private static readonly Dictionary<HealthScannerWindow, State> Windows = new();

    public static void Populate(HealthScannerWindow window, EntityUid target, IEntityManager entities)
    {
        if (!Windows.TryGetValue(window, out var state))
        {
            state = new State();
            Windows[window] = state;
            window.OnClose += () => Windows.Remove(window);
        }

        if (!state.Attached)
            state.Attached = Attach(window, state);

        if (!state.Attached)
            return;

        var netTarget = entities.GetNetEntity(target);
        if (state.LastTarget != netTarget)
        {
            state.LastTarget = netTarget;
            state.SelectedZone = BodyZone.Chest;
        }

        var scans = entities.System<DwarfBodyPartScanSystem>();
        state.Parts.Clear();
        state.Parts.AddRange(scans.Build(target));
        state.Organs.Clear();
        state.Organs.AddRange(scans.BuildOrgans(target));

        var any = state.Parts.Count > 0 || state.Organs.Count > 0;
        state.Panel.Visible = any;
        if (!any)
            return;

        state.Panel.SetScan(state.Parts, state.Organs);
        state.Panel.SetSelected(state.SelectedZone);
        state.Panel.PopulateStatus(state.Parts, state.Organs, state.SelectedZone);
    }

    private static bool Attach(HealthScannerWindow window, State state)
    {
        if (window.Contents.ChildCount == 0 ||
            window.Contents.GetChild(0) is not BoxContainer root)
        {
            return false;
        }

        root.AddChild(state.Panel);
        state.Panel.PartSelected += zone =>
        {
            state.SelectedZone = zone;
            state.Panel.SetSelected(zone);
            state.Panel.PopulateStatus(state.Parts, state.Organs, zone);
        };

        return true;
    }
}
