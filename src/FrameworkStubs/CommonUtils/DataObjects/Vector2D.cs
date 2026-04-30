// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-common-utils/Vector2D.md
// Stub for the real type shipped in: gcu-common-utils 4.3.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

namespace gcu_common_utils.DataObjects;

public class Vector2D
{
    // Documented values per framework-docs/gcu-common-utils/Vector2D.md.
    // The framework-stubs spec permits trivial returns "where the documentation
    // explicitly defines a default" — these directional vectors qualify, so
    // they MUST hold the documented values, not zeros (otherwise tests pass
    // against the stub but break against the real DLL).
    public static readonly Vector2D Up = new() { X = 0, Y = 1 }; // Spec: Up = [0, 1]
    public static readonly Vector2D Down = new() { X = 0, Y = -1 }; // Spec: Down = [0, -1]
    public static readonly Vector2D Left = new() { X = -1, Y = 0 }; // Spec: Left = [-1, 0]
    public static readonly Vector2D Right = new() { X = 1, Y = 0 }; // Spec: Right = [1, 0]
    public static readonly Vector2D Zero = new() { X = 0, Y = 0 }; // Spec: Zero = [0, 0]

    public float X
    {
        get; set;
    }

    public float Y
    {
        get; set;
    }

    // Documented behaviour: "true if the X and Y properties of this object
    // match the X and Y properties of the compared Vector2D." Trivial enough
    // that the spec permits a real implementation in the stub (rather than
    // a throw that would break consumers reading these readonly directionals).
    public override bool Equals(object? obj)
        => obj is Vector2D other && other.X == X && other.Y == Y;

    public override int GetHashCode() => HashCode.Combine(X, Y);
}
