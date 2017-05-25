using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PCx.IO.Compression
{
	partial class StreamExtensions
	{
		#region Methods

		/// <summary>
		/// Reads the compressed bytes from the <paramref name="source"/> stream and
		/// writes them decompressed to the <paramref name="destination"/> stream.
		/// <para/>
		/// This operation is optimized by using parallel algorithms.
		/// </summary>
		/// <param name="source">The stream from which the compressed content will be read.</param>
		/// <param name="destination">The stream to which the decompressed content will be written.</param>
		/// <returns>The awaitable <see cref="Task"/> to synchronize the operation.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is null.</exception>
		/// <exception cref="System.NotSupportedException"><paramref name="source"/> does not support reading or <paramref name="destination"/> does not support writing.</exception>
		/// <remarks>
		/// The compressed content must be created by the method <see cref="CompressParallelToAsync(System.IO.Stream, System.IO.Stream, System.IO.Compression.CompressionLevel, int, System.IProgress{double})"/>.
		/// <para/>
		/// Copying begins at the current position in the <paramref name="source"/> stream,
		/// and does not reset the position of the <paramref name="destination"/> stream after the copy operation is complete.
		/// </remarks>
		public static Task DecompressParallelToAsync(this Stream source, Stream destination)
		{
			return source.DecompressParallelToAsync(destination, new Progress<double>());
		}

		/// <summary>
		/// Reads the compressed bytes from the <paramref name="source"/> stream and
		/// writes them decompressed to the <paramref name="destination"/> stream.
		/// <para/>
		/// This operation is optimized by using parallel algorithms.
		/// </summary>
		/// <param name="source">The stream from which the compressed content will be read.</param>
		/// <param name="destination">The stream to which the decompressed content will be written.</param>
		/// <param name="progress">The progress that is used for progress reporting.</param>
		/// <returns>The awaitable <see cref="Task"/> to synchronize the operation.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="source"/>, <paramref name="destination"/> or <paramref name="progress"/> is null.</exception>
		/// <exception cref="System.NotSupportedException"><paramref name="source"/> does not support reading or <paramref name="destination"/> does not support writing.</exception>
		/// <remarks>
		/// The compressed content must be created by the method <see cref="CompressParallelToAsync(System.IO.Stream, System.IO.Stream, System.IO.Compression.CompressionLevel, int, System.IProgress{double})"/>.
		/// <para/>
		/// Copying begins at the current position in the <paramref name="source"/> stream,
		/// and does not reset the position of the <paramref name="destination"/> stream after the copy operation is complete.
		/// </remarks>
		public static async Task DecompressParallelToAsync(this Stream source, Stream destination, IProgress<double> progress)
		{
			#region Contracts

			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (!source.CanRead)
			{
				throw new NotSupportedException("source does not support reading");
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (!destination.CanWrite)
			{
				throw new NotSupportedException("destination does not support writing");
			}

			if (progress == null)
			{
				throw new ArgumentNullException(nameof(progress));
			}

			Contract.EndContractBlock();

			#endregion

			var decompressGraph = new DecompressGraph(source);

			while (await decompressGraph.OutputAvailableAsync())
			{
				var buffer = decompressGraph.Receive();

				buffer.WriteTo(destination, progress);
			}

			await decompressGraph.CompleteAsync();

			progress.Report(1.0);
		}

		#endregion
	}
}