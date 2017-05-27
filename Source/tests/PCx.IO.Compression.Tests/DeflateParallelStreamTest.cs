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
using System.IO;
using System.IO.Compression;
using System.Linq;

using Xunit;

namespace PCx.IO.Compression.Tests
{
	public static class DeflateParallelStreamTest
	{
		#region Tests

		[Fact]
		public static void CompressParallelStream_Validation()
		{
			var dataList = new[]
			{
				GenerateMockData(1024, 64),
				GenerateMockData(1024, 128),
				GenerateMockData(1024, 1120)
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

		#endregion

		#region Mocks

		private static byte[] GenerateMockData(int size, int repeat)
		{
			var random = new Random();

			var randomBuffer = new byte[size];
			random.NextBytes(randomBuffer);

			return Enumerable.Repeat(randomBuffer, repeat).SelectMany(buffer => buffer).ToArray();
		}

		#endregion
	}
}
