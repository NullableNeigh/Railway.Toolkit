namespace Railway.Toolkit;

// Missing railway structures are programming errors; nullable success data is valid.
internal static class RailwayContracts
{
    internal static T Output<T>(T? value) where T : class
    {
        return value ?? throw new InvalidOperationException("A callback or task returned a null railway structure.");
    }
}
