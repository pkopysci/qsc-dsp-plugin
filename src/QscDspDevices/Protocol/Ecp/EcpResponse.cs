// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Globalization;

namespace QscDspDevices.Protocol.Ecp;

/// <summary>
/// Tag for the kind of <see cref="EcpResponse"/> parsed from a single
/// inbound frame. One enum value per response shape in ECP §3.4 + §3.5
/// + the auth and error sentinels.
/// </summary>
internal enum EcpResponseKind
{
    /// <summary>Unrecognised / malformed line. Surface as Logger.Warn.</summary>
    Unknown,

    /// <summary>`sr "DESIGN" "ID" IS_PRIMARY IS_ACTIVE` — Status Report.</summary>
    StatusReport,

    /// <summary>`cv "ID" "DISPLAY" VALUE POSITION` — scalar control value.</summary>
    ControlValue,

    /// <summary>`cgpa` — change-group poll ack (end-of-poll sentinel).</summary>
    ChangeGroupPollAck,

    /// <summary>`login_required`.</summary>
    LoginRequired,

    /// <summary>`login_success`.</summary>
    LoginSuccess,

    /// <summary>`login_failed` — Core will close the socket.</summary>
    LoginFailed,

    /// <summary>`core_not_active` — write rejected because this Core is on Standby.</summary>
    CoreNotActive,

    /// <summary>`bad_id "CONTROL_ID"` — control name unknown.</summary>
    BadId,

    /// <summary>`control_read_only` — write rejected, control is RO.</summary>
    ControlReadOnly,

    /// <summary>`bad_change_group_handle` — group id unknown.</summary>
    BadChangeGroupHandle,

    /// <summary>`too_many_change_groups` — group budget exceeded.</summary>
    TooManyChangeGroups,
}

