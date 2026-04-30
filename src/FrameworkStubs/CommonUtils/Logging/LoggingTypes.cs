// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-common-utils/LoggingTypes.md
// Stub for the real types shipped in: gcu-common-utils 4.3.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

namespace gcu_common_utils.Logging.LoggingTypes;

public enum LogServiceTypes
{
    ControlSystem,
    Common,
    Configuration,
    Domain,
    Hardware,
    Application,
    Presentation,
    UiPlugin,
}

public enum LogDeviceTypes
{
    NotApplicable,
    Display,
    Avr,
    Dsp,
    Ctv,
    Bluray,
    VideoWall,
    Lighting,
    Camera,
    Endpoint,
    UserInterface,
}
