using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace RtwoDtwo.IO.Compression.Tests
{
	public static class StreamExtensionsTest
    {
        #region Tests

        [Theory]
        [InlineData(CompressionLevel.Optimal)]
        [InlineData(CompressionLevel.Fastest)]
        public static async void CompressParallel(CompressionLevel compressionLevel)
        {
            var data = GenerateData(1024, 1024);
            
            byte[] compressedData;

            using (var source = new MemoryStream(data))
            {
                using (var compressed = new MemoryStream())
                {
                    await source.CompressParallelAsync(compressed, compressionLevel, 128 * 1024);

                    compressedData = compressed.ToArray();
                }
            }

            Console.WriteLine($"Source size = {data.Length} -> Compressed size = {compressedData.Length} ({compressionLevel})");

            Assert.True(compressedData.Length < data.Length);
        }

        [Fact]
        public static async void CompressParallel_NoCompression()
        {
            var data = GenerateData(1024, 1024);
            
            byte[] compressedData;

            using (var source = new MemoryStream(data))
            {
                using (var destination = new MemoryStream())
                {
                    await source.CompressParallelAsync(destination, CompressionLevel.NoCompression, 128 * 1024);

                    compressedData = destination.ToArray();
                }
            }

            Console.WriteLine($"Source size = {data.Length} -> Compressed size = {compressedData.Length} ({CompressionLevel.NoCompression})");

            Assert.True(compressedData.Length > data.Length);
        }

        [Fact]
        public static async void CompressParallel_ArgumentValidation()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => StreamExtensions.CompressParallelAsync(null, new MemoryStream(), CompressionLevel.NoCompression, 1));
            await Assert.ThrowsAsync<ArgumentNullException>(() => StreamExtensions.CompressParallelAsync(new MemoryStream(), null, CompressionLevel.NoCompression, 1));

            var closedStream = new MemoryStream();
            closedStream.Dispose();

            await Assert.ThrowsAsync<NotSupportedException>(() => StreamExtensions.CompressParallelAsync(closedStream, new MemoryStream(), CompressionLevel.NoCompression, 1));
            await Assert.ThrowsAsync<NotSupportedException>(() => StreamExtensions.CompressParallelAsync(new MemoryStream(), closedStream, CompressionLevel.NoCompression, 1));

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => StreamExtensions.CompressParallelAsync(new MemoryStream(), new MemoryStream(), CompressionLevel.NoCompression, -73));
        }

        [Theory]
        [InlineData(CompressionLevel.Optimal)]
        [InlineData(CompressionLevel.Fastest)]
        [InlineData(CompressionLevel.NoCompression)]
        public static async void DecompressParallel(CompressionLevel compressionLevel)
        {
            var data = GenerateData(1024, 1024);
            
            byte[] decompressedData;

            using (var source = new MemoryStream(data))
            {
                using (var compressed = new MemoryStream())
                {
                    await source.CompressParallelAsync(compressed, compressionLevel, 128 * 1024);

                    compressed.Seek(0, SeekOrigin.Begin);

                    using (var decompressed = new MemoryStream())
                    {
                        await compressed.DecompressParallelAsync(decompressed);

                        decompressedData = decompressed.ToArray();
                    }
                }
            }

            Assert.True(data.SequenceEqual(decompressedData));
        }

        [Fact]
        public static async void DecompressParallel_ArgumentValidation()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => StreamExtensions.DecompressParallelAsync(null, new MemoryStream()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => StreamExtensions.DecompressParallelAsync(new MemoryStream(), null));

            var closedStream = new MemoryStream();
            closedStream.Dispose();

            await Assert.ThrowsAsync<NotSupportedException>(() => StreamExtensions.DecompressParallelAsync(closedStream, new MemoryStream()));
            await Assert.ThrowsAsync<NotSupportedException>(() => StreamExtensions.DecompressParallelAsync(new MemoryStream(), closedStream));
        }

        #endregion

        #region Methods

        private static byte[] GenerateData(int size, int repeat)
        {
            var random = new Random();

            var randomBuffer = new byte[size];
            random.NextBytes(randomBuffer);

            return Enumerable.Repeat(randomBuffer, repeat).SelectMany(buffer => buffer).ToArray();
        }

        #endregion
    }
}