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
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PCx.IO.Compression
{
	internal sealed class CompressGraph
	{
		#region Fields

		private readonly ITargetBlock<Buffer> _TargetBlock;

		private readonly ISourceBlock<Buffer> _SourceBlock;

		private readonly Task _WriteStream;

		#endregion

		#region Constructor

		public CompressGraph(Stream stream, CompressionLevel compressionLevel)
			:this(stream, compressionLevel, new Progress<double>())
		{
		}

		public CompressGraph(Stream stream, CompressionLevel compressionLevel, IProgress<double> progress)
		{
			BuildGraph(compressionLevel, out _TargetBlock, out _SourceBlock);

			_WriteStream = WriteAsync(stream, progress);
		}

		#endregion

		#region Methods

		public Task SendAsync(Buffer buffer)
		{
			return _TargetBlock.SendAsync(buffer);
		}

		public async Task CompleteAsync()
		{
			_TargetBlock.Complete();

			await _SourceBlock.Completion;

			await _WriteStream;
		}

		private static void BuildGraph(CompressionLevel compressionLevel, out ITargetBlock<Buffer> targetBlock, out ISourceBlock<Buffer> sourceBlock)
		{
			int boundedCapacity = Environment.ProcessorCount * 2;

			var bufferBlock = new BufferBlock<Buffer>(new DataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity
			});

			var compressBlock = new TransformBlock<Buffer, Buffer>(buffer => Compress(buffer, compressionLevel), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				MaxDegreeOfParallelism = Environment.ProcessorCount,
				SingleProducerConstrained = true
			});

			bufferBlock.LinkTo(compressBlock, new DataflowLinkOptions()
			{
				PropagateCompletion = true
			});

			targetBlock = bufferBlock;
			sourceBlock = compressBlock;
		}

		private static Buffer Compress(Buffer buffer, CompressionLevel compressionLevel)
		{
			using (var destination = new MemoryStream())
			{
				using (var deflate = new DeflateStream(destination, compressionLevel, leaveOpen: true))
				{
					buffer.WriteTo(deflate);
				}

				return new Buffer(destination.ToArray(), buffer.Progress);
			}
		}

		private async Task WriteAsync(Stream stream, IProgress<double> progress)
		{
			while (await _SourceBlock.OutputAvailableAsync())
			{
				var buffer = _SourceBlock.Receive();

				Write(stream, buffer.Size);
				Write(stream, ~buffer.Size);

				buffer.WriteTo(stream, progress);
			}
		}

		private static void Write(Stream stream, int value)
		{
			var bytes = BitConverter.GetBytes(value);

			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}

			stream.Write(bytes, 0, bytes.Length);
		}

		#endregion
	}
}