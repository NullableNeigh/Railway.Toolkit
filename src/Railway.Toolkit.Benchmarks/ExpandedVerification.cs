namespace Railway.Toolkit;

internal static class ExpandedVerification
{
    internal static void Require(bool condition, string scenario)
    {
        if (!condition) throw new InvalidOperationException($"Benchmark scenario failed: {scenario}");
    }
    internal static void CheckTrack<T>(Result<T> result, bool success) => Require(success ? result is Result<T>.Ok : result is Result<T>.Fail, "Result track");

    internal static void Run()
    {
        foreach (bool flag in new[] { false, true })
        {
            FallbackBenchmarks fallback = new FallbackBenchmarks { Failure = flag }; fallback.Setup(); fallback.Verify();
            ValidationBenchmarks validation = new ValidationBenchmarks { Pass = flag }; validation.Setup(); validation.Verify();
            ZipBenchmarks zip = new ZipBenchmarks { Failure = flag }; zip.Setup(); zip.Verify();
            ErrorTransformBenchmarks error = new ErrorTransformBenchmarks { Failure = flag }; error.Setup(); error.Verify();
            ObservationBenchmarks observation = new ObservationBenchmarks { Failure = flag }; observation.Setup(); observation.Verify();
            CancellationBenchmarks cancellation = new CancellationBenchmarks { Failure = flag }; cancellation.Setup(); cancellation.Verify();
            IncomingTaskBenchmarks incoming = new IncomingTaskBenchmarks { Failure = flag }; incoming.Setup(); incoming.Verify();
            foreach (int steps in new[] { 1, 16 })
            {
                PipelineShapeBenchmarks shape = new PipelineShapeBenchmarks { Failure = flag, Steps = steps }; shape.Setup(); shape.Verify();
            }
            foreach (int count in new[] { 2, 16 })
            {
                CombinedValidationBenchmarks combined = new CombinedValidationBenchmarks { Failures = flag, Count = count }; combined.Setup(); combined.Verify();
            }
            LoggingFailureBenchmarks logging = new LoggingFailureBenchmarks { Enabled = flag };
            logging.Setup();
            try { logging.Verify(); } finally { logging.Cleanup(); }
        }
        TryBenchmarks attempt = new TryBenchmarks(); attempt.Setup(); attempt.Verify();
        CancellationExceptionBenchmarks cancelled = new CancellationExceptionBenchmarks(); cancelled.Setup(); cancelled.Verify();
        foreach (int count in new[] { 8, 128 })
        {
            CollectionBenchmarks collection = new CollectionBenchmarks { Count = count }; collection.Setup(); collection.Verify();
        }
        foreach (int count in new[] { 2, 16 })
        {
            ErrorConstructionBenchmarks errors = new ErrorConstructionBenchmarks { Count = count }; errors.Setup(); errors.Verify();
        }
        ResultBenchmarks result = new ResultBenchmarks();
        Require(result.PatternMatchSuccess() == result.MatchSuccess() && result.PatternMatchFailure() == result.MatchFailure(), "Matching alternatives");
    }
}
