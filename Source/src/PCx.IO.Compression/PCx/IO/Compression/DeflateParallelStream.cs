using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;

namespace PCx.IO.Compression
{
	/// <summary>
	/// Provides methods and properties for compressing and decompressing streams by using the Deflate algorithm.
	/// In comparison to <see cref="DeflateStream"/> this stream is optimized by using parallel algorithms.
	/// <remarks>
	/// A compressed stream by <see cref="DeflateParallelStream"/> is incompatible with decompression by <see cref="DeflateStream"/>.
	/// A compressed stream by <see cref="DeflateStream"/> is incompatible with decompression by <see cref="DeflateParallelStream"/>.
	/// A with <see cref="DeflateParallelStream"/> compressed stream has to be decompressed with <see cref="DeflateParallelStream"/>.
	/// </remarks>
	/// </summary>
	public sealed class DeflateParallelStream : Stream
	{
		#region Fields

		private Stream _Stream;

		private readonly bool _LeaveOpen;

		private CompressStream _CompressStream;
		private DecompressStream _DecompressStream;

		private const int DefaultBufferSize = 81920;

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="DeflateParallelStream"/> class
		/// using the specified <paramref name="stream"/> and <paramref name="compressionMode"/>.
		/// </summary>
		/// <param name="stream">The stream to compress or decompress.</param>
		/// <param name="compressionMode">One of the enumeration values that indicates whether to compress or decompress the stream.</param>
		public DeflateParallelStream(Stream stream, CompressionMode compressionMode)
			: this(stream, compressionMode, leaveOpen: false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DeflateParallelStream"/> class
		/// using the specified <paramref name="stream"/> and <paramref name="compressionMode"/>,
		/// and optionally leaves the stream open.
		/// </summary>
		/// <param name="stream">The stream to compress or decompress.</param>
		/// <param name="compressionMode">One of the enumeration values that indicates whether to compress or decompress the stream.</param>
		/// <param name="leaveOpen">true to leave the stream object open after disposing the <see cref="DeflateParallelStream"/> object; otherwise, false.</param>
		public DeflateParallelStream(Stream stream, CompressionMode compressionMode, bool leaveOpen)
		{
			#region Contracts

			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			if ((compressionMode == CompressionMode.Compress) && !stream.CanWrite)
			{
				throw new NotSupportedException("stream does not support writing");
			}

			if ((compressionMode == CompressionMode.Decompress) && !stream.CanRead)
			{
				throw new NotSupportedException("stream does not support reading");
			}

			Contract.EndContractBlock();

			#endregion

			_Stream = stream;

			_LeaveOpen = leaveOpen;

			switch (compressionMode)
			{
				case CompressionMode.Compress:

					_CompressStream = new CompressStream(_Stream, CompressionLevel.Optimal, DefaultBufferSize);

					break;

				case CompressionMode.Decompress:

					_DecompressStream = new DecompressStream(_Stream);

					break;

				default:

					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DeflateParallelStream"/> class
		/// using the specified <paramref name="stream"/>, <paramref name="compressionLevel"/> and <paramref name="bufferSize"/>.
		/// </summary>
		/// <param name="stream">The stream to compress.</param>
		/// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression efficiency when compressing the stream.</param>
		/// <param name="bufferSize">The size of the buffer. This value must be greater than zero.</param>
		/// <remarks>
		/// The specified <paramref name="bufferSize"/> affects the parallelization potential and
		/// and should be selected in dependency of the size of the <paramref name="stream"/> stream.
		/// </remarks>
		public DeflateParallelStream(Stream stream, CompressionLevel compressionLevel, int bufferSize)
			: this(stream, compressionLevel, bufferSize, leaveOpen: false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DeflateParallelStream"/> class
		/// using the specified <paramref name="stream"/>, <paramref name="compressionLevel"/> and <paramref name="bufferSize"/>,
		/// and optionally leaves the stream open.
		/// </summary>
		/// <param name="stream">The stream to compress.</param>
		/// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression efficiency when compressing the stream.</param>
		/// <param name="bufferSize">The size of the buffer. This value must be greater than zero.</param>
		/// <param name="leaveOpen">true to leave the stream object open after disposing the <see cref="DeflateParallelStream"/> object; otherwise, false.</param>
		/// <remarks>
		/// The specified <paramref name="bufferSize"/> affects the parallelization potential and
		/// and should be selected in dependency of the size of the <paramref name="stream"/> stream.
		/// </remarks>
		public DeflateParallelStream(Stream stream, CompressionLevel compressionLevel, int bufferSize, bool leaveOpen)
		{
			#region Contracts

			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			if (!stream.CanWrite)
			{
				throw new NotSupportedException("stream does not support writing");
			}

			if (bufferSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(bufferSize), "bufferSize is negative or zero");
			}

			Contract.EndContractBlock();

			#endregion

			_Stream = stream;

			_LeaveOpen = leaveOpen;

			_CompressStream = new CompressStream(_Stream, compressionLevel, bufferSize);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets a reference to the underlying stream.
		/// </summary>
		/// <value>A stream object that represents the underlying stream.</value>
		public Stream BaseStream
		{
			get
			{
				return _Stream;
			}
		}

		#endregion

		#region Methods

		private void EnsureNotDisposed()
		{
			if (_Stream == null)
			{
				throw new ObjectDisposedException(null, "Stream is closed");
			}
		}

		private void EnsureCompressionMode()
		{
			if (_CompressStream != null)
			{
				throw new InvalidOperationException("Cannot write to stream");
			}
		}

		private void EnsureDecompressionMode()
		{
			if (_DecompressStream != null)
			{
				throw new InvalidOperationException("Cannot read from stream");
			}
		}

		private void ValidateParameters(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

            if (offset < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(offset));
			}

            if (count < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			if ((buffer.Length - offset) < count)
			{
				throw new ArgumentException($"{nameof(buffer)} length minus {nameof(offset)} is less than {nameof(count)}");
			}
		}

		#endregion

		#region Overridden from Stream

		#region Properties

		/// <summary>
		/// See <see cref="Stream.CanRead"/> 
		/// </summary>
		public override bool CanRead
		{
			get
			{
				if (_Stream == null)
				{
					return false;
				}

				return _DecompressStream != null;
			}
		}

		/// <summary>
		/// See <see cref="Stream.CanWrite"/> 
		/// </summary>
		public override bool CanWrite
		{
			get
			{
				if (_Stream == null)
				{
					return false;
				}

				return _CompressStream != null;
			}
		}

		/// <summary>
		/// See <see cref="Stream.CanSeek"/> 
		/// </summary>
		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// See <see cref="Stream.Length"/> 
		/// </summary>
		public override long Length
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// See <see cref="Stream.Position"/> 
		/// </summary>
		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// See <see cref="Stream.Read"/> 
		/// </summary>
		public override int Read(byte[] buffer, int offset, int count)
		{
			#region Contracts

			ValidateParameters(buffer, offset, count);

			Contract.EndContractBlock();

			#endregion

			EnsureNotDisposed();
			EnsureDecompressionMode();

			return _DecompressStream.Read(buffer, offset, count);
		}

		/// <summary>
		/// See <see cref="Stream.Write"/> 
		/// </summary>
		public override void Write(byte[] buffer, int offset, int count)
		{
			#region Contracts

			ValidateParameters(buffer, offset, count);

			Contract.EndContractBlock();

			#endregion

			EnsureNotDisposed();
			EnsureCompressionMode();

			_CompressStream.Write(buffer, offset, count);
		}

		/// <summary>
		/// See <see cref="Stream.Flush"/> 
		/// </summary>
		public override void Flush()
		{
			EnsureNotDisposed();

			if (_CompressStream != null)
			{
				_CompressStream.Flush();
			}
		}

		/// <summary>
		/// See <see cref="Stream.Seek"/> 
		/// </summary>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// See <see cref="Stream.SetLength"/> 
		/// </summary>
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// See <see cref="Stream.Dispose(bool)"/> 
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					try
					{
						if (_CompressStream != null)
						{
							_CompressStream.Dispose();
						}
					}
					finally
					{
						_CompressStream = null;
					}

					try
					{
						if (_DecompressStream != null)
						{
							_DecompressStream.Dispose();
						}
					}
					finally
					{
						_DecompressStream = null;
					}

					try
					{
						if ((_Stream != null) && !_LeaveOpen)
						{
							_Stream.Dispose();
						}
					}
					finally
					{
						_Stream = null;
					}

				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		#endregion

		#endregion
	}
}