// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-common-utils/ListBuffer.md
// Stub for the real type shipped in: gcu-common-utils 4.3.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

namespace gcu_common_utils.DataObjects;

public class ListBuffer<T>
{
    public string ReadWriteFailureReason => throw new NotImplementedException(
        "FrameworkStubs.ListBuffer<T>.ReadWriteFailureReason — replace stub with real gcu-common-utils package.");

    public bool AddItem(T item) => throw new NotImplementedException(
        "FrameworkStubs.ListBuffer<T>.AddItem — replace stub with real gcu-common-utils package.");

    public bool AddItems(IEnumerable<T> items) => throw new NotImplementedException(
        "FrameworkStubs.ListBuffer<T>.AddItems — replace stub with real gcu-common-utils package.");

    public bool CheckExists(T item) => throw new NotImplementedException(
        "FrameworkStubs.ListBuffer<T>.CheckExists(T) — replace stub with real gcu-common-utils package.");

    public bool CheckExists(IList<T> items) => throw new NotImplementedException(
        "FrameworkStubs.ListBuffer<T>.CheckExists(IList<T>) — replace stub with real gcu-common-utils package.");

    public int GetLength() => throw new NotImplementedException(
        "FrameworkStubs.ListBuffer<T>.GetLength — replace stub with real gcu-common-utils package.");

    public List<T> RemoveByLength(int length) => throw new NotImplementedException(
        "FrameworkStubs.ListBuffer<T>.RemoveByLength — replace stub with real gcu-common-utils package.");

    public List<T> RemoveByDelimiter(T delimiter) => throw new NotImplementedException(
        "FrameworkStubs.ListBuffer<T>.RemoveByDelimiter — replace stub with real gcu-common-utils package.");

    public T[] PeakByLength(int length) => throw new NotImplementedException(
        "FrameworkStubs.ListBuffer<T>.PeakByLength — replace stub with real gcu-common-utils package.");
}
