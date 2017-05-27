using System;
using System.IO;
using System.Threading.Tasks;

namespace PCx.IO.Compression
{
	internal sealed class DecompressStream : IDisposable
	{
		#region Fields

		private readonly DecompressGraph _DecompressGraph;

		private Buffer _Buffer = Buffer.Empty;
		private int _BufferPosition;

		#endregion

		#region Constructor

		public DecompressStream(Stream stream)
		{
			_DecompressGraph = new DecompressGraph(stream);
		}

		#endregion

		#region Methods

		public int Read(byte[] buffer, int offset, int count)
		{
			int readCount = 0;

			while (true)
			{
				if (_Buffer.Size == 0)
				{
					if (!ReceiveBuffer())
					{
						break;
					}
				}

				int requiredCount = count - readCount;

				int remainingCount = _Buffer.Size - _BufferPosition;

				if (remainingCount >= requiredCount)
				{
					System.Buffer.BlockCopy(_Buffer.Bytes, _BufferPosition, buffer, offset + readCount, requiredCount);

					readCount += requiredCount;

					_BufferPosition += requiredCount;

					break;
				}
				else
				{
					System.Buffer.BlockCopy(_Buffer.Bytes, _BufferPosition, buffer, offset + readCount, remainingCount);

					readCount += remainingCount;

					_BufferPosition = 0;
					_Buffer = Buffer.Empty;
				}
			}

			return readCount;
		}

		private bool ReceiveBuffer()
		{
			if (_DecompressGraph.OutputAvailableAsync().Result)
			{
				_Buffer = _DecompressGraph.ReceiveAsync().Result;

				_BufferPosition = 0;

				return true;
			}

			_Buffer = Buffer.Empty;

			_BufferPosition = 0;

			return false;
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			_DecompressGraph.CompleteAsync().Wait();
		}

		#endregion
	}
}