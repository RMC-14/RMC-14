﻿using System;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Shared.Utility;

// Definitely a "kick the can down the road" for not moving these immediately to shared to avoid a breaking change.
internal static class ImageOps
{
    /// <summary>
    ///     Blit an image into another, with the specified offset.
    /// </summary>
    /// <param name="source">The image to copy data from.</param>
    /// <param name="sourceRect">The sub section of <see cref="source"/> that will be copied.</param>
    /// <param name="destinationOffset">
    ///     The offset into <see cref="destination"/> that data will be copied into.
    /// </param>
    /// <param name="destination">The image to copy to.</param>
    /// <typeparam name="T">The type of pixel stored in the images.</typeparam>
    public static void Blit<T>(Image<T> source, UIBox2i sourceRect,
        Image<T> destination, Vector2i destinationOffset)
        where T : unmanaged, IPixel<T>
    {
        // TODO: Bounds checks.

        Blit(GetPixelSpan(source), source.Width, sourceRect, destination, destinationOffset);
    }

    public static void Blit<T>(ReadOnlySpan<T> source, int sourceWidth, UIBox2i sourceRect,
        Image<T> destination, Vector2i destinationOffset) where T : unmanaged, IPixel<T>
    {
        var dstSpan = GetPixelSpan(destination);
        var dstWidth = destination.Width;
        var srcHeight = sourceRect.Height;
        var srcWidth = sourceRect.Width;

        var (ox, oy) = destinationOffset;

        for (var y = 0; y < srcHeight; y++)
        {
            var sourceRowOffset = sourceWidth * (y + sourceRect.Top) + sourceRect.Left;
            var destRowOffset = dstWidth * (y + oy) + ox;

            var srcRow = source[sourceRowOffset..(sourceRowOffset + srcWidth)];
            var dstRow = dstSpan[destRowOffset..(destRowOffset + srcWidth)];

            srcRow.CopyTo(dstRow);
        }
    }

    /// <summary>
    /// Gets a <see cref="T:System.Span`1" /> to the backing data if the backing group consists of a single contiguous memory buffer.
    /// </summary>
    /// <returns>The <see cref="T:System.Span`1" /> referencing the memory area.</returns>
    /// <exception cref="ArgumentException">Thrown if the image is not a single contiguous buffer.</exception>
    public static Span<T> GetPixelSpan<T>(Image<T> image) where T : unmanaged, IPixel<T>
    {
        if (!image.DangerousTryGetSinglePixelMemory(out var memory))
            throw new ArgumentException("Image is not backed by a single buffer, cannot fetch span.");

        return memory.Span;
    }
}
