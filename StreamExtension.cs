using System.IO.Pipelines;

namespace CopyStreamWithProgress;

public static class StreamExtension
{
    /// <summary>
    /// Asynchronously reads the bytes from source stream and writes them to a destination stream.
    /// <para/>
    /// This overload is using pipeline for performance, and support progress report.
    /// </summary>
    /// <param name="destination">The stream to which the contents will be copied.</param>
    /// <param name="pipe">The pipe which used to copy data. This function is not responsible for reset the pipe before or after data transfer.</param>
    /// <param name="progress">The progress for reporting buffer read byte length.</param>
    /// <param name="reportAtRead">True to report progress at reading process, false to report at writing process.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    public static async Task CopyToAsync(this Stream source, Stream destination, Pipe pipe, IProgress<int> progress, bool reportAtRead = true, CancellationToken cancellationToken = default)
    {
        PipeWriter pipeWriter = pipe.Writer;
        PipeReader pipeReader = pipe.Reader;
        Task read = reportAtRead ? source.CopyToAsync(pipeWriter, progress, cancellationToken) : source.CopyToAsync(pipeWriter, cancellationToken);
        Task write = reportAtRead ? pipeReader.CopyToAsync(destination, CancellationToken.None) : pipeReader.CopyToAsync(destination, progress, CancellationToken.None);

        try
        {
            await read.ConfigureAwait(false);
        }
        finally
        {
            await pipeWriter.CompleteAsync().ConfigureAwait(false);
            await write.ConfigureAwait(false);
            await pipeReader.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously reads the bytes from the specified stream and writes them to the <see cref="System.IO.Pipelines.PipeWriter" />.
    /// <para/>
    /// This overload has progress report support.
    /// </summary>
    /// <param name="pipeWriter">
    /// The pipeWriter from which the contents will be copied.
    /// <para/>
    /// This function is not responsible for complete the pipe writer.
    /// </param>
    /// <param name="progress">The progress for reporting buffer read byte length.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    public static async Task CopyToAsync(this Stream stream, PipeWriter pipeWriter, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        if (!stream.CanRead)
        {
            if (!stream.CanWrite)
            {
                throw new ObjectDisposedException(nameof(stream), "Source stream is disposed.");
            }
            throw new NotSupportedException("Can not read from source stream.");
        }
        cancellationToken.ThrowIfCancellationRequested();

        while (true)
        {
            Memory<byte> memory = pipeWriter.GetMemory();
            int bytesRead = await stream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }
            pipeWriter.Advance(bytesRead);
            FlushResult result = await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsCanceled)
            {
                throw new OperationCanceledException("PipeWriter flush canceled.");
            }
            if (result.IsCompleted)
            {
                break;
            }
            progress.Report(bytesRead);
        }
        progress.Report(0);
    }

}
