using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface;

public static class RMCIcons
{
    private const string Dir = "/Textures/_RMC14/Interface/Icons/";

    public const string ArrowDown19 = Dir + "arrow-down-1-9.svg.192dpi.png";
    public const string ArrowDown91 = Dir + "arrow-down-9-1.svg.192dpi.png";
    public const string ArrowDownAZ = Dir + "arrow-down-a-z.svg.192dpi.png";
    public const string ArrowDownZA = Dir + "arrow-down-z-a.svg.192dpi.png";
    public const string ArrowRight = Dir + "arrow-right.svg.192dpi.png";
    public const string ArrowsRotate = Dir + "arrows-rotate.svg.192dpi.png";
    public const string ArrowsSplitUpAndLeft = Dir + "arrows-split-up-and-left.svg.192dpi.png";
    public const string Ban = Dir + "ban.svg.192dpi.png";
    public const string Book = Dir + "book.svg.192dpi.png";
    public const string Box = Dir + "box.svg.192dpi.png";
    public const string Check = Dir + "check.svg.192dpi.png";
    public const string CircleDot = Dir + "circle-dot.svg.192dpi.png";
    public const string CircleMinus = Dir + "circle-minus.svg.192dpi.png";
    public const string CirclePlus = Dir + "circle-plus.svg.192dpi.png";
    public const string CircleXmark = Dir + "circle-xmark.svg.192dpi.png";
    public const string DiceOne = Dir + "dice-one.svg.192dpi.png";
    public const string Download = Dir + "download.svg.192dpi.png";
    public const string Eject = Dir + "eject.svg.192dpi.png";
    public const string EqualsSign = Dir + "equals.svg.192dpi.png";
    public const string Exclamation = Dir + "exclamation.svg.192dpi.png";
    public const string FileImport = Dir + "file-import.svg.192dpi.png";
    public const string Flask = Dir + "flask.svg.192dpi.png";
    public const string FloppyDisk = Dir + "floppy-disk.svg.192dpi.png";
    public const string Gear = Dir + "gear.svg.192dpi.png";
    public const string HardDrive = Dir + "hard-drive.svg.192dpi.png";
    public const string Industry = Dir + "industry.svg.192dpi.png";
    public const string Link = Dir + "link.svg.192dpi.png";
    public const string LinkSlash = Dir + "link-slash.svg.192dpi.png";
    public const string Pause = Dir + "pause.svg.192dpi.png";
    public const string Play = Dir + "play.svg.192dpi.png";
    public const string Print = Dir + "print.svg.192dpi.png";
    public const string Radiation = Dir + "radiation.svg.192dpi.png";
    public const string Repeat = Dir + "repeat.svg.192dpi.png";
    public const string Snowplow = Dir + "snowplow.svg.192dpi.png";
    public const string SquareMinus = Dir + "square-minus.svg.192dpi.png";
    public const string SquarePlus = Dir + "square-plus.svg.192dpi.png";
    public const string Trash = Dir + "trash.svg.192dpi.png";
    public const string Upload = Dir + "upload.svg.192dpi.png";
    public const string Xmark = Dir + "xmark.svg.192dpi.png";

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
