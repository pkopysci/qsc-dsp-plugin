// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using QscDspDevices.Plugin;
using Xunit;

namespace QscDspDevices.UnitTests;

/// <summary>
/// Snapshot-locks the public API surface of <c>QscDspDevices.dll</c>.
/// Any addition / removal / signature change to a <c>public</c> symbol
/// MUST be reflected by an edit to <c>tests/QscDspDevices.UnitTests/PublicSurface.expected.txt</c>
/// in the same commit. The list is sorted alphabetically so diffs read
/// cleanly.
/// </summary>
/// <remarks>
/// Why a reflection-based snapshot rather than
/// <c>Microsoft.CodeAnalysis.PublicApiAnalyzers</c>: the analyzer's
/// canonical-form bootstrapping requires every symbol's exact
/// <c>RS0016</c>-formatted line to be hand-typed in
/// <c>PublicAPI.Shipped.txt</c>, which is mechanical work that adds no
/// safety this test does not already provide. The reflection snapshot
/// fails identically on drift and is auto-bootstrappable: when the
/// expected file is missing, the test writes the current surface to
/// it and skips. CI never sees a missing file because the file is
/// checked in.
/// </remarks>
public sealed class PublicSurfaceTests
{
    [Fact]
    public void Public_surface_matches_expected_snapshot()
    {
        Assembly assembly = typeof(QscDspTcp).Assembly;
        string actual = RenderSurface(assembly);

        string expectedPath = LocateExpectedFile();
        if (!File.Exists(expectedPath))
        {
            File.WriteAllText(expectedPath, actual);
            throw new Xunit.Sdk.XunitException(
                $"Bootstrap: wrote initial public-surface snapshot to {expectedPath}. Re-run tests.");
        }

        string expected = File.ReadAllText(expectedPath).Replace("\r\n", "\n", StringComparison.Ordinal);
        const string Drift = "the public surface of QscDspDevices.dll has drifted from the snapshot. If this is intentional, update tests/QscDspDevices.UnitTests/PublicSurface.expected.txt to match the diff. If this is unexpected, revert the public symbol change.";
        actual.Should().Be(expected, Drift);
    }

    private static string RenderSurface(Assembly assembly)
    {
        var lines = new List<string>();
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        foreach (Type type in assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            lines.Add($"type {type.FullName}");
            foreach (MemberInfo member in type.GetMembers(flags)
                .Where(m => !(m is MethodInfo mi && mi.IsSpecialName))
                .OrderBy(m => Render(m), StringComparer.Ordinal))
            {
                lines.Add($"  {Render(member)}");
            }
        }

        return string.Join("\n", lines) + "\n";
    }

    private static string Render(MemberInfo member)
    {
        return member switch
        {
            MethodInfo m => $"method {m.ReturnType.Name} {m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))})",
            PropertyInfo p => $"property {p.PropertyType.Name} {p.Name} {{{(p.CanRead ? " get;" : string.Empty)}{(p.CanWrite ? " set;" : string.Empty)} }}",
            FieldInfo f => $"field {f.FieldType.Name} {f.Name}",
            EventInfo e => $"event {e.EventHandlerType?.Name ?? "?"} {e.Name}",
            ConstructorInfo c => $"ctor ({string.Join(",", c.GetParameters().Select(p => p.ParameterType.Name))})",
            _ => $"{member.MemberType} {member.Name}",
        };
    }

    private static string LocateExpectedFile()
    {
        // The test runs from bin/Debug|Release/net8.0; walk up to repo
        // root and locate the file. Falls back to AppContext.BaseDirectory
        // for runners that copy the file alongside the dll.
        string baseDir = AppContext.BaseDirectory;
        string? dir = baseDir;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "tests", "QscDspDevices.UnitTests", "PublicSurface.expected.txt");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            // Stop walking when we leave the repo (we're at /).
            dir = Path.GetDirectoryName(dir);
        }

        // First-run bootstrap: walk up looking for the .csproj and write the
        // expected file next to it.
        dir = baseDir;
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, "tests", "QscDspDevices.UnitTests")))
            {
                return Path.Combine(dir, "tests", "QscDspDevices.UnitTests", "PublicSurface.expected.txt");
            }

            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException("Could not locate the test project root.");
    }
}
