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

		#region Methods

		public Task<bool> OutputAvailableAsync()
		{
			return _SourceBlock.OutputAvailableAsync();
		}

		public Task<Buffer> ReceiveAsync()
		{
			return _SourceBlock.ReceiveAsync();
		}

		public async Task CompleteAsync()
		{
			_Cancellation.Cancel();

			await _ReadStream.ConfigureAwait(false);

			await FlushAsync().ConfigureAwait(false);

			await _SourceBlock.Completion.ConfigureAwait(false);

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
			int boundedCapacity = Environment.ProcessorCount * 2;

			var bufferBlock = new BufferBlock<Buffer>(new DataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity
			});

			var decompressBlock = new TransformBlock<Buffer, Buffer>(buffer => Decompress(buffer), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				MaxDegreeOfParallelism = Environment.ProcessorCount,
				SingleProducerConstrained = true
			});

			bufferBlock.LinkTo(decompressBlock, new DataflowLinkOptions()
			{
				PropagateCompletion = true
			});

			targetBlock = bufferBlock;
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
					var length = await ReadInt32Async(stream).ConfigureAwait(false);

					if (!length.HasValue)
					{
						break;
					}

					cancellationToken.ThrowIfCancellationRequested();

					var complementLength = await ReadInt32Async(stream).ConfigureAwait(false);

					if (!complementLength.HasValue || (~length.Value != complementLength.Value))
					{
						throw new IOException("Source stream is not well-formed");
					}
					
					cancellationToken.ThrowIfCancellationRequested();
					
					var buffer = await Buffer.ReadFromAsync(stream, length.Value).ConfigureAwait(false);

					if (buffer.Size != length.Value)
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

		private static async Task<int?> ReadInt32Async(Stream stream)
		{
			var bytes = new byte[4];

			if (await stream.ReadAsync(bytes, 0, bytes.Length).ConfigureAwait(false) == bytes.Length)
			{
				if (!BitConverter.IsLittleEndian)
				{
					Array.Reverse(bytes);
				}

				return BitConverter.ToInt32(bytes, 0);
			}

			return null;
		}

		#endregion
	}
}