using System.Diagnostics;

namespace Sorting;

public sealed record MergeLevel<T>(int Threads, IReadOnlyList<IReadOnlyList<T>> Runs);

public sealed record SortResult<T>(
    T[] Items,
    double ElapsedMilliseconds,
    int Threads,
    IReadOnlyList<MergeLevel<T>> Levels);

public static class ParallelMergeSort
{
    public static SortResult<T> Sort<T>(IReadOnlyList<T> source, int threads, IComparer<T>? comparer = null)
    {
        if (threads < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(threads));
        }

        comparer ??= Comparer<T>.Default;
        var watch = Stopwatch.StartNew();
        if (source.Count <= 1)
        {
            watch.Stop();
            return new SortResult<T>(source.ToArray(), Elapsed(watch), threads, []);
        }

        var workerCount = Math.Min(threads, source.Count);
        var chunks = Split(source, workerCount);
        var runs = new T[workerCount][];
        var tasks = new Task[workerCount];
        for (var i = 0; i < workerCount; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() => runs[index] = SequentialMergeSort(chunks[index], comparer));
        }

        Task.WaitAll(tasks);

        var levels = new List<MergeLevel<T>>
        {
            new(workerCount, Copy(runs))
        };

        var current = runs;
        while (current.Length > 1)
        {
            var nextCount = (current.Length + 1) / 2;
            var next = new T[nextCount][];
            var mergeTasks = new Task[nextCount];
            for (var i = 0; i < nextCount; i++)
            {
                var index = i;
                mergeTasks[i] = Task.Run(() =>
                {
                    var left = index * 2;
                    if (left + 1 >= current.Length)
                    {
                        next[index] = current[left];
                        return;
                    }

                    next[index] = Merge(current[left], current[left + 1], comparer);
                });
            }

            Task.WaitAll(mergeTasks);
            current = next;
            levels.Add(new MergeLevel<T>(nextCount, Copy(current)));
        }

        watch.Stop();
        return new SortResult<T>(current[0], Elapsed(watch), workerCount, levels);
    }

    private static T[][] Split<T>(IReadOnlyList<T> source, int threads)
    {
        var chunks = new T[threads][];
        var baseSize = source.Count / threads;
        var remainder = source.Count % threads;
        var offset = 0;
        for (var i = 0; i < threads; i++)
        {
            var size = baseSize + (i >= threads - remainder ? 1 : 0);
            var chunk = new T[size];
            for (var j = 0; j < size; j++)
            {
                chunk[j] = source[offset + j];
            }

            chunks[i] = chunk;
            offset += size;
        }

        return chunks;
    }

    private static T[] SequentialMergeSort<T>(T[] source, IComparer<T> comparer)
    {
        if (source.Length <= 1)
        {
            return source;
        }

        var middle = source.Length / 2;
        var left = SequentialMergeSort(source[..middle], comparer);
        var right = SequentialMergeSort(source[middle..], comparer);
        return Merge(left, right, comparer);
    }

    private static T[] Merge<T>(T[] left, T[] right, IComparer<T> comparer)
    {
        var merged = new T[left.Length + right.Length];
        var i = 0;
        var j = 0;
        var k = 0;
        while (i < left.Length && j < right.Length)
        {
            merged[k++] = comparer.Compare(left[i], right[j]) <= 0 ? left[i++] : right[j++];
        }

        while (i < left.Length)
        {
            merged[k++] = left[i++];
        }

        while (j < right.Length)
        {
            merged[k++] = right[j++];
        }

        return merged;
    }

    private static IReadOnlyList<IReadOnlyList<T>> Copy<T>(T[][] runs)
    {
        return runs.Select(run => (IReadOnlyList<T>)run).ToArray();
    }

    private static double Elapsed(Stopwatch watch)
    {
        return Math.Round(watch.Elapsed.TotalMilliseconds, 3);
    }
}
