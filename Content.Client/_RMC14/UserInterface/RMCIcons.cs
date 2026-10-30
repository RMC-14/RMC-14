using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface;

public static class RMCIcons
{
    private const string Dir = "/Textures/_RMC14/Interface/Icons/";

    public const string ArrowRightLong = Dir + "arrow-right-long.svg.192dpi.png";
    public const string ArrowsRotate = Dir + "arrows-rotate.svg.192dpi.png";
    public const string Book = Dir + "book.svg.192dpi.png";
    public const string Eject = Dir + "eject.svg.192dpi.png";
    public const string Flask = Dir + "flask.svg.192dpi.png";
    public const string FloppyDisk = Dir + "floppy-disk.svg.192dpi.png";
    public const string Gear = Dir + "gear.svg.192dpi.png";
    public const string Link = Dir + "link.svg.192dpi.png";
    public const string Play = Dir + "play.svg.192dpi.png";
    public const string Print = Dir + "print.svg.192dpi.png";
    public const string Radiation = Dir + "radiation.svg.192dpi.png";
    public const string Server = Dir + "server.svg.192dpi.png";
    public const string SquareMinus = Dir + "square-minus.svg.192dpi.png";
    public const string SquarePlus = Dir + "square-plus.svg.192dpi.png";
    public const string Stop = Dir + "stop.svg.192dpi.png";
    public const string Trash = Dir + "trash.svg.192dpi.png";

    public static readonly Color Idle = Color.Black;
    public static readonly Color Hover = Color.White;
    public static readonly Color Yellow = Color.FromHex("#FFD21A");
    public static RMCIconLabelRow AddIconLabel(
        this Button button,
        string path,
        string text,
        float size = 14,
        Control.HAlignment alignment = Control.HAlignment.Left,
        Color? fontColor = null)
    {
        button.Text = null;

        var row = new RMCIconLabelRow { HorizontalAlignment = alignment };
        row.Icon.IconSize = size;
        row.SetIcon(path);
        row.Label.Text = text;

        if (fontColor != null)
            row.Label.FontColorOverride = fontColor;

        if (alignment == Control.HAlignment.Stretch)
        {
            row.Label.HorizontalExpand = true;
            row.Label.ClipText = true;
        }

        row.BindHover(button);
        button.AddChild(row);
        return row;
    }
}
