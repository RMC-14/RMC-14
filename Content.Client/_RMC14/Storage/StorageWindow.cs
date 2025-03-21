using Content.Shared.Storage;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

// ReSharper disable CheckNamespace

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed partial class StorageWindow
{
    private (Box2i Grid, EntityUid[] Contained, Dictionary<EntityUid, ItemStorageLocation> Stored) _lastUpdate =
        (default, Array.Empty<EntityUid>(), new Dictionary<EntityUid, ItemStorageLocation>());

    private PanelContainer? WrapBorders(Control control, int? fixedSizeX, int x, int right)
    {
        if (fixedSizeX != null &&
            x != 0 &&
            (x != right || x % fixedSizeX == 0))
        {
            var thickness = x % fixedSizeX == 0
                ? new Thickness(1, 0, 0, 0)
                : new Thickness(0, 0, 1, 0);
            var panel = new PanelContainer();
            panel.PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Black,
                BorderThickness = thickness
            };
            panel.AddChild(control);
            return panel;
        }

        return null;
    }
}
