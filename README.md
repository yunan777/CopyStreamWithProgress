# Example

```c#
using CopyStreamWithProgress;
using System.IO.Pipelines;

namespace Example;

internal class Example
{
    public static async Task CopyDataAsync(Stream source, Stream destination)
    {
        Progress<int> progress = new();
        Pipe pipe = new();
        await source.CopyToAsync(destination, pipe, progress, reportAtRead: true, CancellationToken.None);
    }

    public static async Task CopyDataAsync(Stream source, PipeWriter destination)
    {
        Progress<int> progress = new();
        await source.CopyToAsync(destination, progress, CancellationToken.None);
    }

    public static async Task CopyDataAsync(PipeReader source, Stream destination)
    {
        Progress<int> progress = new();
        await source.CopyToAsync(destination, progress, CancellationToken.None);
    }
}
```

## Class: PipeReaderExtension

Contains extension methods overload the PipeReader.CopyToAsync, with IProgress support.

### Definition

```c#
public static class PipeReaderExtension
```

Namespace: CopyStreamWithProgress

### Methods

```c#
public static async Task CopyToAsync(this PipeReader pipeReader, Stream stream, IProgress<int> progress, CancellationToken cancellationToken = default);
```

## Class: StreamExtension

Contains extension methods overload the Stream.CopyToAsync. Using pipeline for speed, with IProgress support.

### Definition

```c#
public static class StreamExtension
```

Namespace: CopyStreamWithProgress

### Methods

```c#
public static async Task CopyToAsync(this Stream source, Stream destination, Pipe pipe, IProgress<int> progress, bool reportAtRead = true, CancellationToken cancellationToken = default);

public static async Task CopyToAsync(this Stream stream, PipeWriter pipeWriter, IProgress<int> progress, CancellationToken cancellationToken = default);
```
