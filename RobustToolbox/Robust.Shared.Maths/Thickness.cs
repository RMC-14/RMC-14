﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths
{
    [PublicAPI]
    public struct Thickness : IEquatable<Thickness>, ISpanFormattable
    {
        public float Left;
        public float Top;
        public float Right;
        public float Bottom;

        public readonly float SumHorizontal => Left + Right;
        public readonly float SumVertical => Top + Bottom;

        public Thickness(float uniform)
        {
            Left = uniform;
            Top = uniform;
            Right = uniform;
            Bottom = uniform;
        }

        public Thickness(float horizontal, float vertical)
        {
            Left = horizontal;
            Right = horizontal;
            Top = vertical;
            Bottom = vertical;
        }

        public Thickness(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
        
        public Thickness Scale(float scale)
        {
            return new Thickness(Left * scale, Top * scale, Right * scale, Bottom * scale);
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public readonly bool Equals(Thickness other)
        {
            return Equals(in other);
        }

        public readonly UIBox2 Inflate(in UIBox2 box)
        {
            return new(
                box.Left - Left,
                box.Top - Top,
                box.Right + Right,
                box.Bottom + Bottom);
        }

        public readonly Vector2 Inflate(in Vector2 size)
        {
            return new(size.X + SumHorizontal, size.Y + SumVertical);
        }

        public readonly UIBox2 Deflate(in UIBox2 box)
        {
            var left = box.Left + Left;
            var top = box.Top + Top;
            return new(
                left,
                top,
                // Avoid inverse boxes if the margins are larger than the box.
                Math.Max(left, box.Right - Right),
                Math.Max(top, box.Bottom - Bottom));
        }

        public readonly Vector2 Deflate(in Vector2 size)
        {
            return Vector2.Max(
                Vector2.Zero,
                new(size.X - SumHorizontal, size.Y - SumVertical));
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public readonly bool Equals(in Thickness other)
        {
            return Left == other.Left &&
                   Top == other.Top &&
                   Right == other.Right &&
                   Bottom == other.Bottom;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Thickness other && Equals(other);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(Left, Top, Right, Bottom);
        }

        public static bool operator ==(in Thickness left, in Thickness right)
        {
            return left.Equals(in right);
        }

        public static bool operator !=(in Thickness left, in Thickness right)
        {
            return !left.Equals(in right);
        }

        public readonly override string ToString()
        {
            return $"{Left},{Top},{Right},{Bottom}";
        }


        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString();
        }

        public readonly bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            return FormatHelpers.TryFormatInto(
                destination,
                out charsWritten,
                $"{Left},{Top},{Right},{Bottom}");
        }

        public readonly void Deconstruct(out float left, out float top, out float right, out float bottom)
        {
            left = Left;
            top = Top;
            right = Right;
            bottom = Bottom;
        }
    }
}
