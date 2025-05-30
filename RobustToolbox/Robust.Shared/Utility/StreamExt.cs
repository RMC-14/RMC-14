using System;
using System.IO;

namespace Robust.Shared.Utility
{
    /// <summary>
    ///     Extension methods for working with streams.
    /// </summary>
    public static class StreamExt
    {
        /// <summary>
        ///     Copies any stream into a byte array.
        /// </summary>
        /// <param name="stream">The stream to copy.</param>
        /// <returns>The byte array.</returns>
        public static byte[] CopyToArray(this Stream stream)
        {
            using (var memStream = new MemoryStream())
            {
                stream.CopyTo(memStream);
                return memStream.ToArray();
            }
        }

        /// <summary>
        /// <see cref="CopyToArray"/> but to the Pinned Object Heap.
        /// </summary>
        internal static byte[] CopyToPinnedArray(this Stream stream)
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);

            var count = (int)ms.Length;
            var array = GC.AllocateUninitializedArray<byte>(count, pinned: true);
            ms.GetBuffer().AsSpan(0, count).CopyTo(array);

            return array;
        }

        internal static MemoryStream ConsumeToMemoryStream(this Stream stream)
        {
            var ms = stream.CopyToMemoryStream();
            stream.Dispose();
            return ms;
        }

        internal static MemoryStream CopyToMemoryStream(this Stream stream)
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        /// <exception cref="EndOfStreamException">
        /// Thrown if not exactly <paramref name="amount"/> bytes could be read.
        /// </exception>
        public static byte[] ReadExact(this Stream stream, int amount)
        {
            var buffer = new byte[amount];
            var read = 0;
            while (read < amount)
            {
                var cRead = stream.Read(buffer, read, amount - read);
                if (cRead == 0)
                {
                    throw new EndOfStreamException();
                }

                read += cRead;
            }

            return buffer;
        }

        /// <exception cref="EndOfStreamException">
        /// Thrown if not exactly <paramref name="buffer.Length"/> bytes could be read.
        /// </exception>
        public static void ReadExact(this Stream stream, Span<byte> buffer)
        {
            while (buffer.Length > 0)
            {
                var cRead = stream.Read(buffer);
                if (cRead == 0)
                    throw new EndOfStreamException();

                buffer = buffer[cRead..];
            }
        }

        public static int ReadToEnd(this Stream stream, Span<byte> buffer)
        {
            var totalRead = 0;
            while (true)
            {
                var read = stream.Read(buffer);
                totalRead += read;
                if (read == 0)
                    return totalRead;

                buffer = buffer[read..];
            }
        }

        public static int ReadToEnd(this Stream stream, byte[] buffer)
        {
            var totalRead = 0;
            while (true)
            {
                var read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
                totalRead += read;
                if (read == 0)
                    return totalRead;
            }
        }

        /// <summary>
        /// Gets the span over the currently filled region of the memory stream, based on its length.
        /// </summary>
        public static Span<byte> AsSpan(this MemoryStream ms)
        {
            // Let it be forever immortalized that, while I was writing this function,
            // Julian suggested that I should name it "AssSpan" to test if that would slip through review.

            var buf = ms.GetBuffer();

            return buf.AsSpan(0, (int) ms.Length);
        }

        /// <summary>
        /// Gets the memory over the currently filled region of the memory stream, based on its length.
        /// </summary>
        public static Memory<byte> AsMemory(this MemoryStream ms)
        {
            var buf = ms.GetBuffer();

            return buf.AsMemory(0, (int) ms.Length);
        }
    }
}
