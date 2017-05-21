using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace PCx.IO.Compression
{
	internal sealed class CompressStream : IDisposable
	{
		#region Fields

		private readonly CompressGraph _CompressGraph;

		private readonly Task _WriteStream;

		private readonly byte[] _Buffer;
		private int _BufferPosition;

		#endregion

		#region Constructor

		public CompressStream(Stream stream, CompressionLevel compressionLevel, int bufferSize)
		{
			_CompressGraph = new CompressGraph(compressionLevel);

			_WriteStream = WriteStreamAsync(stream);

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

		private async Task WriteStreamAsync(Stream stream)
		{
			while (await _CompressGraph.OutputAvailableAsync())
			{
				var buffer = _CompressGraph.Receive();

				Write(stream, buffer.Size);
				Write(stream, ~buffer.Size);

				buffer.WriteTo(stream);
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

		#region IDisposable Members

		public void Dispose()
		{
			SendBuffer();

			Task.WaitAll(_CompressGraph.CompleteAsync(), _WriteStream);
		}

		#endregion
	}
}