using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Marines.GroundsideOperations;

/// <summary>
/// Draws the Font Awesome glyphs used by the groundside console without loading a fixed-size bitmap font.
/// The masks were rasterized from SS13's Font Awesome Free 6 font at its native CRT icon size.
/// </summary>
public sealed class GroundsideOperationsIconControl : Control
{
    private const int GlyphSize = 18;
    private const int RowBits = 24;

    private static readonly uint[] Heartbeat =
    [
        0x000000, 0x000000, 0x000000, 0x0E1C00, 0x1F7E00, 0x3FFF00,
        0x3FFF00, 0x3CFF00, 0x3CDF00, 0x03A000, 0x0F3C00, 0x077800,
        0x03F000, 0x01E000, 0x00C000, 0x000000, 0x000000, 0x000000,
    ];

    private static readonly uint[] IdCard =
    [
        0x000000, 0x000000, 0x000000, 0x7FFF80, 0x000000, 0x7FFF80,
        0x7FFF80, 0x79E180, 0x70FF80, 0x71FF80, 0x7FE180, 0x607F80,
        0x607F80, 0x7FFF80, 0x7FFF80, 0x000000, 0x000000, 0x000000,
    ];

    private static readonly uint[] Warning =
    [
        0x000000, 0x000000, 0x018000, 0x018000, 0x03C000, 0x066000,
        0x066000, 0x0E7000, 0x0E7000, 0x1E7800, 0x1FF800, 0x3E7C00,
        0x3E7C00, 0x7FFE00, 0x7FFE00, 0x000000, 0x000000, 0x000000,
    ];

    private static readonly uint[] Cog =
    [
        0x000000, 0x000000, 0x00E000, 0x01E000, 0x09E400, 0x1FFE00,
        0x1FFE00, 0x1F3E00, 0x0E1C00, 0x0E1C00, 0x1F3E00, 0x1FFE00,
        0x1FFE00, 0x09E400, 0x01E000, 0x01E000, 0x000000, 0x000000,
    ];

    private static readonly uint[] Bullhorn =
    [
        0x000000, 0x000000, 0x000000, 0x000600, 0x000E00, 0x003E00,
        0x3FE600, 0x3E0700, 0x3E0700, 0x3E0700, 0x3FC600, 0x3FF600,
        0x0E3E00, 0x0E0E00, 0x0E0600, 0x0E0000, 0x000000, 0x000000,
    ];

    private static readonly uint[] Map =
    [
        0x000000, 0x000000, 0x370E00, 0xF7DE00, 0xF7DE00, 0xF7DE00,
        0xF7DE00, 0xF7DE00, 0xF7DE00, 0xF7DE00, 0xF7DE00, 0xF7DE00,
        0xF3DC00, 0x805000, 0x000000, 0x000000, 0x000000, 0x000000,
    ];

    private static readonly uint[] Home =
    [
        0x000000, 0x00C000, 0x01E000, 0x03F000, 0x0FFC00, 0x1FFE00,
        0x3FFF00, 0x7FFF80, 0x1FFE00, 0x1FFE00, 0x1E1E00, 0x1E1E00,
        0x1E1E00, 0x1E1E00, 0x000000, 0x000000, 0x000000, 0x000000,
    ];

    private static readonly uint[] Users =
    [
        0x000000, 0x380600, 0x7C0F00, 0x7C0F00, 0x39E700, 0x03E000,
        0x7BF780, 0xFBFF80, 0xFDEF80, 0x000000, 0x000000, 0x07F000,
        0x0FF800, 0x0FFC00, 0x0FFC00, 0x000000, 0x000000, 0x000000,
    ];

    private static readonly uint[] PaperPlane =
    [
        0x000000, 0x000600, 0x001E00, 0x007E00, 0x01FE00, 0x03DC00,
        0x0FBC00, 0x3F7C00, 0x7EFC00, 0x3CFC00, 0x01FC00, 0x03FC00,
        0x03F800, 0x031800, 0x020000, 0x000000, 0x000000, 0x000000,
    ];

    private static readonly uint[] Medal =
    [
        0x000000, 0x783E00, 0x3C3C00, 0x3C7800, 0x1C3000, 0x03D000,
        0x0FE000, 0x1EF000, 0x1E7000, 0x1C3800, 0x1C7000, 0x1D7000,
        0x0FF000, 0x07E000, 0x010000, 0x000000, 0x000000, 0x000000,
    ];

    private static readonly uint[] DoorOpen =
    [
        0x000000, 0x03C000, 0x1FC000, 0x1FFE00, 0x1FC600, 0x1FC600,
        0x1FC600, 0x1EC600, 0x1EC600, 0x1FC600, 0x1FC600, 0x1FC600,
        0x1FC600, 0x7FC780, 0x000000, 0x000000, 0x000000, 0x000000,
    ];

    private static readonly uint[] Ban =
    [
        0x000000, 0x000000, 0x07E000, 0x0FF000, 0x1C3800, 0x3C0C00,
        0x660E00, 0x630600, 0x638600, 0x61C600, 0x60E600, 0x707E00,
        0x303C00, 0x1C3800, 0x0FF000, 0x07E000, 0x000000, 0x000000,
    ];

    private GroundsideOperationsIcon _icon;

    public GroundsideOperationsIcon Icon
    {
        get => _icon;
        set => _icon = value;
    }

    public Color IconColor { get; set; } = Color.White;

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var rows = GetRows(_icon);
        var scale = MathF.Min(PixelSize.X, PixelSize.Y) / GlyphSize;
        if (scale <= 0)
            return;

        var left = (PixelSize.X - GlyphSize * scale) / 2f;
        var top = (PixelSize.Y - GlyphSize * scale) / 2f;

        for (var y = 0; y < rows.Length; y++)
        {
            var x = 0;
            while (x < GlyphSize)
            {
                if (!IsSet(rows[y], x))
                {
                    x++;
                    continue;
                }

                var start = x;
                while (x < GlyphSize && IsSet(rows[y], x))
                    x++;

                var position = new Vector2(left + start * scale, top + y * scale);
                var size = new Vector2((x - start) * scale, scale);
                handle.DrawRect(UIBox2.FromDimensions(position, size), IconColor);
            }
        }
    }

    private static bool IsSet(uint row, int x)
    {
        return (row & (1u << (RowBits - x - 1))) != 0;
    }

    private static uint[] GetRows(GroundsideOperationsIcon icon)
    {
        return icon switch
        {
            GroundsideOperationsIcon.Ban => Ban,
            GroundsideOperationsIcon.Bullhorn => Bullhorn,
            GroundsideOperationsIcon.Cog => Cog,
            GroundsideOperationsIcon.DoorOpen => DoorOpen,
            GroundsideOperationsIcon.Heartbeat => Heartbeat,
            GroundsideOperationsIcon.Home => Home,
            GroundsideOperationsIcon.IdCard => IdCard,
            GroundsideOperationsIcon.Map => Map,
            GroundsideOperationsIcon.Medal => Medal,
            GroundsideOperationsIcon.PaperPlane => PaperPlane,
            GroundsideOperationsIcon.Users => Users,
            GroundsideOperationsIcon.Warning => Warning,
            _ => Ban,
        };
    }
}
