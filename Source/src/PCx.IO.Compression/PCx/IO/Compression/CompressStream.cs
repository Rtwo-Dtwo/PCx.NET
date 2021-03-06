﻿// Copyright (c) 2017 Christian Winter <Christian.Winter81@me.com>
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
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace PCx.IO.Compression
{
	internal sealed class CompressStream : IDisposable
	{
		#region Fields

		private Stream _Stream;

		private readonly CompressionLevel _CompressionLevel;

		private readonly int _BufferSize;

		private CompressGraph _CompressGraph;

		private Buffer _Buffer;
		private int _BufferPosition;

		#endregion

		#region Constructor

		public CompressStream(Stream stream, CompressionLevel compressionLevel, int bufferSize)
		{
			_Stream = stream;

			_CompressionLevel = compressionLevel;

			_BufferSize = bufferSize;

			_Buffer = new Buffer(_BufferSize);
		}

		#endregion

		#region Methods

		public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			InitializeGraph();

			var writeCount = 0;

			while (true)
			{
				var requiredCount = count - writeCount;

				var remainingCount = _Buffer.Size - _BufferPosition;

				if (requiredCount < remainingCount)
				{
					System.Buffer.BlockCopy(buffer, offset + writeCount, _Buffer.Bytes, _BufferPosition, requiredCount);

					writeCount += requiredCount;

					_BufferPosition += requiredCount;

					break;
				}
				else
				{
					System.Buffer.BlockCopy(buffer, offset + writeCount, _Buffer.Bytes, _BufferPosition, remainingCount);

					writeCount += remainingCount;

					_BufferPosition += remainingCount;

					await SendBufferAsync(cancellationToken).ConfigureAwait(false);
				}
			}

			Debug.Assert(writeCount == count);
		}

		public async Task FlushAsync(CancellationToken cancellationToken)
		{
			if (_CompressGraph != null)
			{
				await SendBufferAsync(cancellationToken).ConfigureAwait(false);

				try
				{
					_CompressGraph.Complete();

					await _CompressGraph.Completion.ConfigureAwait(false);
				}
				finally
				{
					_CompressGraph = null;
				}
			}
		}

		private void InitializeGraph()
		{
			if (_CompressGraph == null)
			{
				Debug.Assert(_Stream != null);

				_CompressGraph = new CompressGraph(_Stream, _CompressionLevel);
			}
		}

		private async Task SendBufferAsync(CancellationToken cancellationToken)
		{
			if (_BufferPosition > 0)
			{
				if (_Buffer.Size == _BufferPosition)
				{
					await _CompressGraph.SendAsync(_Buffer, cancellationToken).ConfigureAwait(false);

					_Buffer = new Buffer(_BufferSize);
				}
				else
				{
					var buffer = new Buffer(_Buffer, _BufferPosition);

					await _CompressGraph.SendAsync(buffer, cancellationToken).ConfigureAwait(false);
				}

				_BufferPosition = 0;
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			if (_Stream != null)
			{
				try
				{
					FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
				}
				finally
				{
					_Stream = null;
				}
			}
		}

		#endregion
	}
}