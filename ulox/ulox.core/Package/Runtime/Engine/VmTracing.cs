using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ULox
{
    public sealed class VmTracingReporter
    {
        public sealed class ChunkStatistics
        {
            public int[] OpCodeOccurances = new int[OpCodeUtil.NumberOfOpCodes];
        }

        public struct TimeLineEvent
        {
            public enum Phase
            {
                Begin,
                End,
                Instant,
            }

            public string name;
            public Phase phase;
            public long timeStamp;
            public int threadId;

            public override string ToString()
            {
                return $"{phase}:{name}";
            }
        }

        private readonly Dictionary<Chunk, ChunkStatistics> _perChunkStats = new();
        public IReadOnlyDictionary<Chunk, ChunkStatistics> PerChunkStats => _perChunkStats;

        private readonly Stopwatch _stopWatch = Stopwatch.StartNew();
        private readonly List<TimeLineEvent> _timeLineEvents = new();
        public IReadOnlyList<TimeLineEvent> TimeLineEvents => _timeLineEvents;
        public bool EnableTracing { get; set; } = false;
        public bool EnableOpCodeInstantTraces { get; set; } = false;

        public void ProcessingOpCode(Chunk chunk, OpCode opCode)
        {
            if(EnableOpCodeInstantTraces)
            {
                AppendTimeLineEvent(opCode.ToString(), TimeLineEvent.Phase.Instant);
            }

            if (!_perChunkStats.TryGetValue(chunk, out var stats))
            {
                stats = new ChunkStatistics();
                _perChunkStats.Add(chunk, stats);
            }

            stats.OpCodeOccurances[(byte)opCode]++;
        }

        public void ProcessPushCallFrame(CallFrame callFrame)
        {
            AppendTimeLineEvent(callFrame, TimeLineEvent.Phase.Begin);
        }

        public void ProcessPopCallFrame(CallFrame callFrame)
        {
            AppendTimeLineEvent(callFrame, TimeLineEvent.Phase.End);
        }

        private void AppendTimeLineEvent(CallFrame callFrame, TimeLineEvent.Phase phase)
        {
            if (!EnableTracing) return;

            var name = callFrame.nativeFunc == null
                ? callFrame.Closure.chunk.FullName
                : callFrame.nativeFunc.Method.Name;
            AppendTimeLineEvent(name, phase);
        }

        private void AppendTimeLineEvent(string name, TimeLineEvent.Phase phase)
        {
            _timeLineEvents.Add(new TimeLineEvent()
            {
                name = name,
                phase = phase,
                timeStamp = _stopWatch.ElapsedTicks,
                threadId = Environment.CurrentManagedThreadId,
            });
        }
    }

    public sealed class VmStatisticsReport
    {
        public sealed class ChunkStatistics
        {
            public string name;
            public int[] OpCodeOccurances = new int[OpCodeUtil.NumberOfOpCodes];
        }

        private readonly List<ChunkStatistics> _chunkStatistics = new();

        public IReadOnlyList<ChunkStatistics> ChunksStats => _chunkStatistics;

        public static VmStatisticsReport Create(IReadOnlyDictionary<Chunk, VmTracingReporter.ChunkStatistics> chunkLookUp)
        {
            var report = new VmStatisticsReport();

            foreach (var item in chunkLookUp)
            {
                var chunkStats = new ChunkStatistics()
                {
                    name = item.Key.FullName,
                    OpCodeOccurances = item.Value.OpCodeOccurances,
                };
                report._chunkStatistics.Add(chunkStats);
            }

            return report;
        }

        public string GenerateStringReport()
        {
            var sb = new StringBuilder();

            foreach (var chunk in ChunksStats)
            {
                sb.AppendLine($"Chunk: {chunk.name}");
                var occurances = chunk.OpCodeOccurances;
                for (int i = 0; i < occurances.Length; i++)
                {
                    var occur = occurances[i];
                    if (occur == 0) continue;
                    sb.AppendLine($"  {(OpCode)i}   {occur}");
                }
            }

            return sb.ToString();
        }
    }


    public sealed class VmTracingReport
    {
        public IReadOnlyList<VmTracingReporter.TimeLineEvent> Timeline { get; private set; }

        public static VmTracingReport Create(IReadOnlyList<VmTracingReporter.TimeLineEvent> timeLineEvents)
        {
            var report = new VmTracingReport();
            report.Timeline = timeLineEvents;

            return report;
        }

        public string GenerateJsonTracingEventArray()
        {
            var processId = Process.GetCurrentProcess().Id;
            var sb = new StringBuilder();
            sb.AppendLine("[");

            var length = Timeline.Count;
            for (int i = 0; i < length; i++)
            {
                var item = Timeline[i];
                sb.Append($"{{\"name\":\"{item.name}\"," +
                    $"\"ph\":\"{PhaseToString(item.phase)}\"," +
                    $"\"ts\":{item.timeStamp}," +
                    $"\"pid\":{processId}," +
                    $"\"tid\":{item.threadId}}}");

                if (i < length - 1)
                {
                    sb.Append(',');
                }
                sb.AppendLine();
            }

            sb.Append(']');
            return sb.ToString();
        }

        public static string PhaseToString(VmTracingReporter.TimeLineEvent.Phase phase)
        {
            return phase switch
            {
                VmTracingReporter.TimeLineEvent.Phase.Begin => "B",
                VmTracingReporter.TimeLineEvent.Phase.End => "E",
                VmTracingReporter.TimeLineEvent.Phase.Instant => "I",
                _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null),
            };
        }
    }
}
