using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using RtwoDtwo.IO.Compression;

namespace RtwoDtwo.IO.Compression.Tests
{
    public class StreamExtensionsTest
    {
        #region Tests

        [Theory]
        [InlineData(CompressionLevel.Optimal)]
        [InlineData(CompressionLevel.Fastest)]
        public async Task CompressParallel(CompressionLevel compressionLevel)
        {
            var data = GenerateData(1024, 1024);
            
            byte[] compressedData;

            using (var source = new MemoryStream(data))
            {
                using (var compressed = new MemoryStream())
                {
                    await source.CompressParallel(compressed, compressionLevel, 128 * 1024);

                    compressedData = compressed.ToArray();
                }
            }

            Console.WriteLine($"Source size = {data.Length} -> Compressed size = {compressedData.Length} ({compressionLevel})");

            Assert.True(compressedData.Length < data.Length);
        }

        [Fact]
        public async Task CompressParallel_NoCompression()
        {
            var data = GenerateData(1024, 1024);
            
            byte[] compressedData;

            using (var source = new MemoryStream(data))
            {
                using (var destination = new MemoryStream())
                {
                    await source.CompressParallel(destination, CompressionLevel.NoCompression, 128 * 1024);

                    compressedData = destination.ToArray();
                }
            }

            Console.WriteLine($"Source size = {data.Length} -> Compressed size = {compressedData.Length} ({CompressionLevel.NoCompression})");

            Assert.True(compressedData.Length > data.Length);
        }

        [Theory]
        [InlineData(CompressionLevel.Optimal)]
        [InlineData(CompressionLevel.Fastest)]
        [InlineData(CompressionLevel.NoCompression)]
        public async Task DecompressParallel(CompressionLevel compressionLevel)
        {
            var data = GenerateData(1024, 1024);
            
            byte[] decompressedData;

            using (var source = new MemoryStream(data))
            {
                using (var compressed = new MemoryStream())
                {
                    await source.CompressParallel(compressed, compressionLevel, 128 * 1024);

                    compressed.Seek(0, SeekOrigin.Begin);

                    using (var decompressed = new MemoryStream())
                    {
                        await compressed.DecompressParallel(decompressed);

                        decompressedData = decompressed.ToArray();
                    }
                }
            }

            Assert.True(data.SequenceEqual(decompressedData));
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