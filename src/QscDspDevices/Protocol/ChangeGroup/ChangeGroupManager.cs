// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.Protocol.ChangeGroup;

/// <summary>
/// Owns the QRC change-group lifecycle. M3 uses one group named
/// <c>qsc-plugin-state</c> at 250 ms AutoPoll for every level/mute
/// control; the QRC protocol caps a connection at four distinct
/// groups, so this manager refuses creation of a fifth.
/// </summary>
/// <remarks>
/// <para>
/// The manager builds JSON-RPC requests but does NOT send them. The
/// caller (post-connect hydration action) is responsible for handing
/// the produced <see cref="JsonRpcRequest"/> objects to the command
/// queue. This split keeps the manager pure and easy to unit-test.
/// </para>
/// <para>
/// AutoPoll responses arrive on the receive thread via
/// <see cref="HandleAutoPollPush"/>, which the dispatcher should call
/// after registering this manager via <see cref="IAutoPollSubscription"/>.
/// The manager parses the <c>Changes</c> array and invokes the
/// registered delta callback for each entry.
/// </para>
/// </remarks>
public sealed class ChangeGroupManager : IAutoPollSubscription
{
    /// <summary>The QRC protocol cap on distinct change-group ids per connection.</summary>
    public const int MaxGroupsPerConnection = 4;

    /// <summary>The fixed group id used by M3+ for the plugin's own state.</summary>
    public const string PluginGroupId = "qsc-plugin-state";

    /// <summary>The default AutoPoll cadence in seconds (250 ms).</summary>
    public const double DefaultAutoPollSeconds = 0.25;

