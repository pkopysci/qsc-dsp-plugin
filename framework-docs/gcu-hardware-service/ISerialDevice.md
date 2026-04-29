# ISerialDevice

**Namespace:** `gcu_hardware_service.Communication`

Required features for any device that uses serial communications for control.

---

## Table of Contents

**Methods**
- [SetSerialControlPort(ISerialPort port)](#setserialcontrolportiserialport-port)

---

## Methods

### SetSerialControlPort(ISerialPort port)

```csharp
void SetSerialControlPort(ISerialPort port)
```

Sets the internal com port used to send and receive data.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `port` | `ISerialPort` | The standard serial port control implementation. |
