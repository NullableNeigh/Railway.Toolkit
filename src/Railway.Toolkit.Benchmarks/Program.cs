using BenchmarkDotNet.Running;

namespace Railway.Toolkit;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args is ["--verify"])
        {
            VerifyScenarios();
            ExpandedVerification.Run();
            Console.WriteLine("Benchmark scenario checks passed.");
            return;
        }
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

    // Verify the workloads before interpreting timing differences as library overhead.
    private static void VerifyScenarios()
    {
        foreach (bool failure in new[] { false, true })
        {
            CompositionBenchmarks composition = new CompositionBenchmarks { Failure = failure };
            composition.Setup();
            (bool success, int value, Error? error) = composition.PlainPipeline();
            Result<int> pipeline = composition.RailwayPipeline();
            if (success != (pipeline is Result<int>.Ok) ||
                (pipeline is Result<int>.Ok ok && ok.Value != value) ||
                (pipeline is Result<int>.Fail fail && !ReferenceEquals(error, fail.Error)))
            {
                throw new InvalidOperationException("Synchronous pipeline workloads disagree.");
            }
            AsyncBenchmarks asynchronous = new AsyncBenchmarks { Failure = failure };
            asynchronous.Setup();
            Result<int> completed = asynchronous.CompletedPipeline().GetAwaiter().GetResult();
            Result<int> yielding = asynchronous.YieldingPipeline().GetAwaiter().GetResult();
            int expected = asynchronous.PlainCompletedPipeline().GetAwaiter().GetResult();
            int yieldedExpected = asynchronous.PlainYieldingPipeline().GetAwaiter().GetResult();
            if (expected != yieldedExpected || (completed is Result<int>.Fail) != failure ||
                (yielding is Result<int>.Fail) != failure ||
                (completed is Result<int>.Ok completedOk && completedOk.Value != expected) ||
                (yielding is Result<int>.Ok yieldingOk && yieldingOk.Value != expected))
            {
                throw new InvalidOperationException("Asynchronous pipeline workloads disagree.");
            }
        }
        foreach (BenchmarkLoggingMode mode in Enum.GetValues<BenchmarkLoggingMode>())
        {
            LoggingBenchmarks logging = new LoggingBenchmarks { Mode = mode };
            logging.Setup();
            try
            {
                Result<int> result = logging.Pipeline();
                bool enabled = mode is BenchmarkLoggingMode.EnabledUntimed or BenchmarkLoggingMode.EnabledTimestamp or BenchmarkLoggingMode.EnabledStopwatch;
                if (result is not Result<int>.Ok { Value: 22 } || logging.EventCount != (enabled ? 3 : 0))
                {
                    throw new InvalidOperationException($"Logging workload is incorrect for {mode}.");
                }
            }
            finally
            {
                logging.Cleanup();
            }
        }
    }
}
