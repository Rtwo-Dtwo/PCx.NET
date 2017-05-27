// Copyright (c) 2017 Christian Winter <Christian.Winter81@me.com>
//
// This file is part of PCx.NET.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using static PCx.IO.Compression.Tests.Mock;

namespace PCx.IO.Compression.Tests
{
	public static class StreamExtensionsTest
	{
		#region Tests

		[Theory]
		[InlineData(CompressionLevel.Optimal)]
		[InlineData(CompressionLevel.Fastest)]
		public static async Task CompressParallel_SizeDecreases(CompressionLevel compressionLevel)
		{
			var data = GenerateData(1024, 1088);

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
		public static async Task CompressParallel_NoCompression_SizeIncreases()
		{
			var data = GenerateData(1024, 1088);

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
		public static async Task CompressParallel_ReportsProgress()
		{
			const int bufferSize = 128 * 1024;

			var data = GenerateData(1024, 1088);

			var progress = new Progress();

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
		public static async Task CompressParallel_ArgumentValidation()
		{
			await Assert.ThrowsAsync<ArgumentNullException>(() => StreamExtensions.CompressParallelToAsync(null, Stream.Null, CompressionLevel.NoCompression, bufferSize: 1, progress: new Progress<double>()));
			await Assert.ThrowsAsync<ArgumentNullException>(() => Stream.Null.CompressParallelToAsync(null, CompressionLevel.NoCompression, bufferSize: 1, progress: new Progress<double>()));
			await Assert.ThrowsAsync<ArgumentNullException>(() => Stream.Null.CompressParallelToAsync(Stream.Null, CompressionLevel.NoCompression, bufferSize: 1, progress: null));

			await Assert.ThrowsAsync<NotSupportedException>(() => ClosedStream.CompressParallelToAsync(Stream.Null, CompressionLevel.NoCompression, bufferSize: 1, progress: new Progress<double>()));
			await Assert.ThrowsAsync<NotSupportedException>(() => Stream.Null.CompressParallelToAsync(ClosedStream, CompressionLevel.NoCompression, bufferSize: 1, progress: new Progress<double>()));

			await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Stream.Null.CompressParallelToAsync(Stream.Null, CompressionLevel.NoCompression, bufferSize: -1, progress: new Progress<double>()));
		}

		[Theory]
		[InlineData(CompressionLevel.Optimal)]
		[InlineData(CompressionLevel.Fastest)]
		[InlineData(CompressionLevel.NoCompression)]
		public static async Task DecompressParallel_EqualsSourceData(CompressionLevel compressionLevel)
		{
			var data = GenerateData(1024, 1088);

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
		public static async Task DecompressParallel_ReportsProgress()
		{
			const int bufferSize = 128 * 1024;

			var data = GenerateData(1024, 1088);

			var progress = new Progress();

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
		public static async Task DecompressParallel_ArgumentValidation()
		{
			await Assert.ThrowsAsync<ArgumentNullException>(() => StreamExtensions.DecompressParallelToAsync(null, Stream.Null, new Progress<double>()));
			await Assert.ThrowsAsync<ArgumentNullException>(() => Stream.Null.DecompressParallelToAsync(null, new Progress<double>()));
			await Assert.ThrowsAsync<ArgumentNullException>(() => Stream.Null.DecompressParallelToAsync(Stream.Null, progress: null));

			await Assert.ThrowsAsync<NotSupportedException>(() => ClosedStream.DecompressParallelToAsync(Stream.Null, new Progress<double>()));
			await Assert.ThrowsAsync<NotSupportedException>(() => Stream.Null.DecompressParallelToAsync(ClosedStream, new Progress<double>()));
		}

		#endregion
	}
}