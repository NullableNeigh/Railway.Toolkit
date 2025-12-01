namespace Railway.Toolkit;

/// <summary>
/// Represents the absence of a meaningful value.
/// Used when an operation succeeds but has no return value.
/// Equivalent to void in functional programming.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// The single instance of Unit.
    /// </summary>
    public static readonly Unit Value = new();

    /// <summary>
    /// Returns a string representation of Unit.
    /// </summary>
    public override string ToString() => "()";

    /// <summary>
    /// Determines whether this instance equals another Unit instance.
    /// All Unit instances are equal since they represent the same concept.
    /// </summary>
    public bool Equals(Unit other) => true;

    /// <summary>
    /// Determines whether this instance equals another object.
    /// </summary>
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(Unit left, Unit right) => false;
}
