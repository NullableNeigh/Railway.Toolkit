namespace Railway.Toolkit;

/// <summary>
/// Represents the absence of a meaningful value.
/// Used when an operation succeeds but has no return value.
/// Equivalent to void in functional programming.
/// </summary>
public readonly struct Unit
{
    /// <summary>
    /// The single instance of Unit.
    /// </summary>
    public static readonly Unit Value = new();

    /// <summary>
    /// Returns a string representation of Unit.
    /// </summary>
    public override string ToString() => "()";
}