    private readonly string _deviceId;
    private readonly IdGenerator _ids;
    private readonly object _lock = new();
    private readonly Dictionary<string, HashSet<string>> _controlsByGroup = new(StringComparer.Ordinal);
    private readonly Dictionary<long, string> _autoPollIdToGroup = new();
    private Action<ChangeGroupDelta>? _onDelta;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeGroupManager"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id, for log messages.</param>
    /// <param name="ids">The shared monotonic id generator.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public ChangeGroupManager(string deviceId, IdGenerator ids)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(ids);
        _deviceId = deviceId;
        _ids = ids;
    }

    /// <summary>
    /// Gets the count of distinct change groups currently tracked.
    /// </summary>
    public int GroupCount
    {
        get
        {
            lock (_lock)
            {
                return _controlsByGroup.Count;
            }
        }
    }

    /// <summary>
    /// Registers the per-delta callback. Called once at composition time
    /// by the audio-control service that wants to consume AutoPoll pushes.
    /// </summary>
    /// <param name="callback">Invoked once per delta entry.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="callback"/> is null.</exception>
    public void SetDeltaCallback(Action<ChangeGroupDelta> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        lock (_lock)
        {
            _onDelta = callback;
        }
    }

    /// <summary>
    /// Builds (but does not send) a <c>ChangeGroup.AddControl</c> request
    /// adding the supplied control name to the named group. Returns
    /// <c>null</c> when the group is full (the QRC 4-group cap is
    /// reached); the caller logs and skips. The control is recorded as
    /// added on the manager's side regardless of when the wire-level
    /// confirmation arrives — the Core's reply is fire-and-forget.
    /// </summary>
    /// <param name="groupId">The change-group id (free-form string).</param>
    /// <param name="controlName">The QSC control name to subscribe.</param>
    /// <returns>A request to enqueue, or <c>null</c> if the cap was hit.</returns>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public JsonRpcRequest? BuildAddControl(string groupId, string controlName)
    {
        ArgumentNullException.ThrowIfNull(groupId);
        ArgumentNullException.ThrowIfNull(controlName);

        lock (_lock)
        {
            if (!_controlsByGroup.TryGetValue(groupId, out HashSet<string>? controls))
            {
                if (_controlsByGroup.Count >= MaxGroupsPerConnection)
                {
                    Log.Error(
                        _deviceId,
                        $"Refusing to create change group '{groupId}': would exceed QRC's {MaxGroupsPerConnection}-group-per-connection cap.");
                    return null;
                }

                controls = new HashSet<string>(StringComparer.Ordinal);
                _controlsByGroup[groupId] = controls;
            }

            controls.Add(controlName);
        }

        return new JsonRpcRequest
        {
            Id = _ids.Next(),
            Method = "ChangeGroup.AddControl",
            Params = new { Id = groupId, Controls = new[] { controlName } },
        };
    }

    /// <summary>
    /// Builds a <c>ChangeGroup.AutoPoll</c> request for the named group
    /// at the supplied cadence (seconds). Records the request id on
    /// the manager so subsequent dispatcher pushes for that id are
    /// recognized as deltas for the right group.
    /// </summary>
    /// <param name="groupId">The change-group id.</param>
    /// <param name="rateSeconds">The AutoPoll cadence; must be greater than zero.</param>
    /// <returns>The request to enqueue.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="groupId"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="rateSeconds"/> is not positive.</exception>
    /// <exception cref="InvalidOperationException">If the group has not had any controls added yet.</exception>
    public JsonRpcRequest BuildAutoPoll(string groupId, double rateSeconds = DefaultAutoPollSeconds)
    {
        ArgumentNullException.ThrowIfNull(groupId);
        if (rateSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rateSeconds), rateSeconds, "Rate must be positive.");
        }

        long id = _ids.Next();
        lock (_lock)
        {
            if (!_controlsByGroup.ContainsKey(groupId))
            {
                throw new InvalidOperationException($"Change group '{groupId}' has no controls registered; call BuildAddControl first.");
            }

            _autoPollIdToGroup[id] = groupId;
        }

        return new JsonRpcRequest
        {
            Id = id,
            Method = "ChangeGroup.AutoPoll",
            Params = new { Id = groupId, Rate = rateSeconds },
        };
    }

    /// <summary>
    /// Builds a <c>ChangeGroup.Destroy</c> request for the named group
    /// and clears the manager's local state for it. Subsequent AutoPoll
    /// pushes for that id are ignored.
    /// </summary>
    /// <param name="groupId">The change-group id.</param>
    /// <returns>The request to enqueue, or <c>null</c> if the group is unknown.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="groupId"/> is null.</exception>
    public JsonRpcRequest? BuildDestroy(string groupId)
    {
        ArgumentNullException.ThrowIfNull(groupId);
        lock (_lock)
        {
            if (!_controlsByGroup.Remove(groupId))
            {
                return null;
            }

            // Drop any AutoPoll id mappings for this group.
            long[] toRemove = _autoPollIdToGroup
                .Where(kv => string.Equals(kv.Value, groupId, StringComparison.Ordinal))
                .Select(kv => kv.Key)
                .ToArray();
            foreach (long id in toRemove)
            {
                _autoPollIdToGroup.Remove(id);
            }
        }

        return new JsonRpcRequest
        {
            Id = _ids.Next(),
            Method = "ChangeGroup.Destroy",
            Params = new { Id = groupId },
        };
    }

    /// <summary>
    /// Returns a snapshot of currently-registered control names for a
    /// group. Test diagnostics; production code uses the build* methods.
    /// </summary>
    /// <param name="groupId">The change-group id.</param>
    /// <returns>A copy of the control-name set, or empty if unknown.</returns>
    public IReadOnlyList<string> GetSubscribedControls(string groupId)
    {
        ArgumentNullException.ThrowIfNull(groupId);
        lock (_lock)
        {
            if (!_controlsByGroup.TryGetValue(groupId, out HashSet<string>? controls))
            {
                return Array.Empty<string>();
            }

            return controls.ToArray();
        }
    }

    /// <inheritdoc />
    public void OnPush(JsonRpcResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        HandleAutoPollPush(response);
    }

    /// <summary>
    /// Parses an AutoPoll response's <c>Changes</c> array and invokes
    /// the registered delta callback once per entry.
    /// </summary>
    /// <param name="response">The dispatcher-routed AutoPoll push.</param>
    public void HandleAutoPollPush(JsonRpcResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        Action<ChangeGroupDelta>? callback;
        lock (_lock)
        {
            callback = _onDelta;
        }

        if (callback is null)
        {
            return;
        }

        // Error response on the AutoPoll id surface (e.g. -32604 Standby
        // or 5 ChangeGroupsExhausted) — the manager would otherwise
        // silently believe the group is alive. Log and return; the next
        // hydration cycle will re-attempt subscription. Cache-side
        // reconciliation is handled by AudioControlService when the
        // next successful AutoPoll arrives.
        if (response.IsError)
        {
            Log.Warn(
                _deviceId,
                $"AutoPoll subscription returned error {response.Error?.Code} '{response.Error?.Message}'. The change group is no longer believed subscribed.");
            return;
        }

        // The Result shape is `{ Id: "...", Changes: [...] }`. Indexing
        // into a JValue (e.g., a string or number Result) throws
        // InvalidOperationException — guard via a JObject type check
        // so a malformed AutoPoll response doesn't crash the receive
        // thread (#23).
        if (response.Result is not JObject result)
        {
            return;
        }

        if (result["Changes"] is not JArray changes)
        {
            return;
        }

        foreach (JToken entry in changes)
        {
            if (entry is not JObject obj)
            {
                continue;
            }

            string? name = obj["Name"]?.ToString();
            JToken? value = obj["Value"];
            if (name is null || value is null)
            {
                continue;
            }

            JToken? stringValue = obj["String"];
            double? position = obj["Position"]?.Type == JTokenType.Float || obj["Position"]?.Type == JTokenType.Integer
                ? obj["Position"]!.ToObject<double>()
                : null;

            callback(new ChangeGroupDelta(name, value, stringValue, position));
        }
    }
}
