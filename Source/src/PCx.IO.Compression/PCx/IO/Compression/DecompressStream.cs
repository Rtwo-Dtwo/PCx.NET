using System;
using System.Diagnostics;
using System.IO;

namespace PCx.IO.Compression
{
	internal sealed class DecompressStream : IDisposable
	{
		#region Fields

		private Stream _Stream;

		private DecompressGraph _DecompressGraph;

		private Buffer _Buffer = Buffer.Empty;
		private int _BufferPosition;

		#endregion

		#region Constructor

		public DecompressStream(Stream stream)
		{
			_Stream = stream;
		}

		#endregion

		#region Methods

		public int Read(byte[] buffer, int offset, int count)
		{
			InitializeGraph();

			var readCount = 0;

			while (true)
			{
				if ((_Buffer.Size == 0) && !ReceiveBuffer())
				{
					break;
				}

				var requiredCount = count - readCount;

				var remainingCount = _Buffer.Size - _BufferPosition;

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

		private void InitializeGraph()
		{
			if (_DecompressGraph == null)
			{
				Debug.Assert(_Stream != null);

				_DecompressGraph = new DecompressGraph(_Stream);
			}
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
			if (_Stream != null)
			{
				try
				{
					if (_DecompressGraph != null)
					{
						try
						{
							_DecompressGraph.CompleteAsync().Wait();
						}
						finally
						{
							_DecompressGraph = null;
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