﻿namespace K5E.Engine.Scanning.Scanners.Pointers
{
    using K5E.Engine.Common;
    using K5E.Engine.Common.Extensions;
    using K5E.Engine.Common.Logging;
    using K5E.Engine.Processes;
    using K5E.Engine.Scanning.Scanners.Constraints;
    using K5E.Engine.Scanning.Scanners.Pointers.SearchKernels;
    using K5E.Engine.Scanning.Scanners.Pointers.Structures;
    using K5E.Engine.Scanning.Snapshots;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using static K5E.Engine.Common.TrackableTask;

    /// <summary>
    /// Validates a snapshot of pointers.
    /// </summary>
    internal static class PointerFilter
    {
        /// <summary>
        /// The name of this scan.
        /// </summary>
        private const String Name = "Pointer Filter";

        /// <summary>
        /// Filters the given snapshot to find all values that are valid pointers.
        /// </summary>
        /// <param name="snapshot">The snapshot on which to perfrom the scan.</param>
        /// <returns></returns>
        public static TrackableTask<Snapshot> Filter(TrackableTask parentTask, Snapshot snapshot, IVectorSearchKernel searchKernel, PointerSize pointerSize, Snapshot DEBUG, UInt32 RADIUS_DEBUG)
        {
            return TrackableTask<Snapshot>
                .Create(PointerFilter.Name, out UpdateProgress updateProgress, out CancellationToken cancellationToken)
                .With(Task<Snapshot>.Run(() =>
                {
                    try
                    {
                        parentTask.CancellationToken.ThrowIfCancellationRequested();

                        ConcurrentBag<IList<SnapshotRegion>> regions = new ConcurrentBag<IList<SnapshotRegion>>();

                        ParallelOptions options = ParallelSettings.ParallelSettingsFastest.Clone();
                        options.CancellationToken = parentTask.CancellationToken;

                        // ISearchKernel DEBUG_KERNEL = new SpanSearchKernel(DEBUG, RADIUS_DEBUG);

                        Parallel.ForEach(
                            snapshot.OptimizedSnapshotRegions,
                            options,
                            (region) =>
                            {
                                // Check for canceled scan
                                parentTask.CancellationToken.ThrowIfCancellationRequested();

                                if (!region.ReadGroup.CanCompare(null))
                                {
                                    return;
                                }

                                ScanConstraints constraints = new ScanConstraints(pointerSize.ToDataType(), null);
                                SnapshotElementVectorComparer vectorComparer = new SnapshotElementVectorComparer(region: region, constraints: constraints);
                                vectorComparer.SetCustomCompareAction(searchKernel.GetSearchKernel(vectorComparer));

                                // SnapshotElementVectorComparer DEBUG_COMPARER = new SnapshotElementVectorComparer(region: region);
                                // DEBUG_COMPARER.SetCustomCompareAction(DEBUG_KERNEL.GetSearchKernel(DEBUG_COMPARER));

                                IList<SnapshotRegion> results = vectorComparer.Compare();

                                // When debugging, these results should be the same as the results above
                                // IList<SnapshotRegion> DEBUG_RESULTS = vectorComparer.Compare();

                                if (!results.IsNullOrEmpty())
                                {
                                    regions.Add(results);
                                }
                            });

                        // Exit if canceled
                        parentTask.CancellationToken.ThrowIfCancellationRequested();

                        snapshot = new Snapshot(PointerFilter.Name, regions.SelectMany(region => region));
                    }
                    catch (OperationCanceledException ex)
                    {
                        Logger.Log(LogLevel.Warn, "Pointer filtering canceled", ex);
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "Error performing pointer filtering", ex);
                        return null;
                    }

                    return snapshot;
                }, parentTask.CancellationToken));
        }
    }
    //// End class
}
//// End namespace