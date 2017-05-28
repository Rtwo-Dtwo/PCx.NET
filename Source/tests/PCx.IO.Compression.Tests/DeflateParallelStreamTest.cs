﻿// Copyright (c) 2017 Christian Winter <Christian.Winter81@me.com>
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
using System.IO;
using System.IO.Compression;
using System.Linq;

using Xunit;

using static PCx.IO.Compression.Tests.Mock;

namespace PCx.IO.Compression.Tests
{
	public static class DeflateParallelStreamTest
	{
		#region Tests

		[Fact]
		public static void DeflateParallelStream_Validation()
		{
			var dataList = new[]
			{
				GenerateData(1024, 64),
				GenerateData(1024, 128),
				GenerateData(1024, 1120)
			};

			byte[] compressedData;
			byte[] decompressedData;

			using (var compressed = new MemoryStream())
			{
				using (var compressStream = new DeflateParallelStream(compressed, CompressionLevel.Optimal, 128 * 1024, leaveOpen: true))
				{
					foreach (var data in dataList)
					{
						compressStream.Write(data, 0, data.Length);
					}
				}

				compressedData = compressed.ToArray();

				compressed.Seek(0, SeekOrigin.Begin);

				using (var decompressStream = new DeflateParallelStream(compressed, CompressionMode.Decompress, leaveOpen: true))
				{
					using (var decompressed = new MemoryStream())
					{
						decompressStream.CopyTo(decompressed);

						decompressedData = decompressed.ToArray();
					}
				}
			}

			Assert.True(compressedData.Length < dataList.Sum(data => data.Length));

			Assert.True(dataList.SelectMany(data => data).SequenceEqual(decompressedData));
		}

		[Fact]
		public static void DeflateParallelStream_CompressMode_NoOperation_StreamUntouched()
		{
			using (var stream = new MemoryStream())
			{
				using (var compressStream = new DeflateParallelStream(stream, CompressionMode.Compress, leaveOpen: true))
				{
					// No Operation
				}

				Assert.Equal(0, stream.Position);
				Assert.Equal(0, stream.Length);
			}
		}

		[Fact]
		public static void DeflateParallelStream_DecompressMode_NoOperation_StreamUntouched()
		{
			using (var stream = new MemoryStream())
			{
				using (var compressStream = new DeflateParallelStream(stream, CompressionMode.Compress, leaveOpen: true))
				{
					var data = GenerateData(1024, 1024);

					compressStream.Write(data, 0, data.Length);
				}

				stream.Seek(0, SeekOrigin.Begin);

				using (var decompressSTream = new DeflateParallelStream(stream, CompressionMode.Decompress, leaveOpen: true))
				{
					// No Operation
				}

				Assert.Equal(0, stream.Position);
			}
		}

		[Fact]
		public static void DeflateParallelStream_Constructor_ArgumentValidation()
		{
			Assert.Throws<ArgumentNullException>(() => new DeflateParallelStream(null, CompressionMode.Compress, leaveOpen: true));
			Assert.Throws<NotSupportedException>(() => new DeflateParallelStream(ClosedStream, CompressionMode.Compress, leaveOpen: true));
			Assert.Throws<NotSupportedException>(() => new DeflateParallelStream(ClosedStream, CompressionMode.Decompress, leaveOpen: true));

			Assert.Throws<ArgumentNullException>(() => new DeflateParallelStream(null, CompressionLevel.NoCompression, bufferSize: 1, leaveOpen: true));
			Assert.Throws<NotSupportedException>(() => new DeflateParallelStream(ClosedStream, CompressionLevel.NoCompression, bufferSize: 1, leaveOpen: true));
			
			Assert.Throws<ArgumentOutOfRangeException>(() => new DeflateParallelStream(Stream.Null, CompressionLevel.NoCompression, bufferSize: -1, leaveOpen: true));
		}

		[Fact]
		public static void DeflateParallelStream_CompressMode_ArgumentValidation()
		{
			DeflateParallelStream stream;

			using (stream = new DeflateParallelStream(new MemoryStream(), CompressionMode.Compress))
			{
				Assert.Throws<ArgumentNullException>(() => stream.Write(buffer: null, offset: 0, count: 0));
				Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(buffer: new byte[0], offset: -1, count: 0));
				Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(buffer: new byte[0], offset: 0, count: -1));
				Assert.Throws<ArgumentException>(() => stream.Write(buffer: new byte[0], offset: 0, count: 1));
				
				Assert.Throws<InvalidOperationException>(() => stream.Read(new byte[0], 0, 0));

				Assert.Throws<NotSupportedException>(() => stream.Length);
				Assert.Throws<NotSupportedException>(() => stream.Position);
				Assert.Throws<NotSupportedException>(() => stream.Position = 0);

				Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
				Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
			}

			Assert.Throws<ObjectDisposedException>(() => stream.Write(new byte[0], 0, 0));
			Assert.Throws<ObjectDisposedException>(() => stream.Flush());
		}

		[Fact]
		public static void DeflateParallelStream_DecompressMode_ArgumentValidation()
		{
			DeflateParallelStream stream;

			using (stream = new DeflateParallelStream(new MemoryStream(), CompressionMode.Decompress))
			{
				Assert.Throws<ArgumentNullException>(() => stream.Read(buffer: null, offset: 0, count: 0));
				Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(buffer: new byte[0], offset: -1, count: 0));
				Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(buffer: new byte[0], offset: 0, count: -1));
				Assert.Throws<ArgumentException>(() => stream.Read(buffer: new byte[0], offset: 0, count: 1));
				
				Assert.Throws<InvalidOperationException>(() => stream.Write(new byte[0], 0, 0));

				Assert.Throws<NotSupportedException>(() => stream.Length);
				Assert.Throws<NotSupportedException>(() => stream.Position);
				Assert.Throws<NotSupportedException>(() => stream.Position = 0);

				Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
				Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
			}

			Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[0], 0, 0));
			Assert.Throws<ObjectDisposedException>(() => stream.Flush());
		}

		#endregion
	}
}