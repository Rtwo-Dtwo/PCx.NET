using System;
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

		private readonly Stream _Stream;

		private readonly CompressStream _CompressStream;

		private readonly DecompressStream _DecompressStream;

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="DeflateParallelStream"/> class
		/// using the specified <paramref name="stream"/> and <paramref name="compressionMode"/>.
		/// </summary>
		/// <param name="stream">The stream to compress or decompress.</param>
		/// <param name="compressionMode">One of the enumeration values that indicates whether to compress or decompress the stream.</param>
		public DeflateParallelStream(Stream stream, CompressionMode compressionMode)
		{
			_Stream = stream;

			switch (compressionMode)
			{
				case CompressionMode.Compress:

					_CompressStream = new CompressStream(_Stream, CompressionLevel.Optimal, 80 * 1024);

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
		{
			_Stream = stream;

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

		#region Overridden from Stream

		#region Properties

		/// <summary>
		/// See <see cref="Stream.CanRead"/> 
		/// </summary>
		public override bool CanRead
		{
			get
			{
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
			return _DecompressStream.Read(buffer, offset, count);
		}

		/// <summary>
		/// See <see cref="Stream.Write"/> 
		/// </summary>
		public override void Write(byte[] buffer, int offset, int count)
		{
			_CompressStream.Write(buffer, offset, count);
		}

		/// <summary>
		/// See <see cref="Stream.Flush"/> 
		/// </summary>
		public override void Flush()
		{
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
				if (_CompressStream != null)
				{
					_CompressStream.Dispose();
				}

				if (_DecompressStream != null)
				{
					_DecompressStream.Dispose();
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