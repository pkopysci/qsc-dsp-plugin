// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-common-utils/GenericEventArgs.md
// Stub for the real types shipped in: gcu-common-utils 4.3.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

namespace gcu_common_utils.GenericEventArgs;

public class GenericSingleEventArgs<T>(T arg) : EventArgs
{
    public T Arg { get; } = arg;
}

public class GenericDualEventArgs<T1, T2>(T1 arg1, T2 arg2) : EventArgs
{
    public T1 Arg1 { get; } = arg1;

    public T2 Arg2 { get; } = arg2;
}

// Note: spelling 'Tripple' (two p's) matches the framework documentation verbatim.
public class GenericTrippleEventArgs<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3) : EventArgs
{
    public T1 Arg1 { get; } = arg1;

    public T2 Arg2 { get; } = arg2;

    public T3 Arg3 { get; } = arg3;
}
