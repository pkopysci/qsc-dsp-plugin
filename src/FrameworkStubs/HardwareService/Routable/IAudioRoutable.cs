// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-hardware-service/IAudioRoutable.md
// Stub for the real type shipped in: gcu-hardware-service 4.3.4
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

using gcu_common_utils.GenericEventArgs;

namespace gcu_hardware_service.Routable;

public interface IAudioRoutable
{
    event EventHandler<GenericDualEventArgs<string, string>> AudioRouteChanged;

    string GetCurrentAudioSource(string outputId);

    void RouteAudio(string sourceId, string outputId);

    void ClearAudioRoute(string outputId);
}
