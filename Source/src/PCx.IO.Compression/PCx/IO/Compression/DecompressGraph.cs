// Copyright (c) 2017 Christian Winter <Christian.Winter81@me.com>
//
// This file is part of PCx.NET <https://github.com/Rtwo-Dtwo/PCx.NET>.
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
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PCx.IO.Compression
{
	internal sealed class DecompressGraph
	{
		#region Fields

		private readonly ITargetBlock<Buffer> _TargetBlock;

		private readonly ISourceBlock<Buffer> _SourceBlock;

		private readonly CancellationTokenSource _Cancellation = new CancellationTokenSource();

		private readonly Task _ReadStream;

		#endregion

		#region Constructor

		public DecompressGraph(Stream stream)
		{
			BuildGraph(out _TargetBlock, out _SourceBlock);

			_ReadStream = ReadAsync(stream, _Cancellation.Token);
		}

		#endregion

		#region Properties

		public Task Completion
		{
			get
			{
				return Task.WhenAll(_ReadStream, FlushAsync(), _SourceBlock.Completion);
			}
		}

		#endregion

		#region Methods

		public Task<bool> OutputAvailableAsync(CancellationToken cancellationToken)
		{
			return _SourceBlock.OutputAvailableAsync(cancellationToken);
		}

		public Task<Buffer> ReceiveAsync(CancellationToken cancellationToken)
		{
			return _SourceBlock.ReceiveAsync(cancellationToken);
		}

		public void Complete()
		{
			_Cancellation.Cancel();
			_Cancellation.Dispose();
		}

		private async Task FlushAsync()
		{
			while (await _SourceBlock.OutputAvailableAsync().ConfigureAwait(false))
			{
				await _SourceBlock.ReceiveAsync().ConfigureAwait(false);
			}
		}

		private static void BuildGraph(out ITargetBlock<Buffer> targetBlock, out ISourceBlock<Buffer> sourceBlock)
		{
			int boundedCapacity = Environment.ProcessorCount * 8;

			var decompressBlock = new TransformBlock<Buffer, Buffer>(buffer => Decompress(buffer), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				MaxDegreeOfParallelism = Environment.ProcessorCount,
				SingleProducerConstrained = true
			});

			targetBlock = decompressBlock;
			sourceBlock = decompressBlock;
		}

		private static Buffer Decompress(Buffer buffer)
		{
			using (var deflate = new DeflateStream(buffer.ToStream(), CompressionMode.Decompress, leaveOpen: false))
			{
				using (var destination = new MemoryStream())
				{
					deflate.CopyTo(destination);

					return new Buffer(destination.ToArray(), buffer.Progress);
				}
			}
		}

		private async Task ReadAsync(Stream stream, CancellationToken cancellationToken)
		{
			try
			{
				while (true)
				{
					int length;

					if (!ReadHeader(stream, out length))
					{
						break;
					}
					
					var buffer = Buffer.ReadFrom(stream, length);

					if (buffer.Size != length)
					{
						throw new IOException("Source stream is not well-formed");
					}

					await _TargetBlock.SendAsync(buffer, cancellationToken).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{
			}
			finally
			{
				_TargetBlock.Complete();
			}
		}

		private static bool ReadHeader(Stream stream, out int length)
		{
			var bytes = new byte[sizeof(int) * 2];

			if (stream.Read(bytes, 0, bytes.Length) == bytes.Length)
			{
				if (!BitConverter.IsLittleEndian)
				{
					Array.Reverse(bytes, 0, sizeof(int));
					Array.Reverse(bytes, sizeof(int), sizeof(int));
				}

				length = BitConverter.ToInt32(bytes, 0);

				var complementLength = BitConverter.ToInt32(bytes, sizeof(int));

				if (~length != complementLength)
				{
					throw new IOException("Source stream is not well-formed");
				}

				return true;
			}

			length = default(int);

			return false;
		}

		#endregion
	}
}