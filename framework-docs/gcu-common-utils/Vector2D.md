# Vector2D

**Namespace:** `gcu_common_utils.DataObjects`

Data object for a 2-dimensional vector. Provides static readonly instances for common directional vectors.

---

## Table of Contents

**Static Properties**
- [Up](#up)
- [Down](#down)
- [Left](#left)
- [Right](#right)
- [Zero](#zero)

**Properties**
- [X](#x)
- [Y](#y)

**Methods**
- [Equals(object? obj)](#equalsobject-obj)
- [GetHashCode()](#gethashcode)

---

## Static Properties

### Up

```csharp
public static readonly Vector2D Up
```

A vector with the points `[0, 1]`.

---

### Down

```csharp
public static readonly Vector2D Down
```

A vector with the points `[0, -1]`.

---

### Left

```csharp
public static readonly Vector2D Left
```

A vector with the points `[-1, 0]`.

---

### Right

```csharp
public static readonly Vector2D Right
```

A vector with the points `[1, 0]`.

---

### Zero

```csharp
public static readonly Vector2D Zero
```

A vector with the points `[0, 0]`.

---

## Properties

### X

```csharp
public float X { get; set; }
```

The x-axis point of the vector.

---

### Y

```csharp
public float Y { get; set; }
```

The y-axis point of the vector.

---

## Methods

### Equals(object? obj)

```csharp
public override bool Equals(object? obj)
```

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `obj` | `object?` | The `Vector2D` object to compare against this one. |

**Returns:** `true` if the `X` and `Y` properties of this object match the `X` and `Y` properties of the compared `Vector2D`.

---

### GetHashCode()

```csharp
public override int GetHashCode()
```

Returns a hash code for this instance based on the `X` and `Y` values.

**Returns:** An integer hash code.
