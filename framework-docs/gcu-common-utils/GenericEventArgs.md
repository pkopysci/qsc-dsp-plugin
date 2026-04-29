# Generic Event Args

**Namespace:** `gcu_common_utils.GenericEventArgs`

A set of generic `EventArgs` classes for passing typed data during application events. Use these classes instead of creating custom `EventArgs` subclasses when one, two, or three discrete data values need to be transmitted with an event.

---

## Table of Contents

**Classes**
- [GenericSingleEventArgs\<T\>](#genericsingleeventargst)
- [GenericDualEventArgs\<T1, T2\>](#genericdualeventargst1-t2)
- [GenericTrippleEventArgs\<T1, T2, T3\>](#generictripleeventargst1-t2-t3)

---

## GenericSingleEventArgs\<T\>

```csharp
public class GenericSingleEventArgs<T>(T arg) : EventArgs
```

Generic arguments package for sending information during application events that require a single data value.

**Type Parameters**

| Name | Description |
|------|-------------|
| `T` | The type of data being sent during an event. |

**Constructor Parameters**

| Name | Type | Description |
|------|------|-------------|
| `arg` | `T` | The data object associated with the triggering event. |

### Properties

#### Arg

```csharp
public T Arg { get; }
```

Gets a value representing the data sent during the event.

---

## GenericDualEventArgs\<T1, T2\>

```csharp
public class GenericDualEventArgs<T1, T2>(T1 arg1, T2 arg2) : EventArgs
```

Generic arguments package for sending information during application events which require two discrete bits of data.

**Type Parameters**

| Name | Description |
|------|-------------|
| `T1` | The type of data being sent during an event for `Arg1`. |
| `T2` | The type of data being sent during an event for `Arg2`. |

**Constructor Parameters**

| Name | Type | Description |
|------|------|-------------|
| `arg1` | `T1` | The first data object supplied when the event was thrown. |
| `arg2` | `T2` | The second data object supplied when the event was thrown. |

### Properties

#### Arg1

```csharp
public T1 Arg1 { get; }
```

Gets the first data object supplied when the event was thrown.

#### Arg2

```csharp
public T2 Arg2 { get; }
```

Gets the second data object supplied when the event was thrown.

---

## GenericTrippleEventArgs\<T1, T2, T3\>

```csharp
public class GenericTrippleEventArgs<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3) : EventArgs
```

Generic arguments package for sending information during application events which require three discrete bits of data.

**Type Parameters**

| Name | Description |
|------|-------------|
| `T1` | The type of data being sent during an event for `Arg1`. |
| `T2` | The type of data being sent during an event for `Arg2`. |
| `T3` | The type of data being sent during an event for `Arg3`. |

**Constructor Parameters**

| Name | Type | Description |
|------|------|-------------|
| `arg1` | `T1` | The first data object supplied when the event was thrown. |
| `arg2` | `T2` | The second data object supplied when the event was thrown. |
| `arg3` | `T3` | The third data object supplied when the event was thrown. |

### Properties

#### Arg1

```csharp
public T1 Arg1 { get; }
```

Gets the first data object supplied when the event was thrown.

#### Arg2

```csharp
public T2 Arg2 { get; }
```

Gets the second data object supplied when the event was thrown.

#### Arg3

```csharp
public T3 Arg3 { get; }
```

Gets the third data object supplied when the event was thrown.
