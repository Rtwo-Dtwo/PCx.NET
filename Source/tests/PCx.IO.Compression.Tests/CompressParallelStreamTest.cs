//using System;
//using System.IO;
//using System.IO.Compression;
//using System.Linq;

//using Xunit;

//namespace PCx.IO.Compression.Tests
//{
//	public static class CompressParallelStreamTest
//	{
//		#region Tests

//		[Fact]
//		public static async void CompressParallelStream_Validation()
//		{
//			var dataList = new[]
//			{
//				GenerateMockData(1024, 64),
//				GenerateMockData(1024, 128),
//				GenerateMockData(1024, 1120)
//			};

//			byte[] compressedData;
//			byte[] decompressedData;

//			using (var compressed = new MemoryStream())
//			{
//				using (var compressStream = new CompressParallelStream(compressed, CompressionLevel.Optimal, 128 * 1024))
//				{
//					foreach (var data in dataList)
//					{
//						compressStream.Write(data, 0, data.Length);
//					}
//				}

//				compressedData = compressed.ToArray();

//				compressed.Seek(0, SeekOrigin.Begin);

//				using (var decompressed = new MemoryStream())
//				{
//					await compressed.DecompressParallelToAsync(decompressed);

//					decompressedData = decompressed.ToArray();
//				}
//			}

//			Assert.True(compressedData.Length < dataList.Sum(data => data.Length));

//			Assert.True(dataList.SelectMany(data => data).SequenceEqual(decompressedData));
//		}

//		#endregion

//		#region Mocks

//		private static byte[] GenerateMockData(int size, int repeat)
//		{
//			var random = new Random();

//			var randomBuffer = new byte[size];
//			random.NextBytes(randomBuffer);

//			return Enumerable.Repeat(randomBuffer, repeat).SelectMany(buffer => buffer).ToArray();
//		}

//		#endregion
//	}
//}
