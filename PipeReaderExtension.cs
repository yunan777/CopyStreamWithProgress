using System.Buffers;
using System.IO.Pipelines;

namespace CopyStreamWithProgress;

public static class PipeReaderExtension
{
    /// <summary>
    /// Asynchronously reads the bytes from the <see cref="System.IO.Pipelines.PipeReader" /> and writes them to the specified stream, using a specified cancellation token.
    /// </summary>
    /// <param name="stream">The stream to which the contents of the current stream will be copied.</param>
    /// <param name="progress">The progress for reporting buffer read byte length.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    public static async Task CopyToAsync(this PipeReader pipeReader, Stream stream, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        if (!stream.CanWrite)
        {
            if (!stream.CanRead)
            {
                throw new ObjectDisposedException(nameof(stream), "Destination stream is disposed.");
            }
            throw new NotSupportedException("Can not write to destination stream.");
        }
        cancellationToken.ThrowIfCancellationRequested();

        while (true)
        {
            ReadResult result = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            ReadOnlySequence<byte> buffer = result.Buffer;
            SequencePosition position = buffer.Start;
            SequencePosition consumed = position;

            try
            {
                if (result.IsCanceled)
                {
                    throw new OperationCanceledException("Read from pipeReader is canceled.");
                }

                while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                {
                    await stream.WriteAsync(memory, cancellationToken).ConfigureAwait(false);
                    consumed = position;
                    progress.Report(memory.Length);
                }
                consumed = buffer.End;
                if (result.IsCompleted)
                {
                    break;
                }
            }
            finally
            {
                pipeReader.AdvanceTo(consumed);
            }
        }
        progress.Report(0);
    }

}

