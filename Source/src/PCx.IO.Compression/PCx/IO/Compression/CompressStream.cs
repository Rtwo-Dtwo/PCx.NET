using System;
using System.IO;
using System.IO.Compression;

namespace PCx.IO.Compression
{
	internal sealed class CompressStream : IDisposable
	{
		#region Fields

		private readonly CompressGraph _CompressGraph;

		private readonly byte[] _Buffer;
		private int _BufferPosition;

		#endregion

		#region Constructor

		public CompressStream(Stream stream, CompressionLevel compressionLevel, int bufferSize)
		{
			_CompressGraph = new CompressGraph(stream, compressionLevel);

			_Buffer = new byte[bufferSize];
		}

		#endregion

		#region Methods

		public void Write(byte[] buffer, int offset, int count)
		{
			var bufferPosition = 0;

			while (true)
			{
				if (_BufferPosition + (count - bufferPosition) >= _Buffer.Length)
				{
					var copyCount = _Buffer.Length - _BufferPosition;
					System.Buffer.BlockCopy(buffer, offset + bufferPosition, _Buffer, _BufferPosition, copyCount);

					_BufferPosition += copyCount;
					bufferPosition += copyCount;

					SendBuffer();
				}
				else
				{
					var copyCount = count - bufferPosition;
					System.Buffer.BlockCopy(buffer, offset + bufferPosition, _Buffer, _BufferPosition, copyCount);

					_BufferPosition += copyCount;

					break;
				}
			}
		}

		public void Flush()
		{
			SendBuffer();
		}

		private void SendBuffer()
		{
			if (_BufferPosition > 0)
			{
				var bytes = new byte[_BufferPosition];
				System.Buffer.BlockCopy(_Buffer, 0, bytes, 0, _BufferPosition);

				_CompressGraph.SendAsync(new Buffer(bytes)).Wait();

				_BufferPosition = 0;
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			SendBuffer();

			_CompressGraph.CompleteAsync().Wait();
		}

		#endregion
	}
}