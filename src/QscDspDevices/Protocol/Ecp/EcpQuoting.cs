// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Text;

namespace QscDspDevices.Protocol.Ecp;

/// <summary>
/// ECP quoting helpers: outbound strings inside a quoted parameter (e.g.
/// the value of <c>css</c>) escape four characters per ECP §1.3 —
/// <c>\n</c>, <c>\r</c>, <c>"</c>, <c>\</c>. Inbound strings reverse the
/// substitution.
/// </summary>
/// <remarks>
/// The escaped form uses the literal two-character sequences
/// <c>\\n</c>, <c>\\r</c>, <c>\\"</c>, <c>\\\\</c> on the wire, exactly
/// as the QSC docs spell them.
/// </remarks>
internal static class EcpQuoting
{
    /// <summary>
    /// Escapes a string for inclusion inside a double-quoted ECP
    /// parameter. The result does not include the surrounding quotes —
    /// callers add those when serialising the full command.
    /// </summary>
    /// <param name="value">The raw string to escape.</param>
    /// <returns>The escaped form; <see cref="string.Empty"/> for an empty input.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="value"/> is null.</exception>
    public static string Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (char ch in value)
        {
            switch (ch)
            {
                case '\\':
                    builder.Append('\\').Append('\\');
                    break;
                case '"':
                    builder.Append('\\').Append('"');
                    break;
                case '\n':
                    builder.Append('\\').Append('n');
                    break;
                case '\r':
                    builder.Append('\\').Append('r');
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Reverses <see cref="Escape(string)"/>. Unrecognised escape
    /// sequences (e.g. <c>\\t</c>) are passed through verbatim — the
    /// ECP spec defines only the four sequences above; the rest are
    /// the Core's problem if it ever produces them.
    /// </summary>
    /// <param name="value">The escaped string from the wire.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="value"/> is null.</exception>
    public static string Unescape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];
            if (ch != '\\' || i + 1 >= value.Length)
            {
                builder.Append(ch);
                continue;
            }

            char next = value[i + 1];
            switch (next)
            {
                case '\\':
                    builder.Append('\\');
                    i++;
                    break;
                case '"':
                    builder.Append('"');
                    i++;
                    break;
                case 'n':
                    builder.Append('\n');
                    i++;
                    break;
                case 'r':
                    builder.Append('\r');
                    i++;
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }
}
