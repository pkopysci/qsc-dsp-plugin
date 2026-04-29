# ISystemPowerApp

**Namespace:** `gcu_application_service.SystemPower`

Common properties and methods for AV system power state management.

---

## Table of Contents

**Events**
- [SystemStateChanged](#systemstatechanged)

**Properties**
- [CurrentSystemState](#currentsystemstate)
- [AutoShutdownEnabled](#autoshutdownenabled)

**Methods**
- [SetActive()](#setactive)
- [SetStandby()](#setstandby)
- [AutoShutdownEnable()](#autoshutdownenable)
- [AutoShutdownDisable()](#autoshutdowndisable)
- [SetAutoShutdownTime(int hour, int minute)](#setautoshutdowntimeint-hour-int-minute)

---

## Events

### SystemStateChanged

```csharp
event EventHandler SystemStateChanged
```

Triggered whenever the system transitions between power states (standby → active or active → standby).

---

## Properties

### CurrentSystemState

```csharp
bool CurrentSystemState { get; }
```

`true` if the system is in the active state; `false` if in standby.

---

### AutoShutdownEnabled

```csharp
bool AutoShutdownEnabled { get; }
```

`true` if the auto-shutdown feature is enabled; `false` if disabled.

---

## Methods

### SetActive()

```csharp
void SetActive()
```

Request to transition to the active state and trigger any startup automation.

---

### SetStandby()

```csharp
void SetStandby()
```

Request to transition to the standby state and trigger any shutdown automation.

---

### AutoShutdownEnable()

```csharp
void AutoShutdownEnable()
```

Enable the automatic shutdown feature.

---

### AutoShutdownDisable()

```csharp
void AutoShutdownDisable()
```

Disable the automatic shutdown feature.

---

### SetAutoShutdownTime(int hour, int minute)

```csharp
void SetAutoShutdownTime(int hour, int minute)
```

Configure the time at which the system should automatically shut down if the feature is enabled.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `hour` | `int` | The hour when the system should shut down (24-hour format). |
| `minute` | `int` | The minute when the system should shut down (0–59). |
