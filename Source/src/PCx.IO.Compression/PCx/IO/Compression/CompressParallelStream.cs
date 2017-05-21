//using System;
//using System.IO;
//using System.IO.Compression;

//namespace PCx.IO.Compression
//{
//	/// <summary>
//	/// Compress parallel stream.
//	/// </summary>
//	public class CompressParallelStream : Stream
//	{
//		#region Fields

//		private readonly Stream _Stream;

//		private readonly CompressGraph _CompressGraph;

//		private readonly byte[] _Buffer;
//		private int _BufferPosition;

//		#endregion

//		#region Constructor

//		/// <summary>
//		/// Initializes a new instance of the <see cref="T:PCx.IO.Compression.CompressStream"/> class.
//		/// </summary>
//		/// <param name="stream">Stream.</param>
//		/// <param name="compressionLevel">Compression level.</param>
//		/// <param name="bufferSize">Buffer size.</param>
//		public CompressParallelStream(Stream stream, CompressionLevel compressionLevel, int bufferSize)
//		{
//			_Stream = stream;

//			_CompressGraph = new CompressGraph(stream, compressionLevel, new Progress<double>());

//			_Buffer = new byte[bufferSize];
//		}

//		#endregion

//		#region Properties

//		/// <summary>
//		/// Gets the base stream.
//		/// </summary>
//		/// <value>The base stream.</value>
//		public Stream BaseStream
//		{
//			get
//			{
//				return _Stream;
//			}
//		}

//		#endregion

//		#region Methods

//		private void SendBuffer()
//		{
//			var bytes = new byte[_BufferPosition];
//			System.Buffer.BlockCopy(_Buffer, 0, bytes, 0, _BufferPosition);

//			_BufferPosition = 0;

//			_CompressGraph.SendAsync(new Buffer(bytes)).Wait();
//		}

//		#endregion

//		#region Overridden from Stream

//		#region Properties

//		/// <summary>
//		/// Gets a value indicating whether this <see cref="T:PCx.IO.Compression.CompressStream"/> can read.
//		/// </summary>
//		/// <value><c>true</c> if can read; otherwise, <c>false</c>.</value>
//		public override bool CanRead
//		{
//			get
//			{
//				return false;
//			}
//		}

//		/// <summary>
//		/// Gets a value indicating whether this <see cref="T:PCx.IO.Compression.CompressStream"/> can seek.
//		/// </summary>
//		/// <value><c>true</c> if can seek; otherwise, <c>false</c>.</value>
//		public override bool CanSeek
//		{
//			get
//			{
//				return false;
//			}
//		}

//		/// <summary>
//		/// Gets a value indicating whether this <see cref="T:PCx.IO.Compression.CompressStream"/> can write.
//		/// </summary>
//		/// <value><c>true</c> if can write; otherwise, <c>false</c>.</value>
//		public override bool CanWrite
//		{
//			get
//			{
//				return _Stream.CanWrite;
//			}
//		}

//		/// <summary>
//		/// Gets the length.
//		/// </summary>
//		/// <value>The length.</value>
//		public override long Length
//		{
//			get
//			{
//				throw new NotSupportedException();
//			}
//		}

//		/// <summary>
//		/// Gets or sets the position.
//		/// </summary>
//		/// <value>The position.</value>
//		public override long Position
//		{
//			get
//			{
//				throw new NotSupportedException();
//			}
//			set
//			{
//				throw new NotSupportedException();
//			}
//		}

//		#endregion

//		#region Methods

//		/// <summary>
//		/// Write the specified buffer, offset and count.
//		/// </summary>
//		/// <returns>The write.</returns>
//		/// <param name="buffer">Buffer.</param>
//		/// <param name="offset">Offset.</param>
//		/// <param name="count">Count.</param>
//		public override void Write(byte[] buffer, int offset, int count)
//		{
//			var bufferPosition = 0;

//			while (true)
//			{
//				if (_BufferPosition + (count - bufferPosition) >= _Buffer.Length)
//				{
//					var copyCount = _Buffer.Length - _BufferPosition;
//					System.Buffer.BlockCopy(buffer, bufferPosition, _Buffer, _BufferPosition, copyCount);

//					_BufferPosition += copyCount;

//					bufferPosition += copyCount;

//					SendBuffer();
//				}
//				else
//				{
//					var copyCount = count - bufferPosition;
//					System.Buffer.BlockCopy(buffer, bufferPosition, _Buffer, _BufferPosition, copyCount);

//					_BufferPosition += copyCount;

//					break;
//				}
//			}
//		}

//		/// <summary>
//		/// Flush this instance.
//		/// </summary>
//		public override void Flush()
//		{
//			if (_BufferPosition > 0)
//			{
//				SendBuffer();
//			}
//		}

//		/// <summary>
//		/// Read the specified buffer, offset and count.
//		/// </summary>
//		/// <returns>The read.</returns>
//		/// <param name="buffer">Buffer.</param>
//		/// <param name="offset">Offset.</param>
//		/// <param name="count">Count.</param>
//		public override int Read(byte[] buffer, int offset, int count)
//		{
//			throw new NotSupportedException();
//		}

//		/// <summary>
//		/// Seek the specified offset and origin.
//		/// </summary>
//		/// <returns>The seek.</returns>
//		/// <param name="offset">Offset.</param>
//		/// <param name="origin">Origin.</param>
//		public override long Seek(long offset, SeekOrigin origin)
//		{
//			throw new NotSupportedException();
//		}

//		/// <summary>
//		/// Sets the length.
//		/// </summary>
//		/// <param name="value">Value.</param>
//		public override void SetLength(long value)
//		{
//			throw new NotSupportedException();
//		}

//		/// <summary>
//		/// Dispose the specified disposing.
//		/// </summary>
//		/// <returns>The dispose.</returns>
//		/// <param name="disposing">If set to <c>true</c> disposing.</param>
//		protected override void Dispose(bool disposing)
//		{
//			try
//			{
//				if (_BufferPosition > 0)
//				{
//					SendBuffer();
//				}

//				_CompressGraph.CompleteAsync().Wait();
//			}
//			finally
//			{
//				base.Dispose(disposing);
//			}
//		}

//		#endregion

//		#endregion
//	}
//}