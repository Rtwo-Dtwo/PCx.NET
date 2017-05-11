using System;
using System.Diagnostics.Contracts;
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
        public static async void CompressParallel_SizeDecreases(CompressionLevel compressionLevel)
        {
            var data = GenerateMockData(1024, 1088);
            
            byte[] compressedData;

            using (var source = new MemoryStream(data))
            {
                using (var compressed = new MemoryStream())
                {
                    await source.CompressParallelToAsync(compressed, compressionLevel, 128 * 1024);

                    compressedData = compressed.ToArray();
                }
            }

            Console.WriteLine($"Source size = {data.Length} -> Compressed size = {compressedData.Length} ({compressionLevel})");

            Assert.True(compressedData.Length < data.Length);
        }

        [Fact]
        public static async void CompressParallel_NoCompression_SizeIncreases()
        {
            var data = GenerateMockData(1024, 1088);
            
            byte[] compressedData;

            using (var source = new MemoryStream(data))
            {
                using (var destination = new MemoryStream())
                {
                    await source.CompressParallelToAsync(destination, CompressionLevel.NoCompression, 128 * 1024);

                    compressedData = destination.ToArray();
                }
            }

            Console.WriteLine($"Source size = {data.Length} -> Compressed size = {compressedData.Length} ({CompressionLevel.NoCompression})");

            Assert.True(compressedData.Length > data.Length);
        }

        [Fact]
        public static async void CompressParallel_ReportsProgress()
        {
            const int bufferSize = 128 * 1024;

            var data = GenerateMockData(1024, 1088);

            var progress = new MockProgress();

            using (var source = new MemoryStream(data))
            {
                using (var destination = new MemoryStream())
                {
                    await source.CompressParallelToAsync(destination, CompressionLevel.Optimal, bufferSize, progress);
                }
            }

            var count = (int)Math.Ceiling(data.Length / (double)bufferSize);
            
            ++count; // Required for: Ensuring completed progress on unseekable streams

            progress.Assert(count);
        }

        [Fact]
        public static async void CompressParallel_ArgumentValidation()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => StreamExtensions.CompressParallelToAsync(null, new MemoryStream(), CompressionLevel.NoCompression, 1, new Progress<double>()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => StreamExtensions.CompressParallelToAsync(new MemoryStream(), null, CompressionLevel.NoCompression, 1, new Progress<double>()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => StreamExtensions.CompressParallelToAsync(new MemoryStream(), new MemoryStream(), CompressionLevel.NoCompression, 1, null));

            var closedStream = new MemoryStream();
            closedStream.Dispose();

            await Assert.ThrowsAsync<NotSupportedException>(() => StreamExtensions.CompressParallelToAsync(closedStream, new MemoryStream(), CompressionLevel.NoCompression, 1, new Progress<double>()));
            await Assert.ThrowsAsync<NotSupportedException>(() => StreamExtensions.CompressParallelToAsync(new MemoryStream(), closedStream, CompressionLevel.NoCompression, 1, new Progress<double>()));

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => StreamExtensions.CompressParallelToAsync(new MemoryStream(), new MemoryStream(), CompressionLevel.NoCompression, -73, new Progress<double>()));
        }

        [Theory]
        [InlineData(CompressionLevel.Optimal)]
        [InlineData(CompressionLevel.Fastest)]
        [InlineData(CompressionLevel.NoCompression)]
        public static async void DecompressParallel_EqualsSourceData(CompressionLevel compressionLevel)
        {
            var data = GenerateMockData(1024, 1088);
            
            byte[] decompressedData;

            using (var source = new MemoryStream(data))
            {
                using (var compressed = new MemoryStream())
                {
                    await source.CompressParallelToAsync(compressed, compressionLevel, 128 * 1024);

                    compressed.Seek(0, SeekOrigin.Begin);

                    using (var decompressed = new MemoryStream())
                    {
                        await compressed.DecompressParallelToAsync(decompressed);

                        decompressedData = decompressed.ToArray();
                    }
                }
            }

            Assert.True(data.SequenceEqual(decompressedData));
        }

        [Fact]
        public static async void DecompressParallel_ReportsProgress()
        {
            const int bufferSize = 128 * 1024;

            var data = GenerateMockData(1024, 1088);
            
            var progress = new MockProgress();

            using (var source = new MemoryStream(data))
            {
                using (var compressed = new MemoryStream())
                {
                    await source.CompressParallelToAsync(compressed, CompressionLevel.Optimal, bufferSize);

                    compressed.Seek(0, SeekOrigin.Begin);

                    using (var decompressed = new MemoryStream())
                    {
                        await compressed.DecompressParallelToAsync(decompressed, progress);
                    }
                }
            }

            var count = (int)Math.Ceiling(data.Length / (double)bufferSize);
            
            ++count; // Required for: Ensuring completed progress on unseekable streams

            progress.Assert(count);
        }

        [Fact]
        public static async void DecompressParallel_ArgumentValidation()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => StreamExtensions.DecompressParallelToAsync(null, new MemoryStream(), new Progress<double>()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => StreamExtensions.DecompressParallelToAsync(new MemoryStream(), null, new Progress<double>()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => StreamExtensions.DecompressParallelToAsync(new MemoryStream(), new MemoryStream(), null));

            var closedStream = new MemoryStream();
            closedStream.Dispose();

            await Assert.ThrowsAsync<NotSupportedException>(() => StreamExtensions.DecompressParallelToAsync(closedStream, new MemoryStream(), new Progress<double>()));
            await Assert.ThrowsAsync<NotSupportedException>(() => StreamExtensions.DecompressParallelToAsync(new MemoryStream(), closedStream, new Progress<double>()));
        }

        #endregion

        #region Mocks

        private static byte[] GenerateMockData(int size, int repeat)
        {
            var random = new Random();

            var randomBuffer = new byte[size];
            random.NextBytes(randomBuffer);

            return Enumerable.Repeat(randomBuffer, repeat).SelectMany(buffer => buffer).ToArray();
        }

        private sealed class MockProgress : IProgress<double>
        {
            #region Fields

            private double _Value = 0.0;

            private int _Count = 0;

            #endregion

            #region Methods

            public void Assert(int count)
            {
                Xunit.Assert.Equal(1.0, _Value);

                Xunit.Assert.Equal(count, _Count);
            }

            #endregion

            #region IProgress Members

            void IProgress<double>.Report(double value)
            {
                #region Contracts
                
                if (value == 0)
                {
                    throw new ArgumentException("value is zero", "value");
                }

                if (value < _Value)
                {
                    throw new ArgumentException("value is less than current value", "value");
                }

                Contract.EndContractBlock();

                #endregion

                _Value = value;

                ++_Count;
            }

            #endregion
        }

        #endregion
    }
}