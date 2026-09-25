using System;
using System.Collections.Generic;

namespace AbyssalEcologies.Core;

public sealed class WorldPerformanceMeasurement
{
    public WorldPerformanceMeasurement(
        double lateSetupMilliseconds,
        double registrationMilliseconds,
        long managedMemoryDeltaBytes,
        int registeredPlacementCount,
        int expectedPlacementCount)
    {
        LateSetupMilliseconds = lateSetupMilliseconds;
        RegistrationMilliseconds = registrationMilliseconds;
        ManagedMemoryDeltaBytes = managedMemoryDeltaBytes;
        RegisteredPlacementCount = registeredPlacementCount;
        ExpectedPlacementCount = expectedPlacementCount;
    }

    public double LateSetupMilliseconds { get; }
    public double RegistrationMilliseconds { get; }
    public long ManagedMemoryDeltaBytes { get; }
    public int RegisteredPlacementCount { get; }
    public int ExpectedPlacementCount { get; }
}

public sealed class PerformanceBudgetReport
{
    internal PerformanceBudgetReport(IReadOnlyList<string> failures)
    {
        Failures = failures;
    }

    public IReadOnlyList<string> Failures { get; }
    public bool Passed => Failures.Count == 0;
}

public static class PerformanceBudget
{
    public const double MaximumLateSetupMilliseconds = 250d;
    public const double MaximumRegistrationMilliseconds = 100d;
    public const long MaximumManagedMemoryDeltaBytes = 16L * 1024L * 1024L;

    public static PerformanceBudgetReport Evaluate(WorldPerformanceMeasurement measurement)
    {
        if (measurement == null) throw new ArgumentNullException(nameof(measurement));

        var failures = new List<string>();
        if (measurement.LateSetupMilliseconds < 0d || measurement.LateSetupMilliseconds > MaximumLateSetupMilliseconds)
            failures.Add($"Late setup took {measurement.LateSetupMilliseconds:0.0} ms; budget is {MaximumLateSetupMilliseconds:0.0} ms.");
        if (measurement.RegistrationMilliseconds < 0d || measurement.RegistrationMilliseconds > MaximumRegistrationMilliseconds)
            failures.Add($"Spawn registration took {measurement.RegistrationMilliseconds:0.0} ms; budget is {MaximumRegistrationMilliseconds:0.0} ms.");
        if (measurement.ManagedMemoryDeltaBytes > MaximumManagedMemoryDeltaBytes)
            failures.Add($"Managed memory grew by {measurement.ManagedMemoryDeltaBytes} bytes; budget is {MaximumManagedMemoryDeltaBytes} bytes.");
        if (measurement.RegisteredPlacementCount != measurement.ExpectedPlacementCount)
            failures.Add($"Registered {measurement.RegisteredPlacementCount} placements; expected {measurement.ExpectedPlacementCount}.");

        return new PerformanceBudgetReport(failures);
    }
}
