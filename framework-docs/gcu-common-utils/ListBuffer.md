# ListBuffer\<T\>

**Namespace:** `gcu_common_utils.DataObjects`

A blocking buffer implementation using a `List<T>` object for data management. This blocks any read/write actions while performing any operation, ensuring thread-safe access.

---

## Table of Contents

**Properties**
- [ReadWriteFailureReason](#readwritefailurereason)

**Methods**
- [AddItem(T item)](#additemet-item)
- [AddItems(IEnumerable\<T\> items)](#additemsienumerablet-items)
- [CheckExists(T item)](#checkexistst-item)
- [CheckExists(IList\<T\> items)](#checkexistsilistt-items)
- [GetLength()](#getlength)
- [RemoveByLength(int length)](#removebylengthint-length)
- [RemoveByDelimiter(T delimiter)](#removebydelimitert-delimiter)
- [PeakByLength(int length)](#peakbylengthint-length)

---

## Properties

### ReadWriteFailureReason

```csharp
public string ReadWriteFailureReason { get; }
```

Contains the exception message as of the last read or write failure. This will be an empty string if the last method call was a success or no read/write has been attempted yet.

---

## Methods

### AddItem(T item)

```csharp
public bool AddItem(T item)
```

Try to add an item to the current collection. This action is blocked until the buffer is released and safe to write to.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `item` | `T` | The object to add to the collection. Cannot be null. |

**Returns:** `true` if the object was added successfully; `false` if not.

**Remarks:** On a failed add, the exception message is assigned to `ReadWriteFailureReason`. If the add was successful, `ReadWriteFailureReason` is set to the empty string.

---

### AddItems(IEnumerable\<T\> items)

```csharp
public bool AddItems(IEnumerable<T> items)
```

Try to add a collection of items to the list buffer. On a failure a message is written to `ReadWriteFailureReason`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `items` | `IEnumerable<T>` | The collection of items to add. |

**Returns:** `true` if the items were successfully added to the collection; `false` otherwise.

---

### CheckExists(T item)

```csharp
public bool CheckExists(T item)
```

Check the current list buffer to see if the given item is already in the collection. This calls the `List.Contains()` method. On any exception, the message is written to `ReadWriteFailureReason` and `false` is returned.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `item` | `T` | The object that will be checked against the list buffer. |

**Returns:** `true` if the object exists in the collection; `false` otherwise.

**Remarks:** If the evaluation successfully returns `false` (no object in collection), `ReadWriteFailureReason` will be set to the empty string.

---

### CheckExists(IList\<T\> items)

```csharp
public bool CheckExists(IList<T> items)
```

Check to see if there is a matching sequence of elements in the internal buffer. The order of elements must match for this to return `true`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `items` | `IList<T>` | The sequence of items to compare. |

**Returns:** `true` if there is a matching sequence in the buffer; `false` otherwise.

---

### GetLength()

```csharp
public int GetLength()
```

Get the current number of elements in the buffer.

**Returns:** The current number of elements in the buffer.

---

### RemoveByLength(int length)

```csharp
public List<T> RemoveByLength(int length)
```

Try to remove a section of the buffer by a given length. This will start at the first item in the collection (index 0) and remove all items up to the supplied length. A failure message is written to `ReadWriteFailureReason` if `length` is less than zero or greater than the number of items in the buffer.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `length` | `int` | The number of items to remove from the buffer. Cannot be less than zero or greater than the total number of items in the buffer. |

**Returns:** A list containing all items removed, or an empty list if an error is encountered.

---

### RemoveByDelimiter(T delimiter)

```csharp
public List<T> RemoveByDelimiter(T delimiter)
```

Remove items from the buffer, starting at index 0, up to the first item that matches the supplied delimiter. The evaluation is conducted using the `List<T>.IndexOf()` method.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `delimiter` | `T` | The item to look for when removing. |

**Returns:** A list of items up to the first occurrence of the delimiter. Returns an empty list if there is no match or if an error is encountered.

**Remarks:** Writes a message to `ReadWriteFailureReason` if any error is encountered.

---

### PeakByLength(int length)

```csharp
public T[] PeakByLength(int length)
```

Get a copy of the elements in the buffer without removing them.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `length` | `int` | The total number of elements to return. |

**Returns:** The number of elements up to and including the element at the length value. Returns an empty array if length is less than zero.
