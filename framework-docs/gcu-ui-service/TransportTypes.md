# TransportTypes

**Namespace:** `gcu_ui_service.Utility`

Enumeration of transport control command types. Used to identify which transport action a UI plugin should trigger on the application service. Maps one-to-one with the transport commands defined in the `ITransportControlApp` interface.

---

## Values

| Value | Description |
|-------|-------------|
| `Unknown` | Unrecognized or unset transport command. |
| `PowerOn` | Power the transport device on. |
| `PowerOff` | Power the transport device off. |
| `PowerToggle` | Toggle the power state of the transport device. |
| `Dial` | Dial a number or channel. |
| `Dash` | Send a dash/separator character during channel entry. |
| `ChannelUp` | Increment the current channel. |
| `ChannelDown` | Decrement the current channel. |
| `ChannelStop` | Stop channel up/down scanning. |
| `PageUp` | Navigate one page up in a menu. |
| `PageDown` | Navigate one page down in a menu. |
| `PageStop` | Stop page up/down scrolling. |
| `Guide` | Open the channel guide or program guide. |
| `Menu` | Open the device menu. |
| `Info` | Display program or device information. |
| `Exit` | Exit the current menu or dialog. |
| `Back` | Navigate back to the previous screen or menu. |
| `Play` | Begin playback. |
| `Pause` | Pause current playback. |
| `Stop` | Stop current playback. |
| `Record` | Begin recording. |
| `ScanForward` | Fast-scan forward through content. |
| `ScanReverse` | Fast-scan backward through content. |
| `SkipForward` | Skip to the next chapter, track, or segment. |
| `SkipReverse` | Skip to the previous chapter, track, or segment. |
| `NavUp` | Navigate up in the on-screen menu. |
| `NavDown` | Navigate down in the on-screen menu. |
| `NavLeft` | Navigate left in the on-screen menu. |
| `NavRight` | Navigate right in the on-screen menu. |
| `NavStop` | Stop directional navigation. |
| `Red` | Press the red color button. |
| `Green` | Press the green color button. |
| `Yellow` | Press the yellow color button. |
| `Blue` | Press the blue color button. |
| `Select` | Confirm the current selection. |
| `Previous` | Return to the previously viewed channel or content. |
| `DishNet` | Trigger the Dish Network-specific button. |
| `Replay` | Replay the last few seconds of content. |
| `Eject` | Eject the disc or media. |