/// <summary>
/// Parsed ECP response. The kind discriminates which fields are
/// populated; <see cref="Raw"/> is always the full original line.
/// </summary>
/// <param name="Kind">The response kind.</param>
/// <param name="Raw">The raw frame text from the wire (CR already stripped by the framer).</param>
/// <param name="ControlId">For <see cref="EcpResponseKind.ControlValue"/> and <see cref="EcpResponseKind.BadId"/>: the control id.</param>
/// <param name="Display">For <see cref="EcpResponseKind.ControlValue"/>: the display string.</param>
/// <param name="Value">For <see cref="EcpResponseKind.ControlValue"/>: the numeric value.</param>
/// <param name="Position">For <see cref="EcpResponseKind.ControlValue"/>: the [0,1] position.</param>
/// <param name="DesignName">For <see cref="EcpResponseKind.StatusReport"/>: the design name.</param>
/// <param name="DesignId">For <see cref="EcpResponseKind.StatusReport"/>: the design id.</param>
/// <param name="IsPrimary">For <see cref="EcpResponseKind.StatusReport"/>: 1 if this Core is primary in a redundant pair, else 0.</param>
/// <param name="IsActive">For <see cref="EcpResponseKind.StatusReport"/>: 1 if this Core is active, else 0.</param>
internal sealed record EcpResponse(
    EcpResponseKind Kind,
    string Raw,
    string? ControlId = null,
    string? Display = null,
    double Value = 0,
    double Position = 0,
    string? DesignName = null,
    string? DesignId = null,
    int IsPrimary = 0,
    int IsActive = 0)
{
    /// <summary>
    /// Parses a single ECP response line. Returns an
    /// <see cref="EcpResponseKind.Unknown"/> response for any line we
    /// don't recognise — callers typically log Warn and continue.
    /// </summary>
    /// <param name="line">The raw frame text.</param>
    /// <returns>The parsed response.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="line"/> is null.</exception>
    public static EcpResponse Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        // Sentinels — exact-match short forms first.
        switch (line)
        {
            case "cgpa":
                return new EcpResponse(EcpResponseKind.ChangeGroupPollAck, line);
            case "login_required":
                return new EcpResponse(EcpResponseKind.LoginRequired, line);
            case "login_success":
                return new EcpResponse(EcpResponseKind.LoginSuccess, line);
            case "login_failed":
                return new EcpResponse(EcpResponseKind.LoginFailed, line);
            case "core_not_active":
                return new EcpResponse(EcpResponseKind.CoreNotActive, line);
            case "control_read_only":
                return new EcpResponse(EcpResponseKind.ControlReadOnly, line);
            case "too_many_change_groups":
                return new EcpResponse(EcpResponseKind.TooManyChangeGroups, line);
        }

        // Tokenize — quoted strings preserved, numbers as bare tokens.
        List<string> tokens = Tokenize(line);
        if (tokens.Count == 0)
        {
            return new EcpResponse(EcpResponseKind.Unknown, line);
        }

        return tokens[0] switch
        {
            "sr" when tokens.Count >= 5 => ParseStatusReport(line, tokens),
            "cv" when tokens.Count >= 5 => ParseControlValue(line, tokens),
            "bad_id" when tokens.Count >= 2 => new EcpResponse(EcpResponseKind.BadId, line, ControlId: tokens[1]),
            "bad_change_group_handle" => new EcpResponse(EcpResponseKind.BadChangeGroupHandle, line),
            _ => new EcpResponse(EcpResponseKind.Unknown, line),
        };
    }

    private static EcpResponse ParseStatusReport(string raw, List<string> tokens)
    {
        string design = tokens[1];
        string designId = tokens[2];
        if (!int.TryParse(tokens[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int isPrimary))
        {
            return new EcpResponse(EcpResponseKind.Unknown, raw);
        }

        if (!int.TryParse(tokens[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int isActive))
        {
            return new EcpResponse(EcpResponseKind.Unknown, raw);
        }

        return new EcpResponse(
            EcpResponseKind.StatusReport,
            raw,
            DesignName: design,
            DesignId: designId,
            IsPrimary: isPrimary,
            IsActive: isActive);
    }

    private static EcpResponse ParseControlValue(string raw, List<string> tokens)
    {
        string id = tokens[1];
        string display = tokens[2];
        if (!double.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            return new EcpResponse(EcpResponseKind.Unknown, raw);
        }

        if (!double.TryParse(tokens[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double position))
        {
            return new EcpResponse(EcpResponseKind.Unknown, raw);
        }

        return new EcpResponse(
            EcpResponseKind.ControlValue,
            raw,
            ControlId: id,
            Display: display,
            Value: value,
            Position: position);
    }

    /// <summary>
    /// Tokenizes an ECP line, preserving quoted strings as single
    /// tokens with quotes stripped and escapes resolved. Whitespace
    /// outside quotes is the separator.
    /// </summary>
    private static List<string> Tokenize(string line)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < line.Length)
        {
            char ch = line[i];
            if (char.IsWhiteSpace(ch))
            {
                i++;
                continue;
            }

            if (ch == '"')
            {
                int closing = FindClosingQuote(line, i + 1);
                if (closing < 0)
                {
                    // Unterminated quote: take the rest as one token.
                    tokens.Add(EcpQuoting.Unescape(line[(i + 1)..]));
                    return tokens;
                }

                tokens.Add(EcpQuoting.Unescape(line[(i + 1)..closing]));
                i = closing + 1;
            }
            else
            {
                int end = i;
                while (end < line.Length && !char.IsWhiteSpace(line[end]))
                {
                    end++;
                }

                tokens.Add(line[i..end]);
                i = end;
            }
        }

        return tokens;
    }

    private static int FindClosingQuote(string line, int from)
    {
        // Walk forward looking for a non-escaped closing quote. A quote
        // preceded by an odd number of backslashes is escaped.
        for (int i = from; i < line.Length; i++)
        {
            if (line[i] != '"')
            {
                continue;
            }

            int back = i - 1;
            int slashes = 0;
            while (back >= from && line[back] == '\\')
            {
                slashes++;
                back--;
            }

            if (slashes % 2 == 0)
            {
                return i;
            }
        }

        return -1;
    }
}
