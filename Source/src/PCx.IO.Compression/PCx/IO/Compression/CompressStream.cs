using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace PCx.IO.Compression
{
	internal sealed class CompressStream : IDisposable
	{
		#region Fields

		private Stream _Stream;

		private readonly CompressionLevel _CompressionLevel;

		private CompressGraph _CompressGraph;

		private readonly byte[] _Buffer;
		private int _BufferPosition;

		#endregion

		#region Constructor

		public CompressStream(Stream stream, CompressionLevel compressionLevel, int bufferSize)
		{
			_Stream = stream;

			_CompressionLevel = compressionLevel;

			_Buffer = new byte[bufferSize];
		}

		#endregion

		#region Methods

		public void Write(byte[] buffer, int offset, int count)
		{
			InitializeGraph();

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

		private void InitializeGraph()
		{
			if (_CompressGraph == null)
			{
				Debug.Assert(_Stream != null);

				_CompressGraph = new CompressGraph(_Stream, _CompressionLevel);
			}
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
			if (_Stream != null)
			{
				try
				{
					if (_CompressGraph != null)
					{
						try
						{
							SendBuffer();

							_CompressGraph.CompleteAsync().Wait();
						}
						finally
						{
							_CompressGraph = null;
						}
					}
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