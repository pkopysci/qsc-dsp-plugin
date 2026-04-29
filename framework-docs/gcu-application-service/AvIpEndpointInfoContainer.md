# AvIpEndpointInfoContainer

**Namespace:** `gcu_application_service.AvRouting`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data object representing a single AV-over-IP endpoint (encoder or decoder).

---

## Table of Contents

**Properties**
- [EmptyAvIpEndpoint](#emptyavipendpoint)
- [HostAvrId](#hostavrid)
- [IsDecoder](#isdecoder)

**Constructors**
- [AvIpEndpointInfoContainer(...)](#avipendpointinfocontainer-1)

---

## Properties

### EmptyAvIpEndpoint

```csharp
public static readonly AvIpEndpointInfoContainer EmptyAvIpEndpoint
```

Default/empty AV-over-IP endpoint. Used when an endpoint query does not find a match.

---

### HostAvrId

```csharp
public string HostAvrId { get; init; }
```

The unique ID of the AV-over-IP AVR that controls this endpoint.

---

### IsDecoder

```csharp
public bool IsDecoder { get; init; }
```

`true` if this endpoint is a decoder; `false` if it is an encoder.

---

## Constructors

### AvIpEndpointInfoContainer(...)

```csharp
public AvIpEndpointInfoContainer(string id, string label, string icon, List<string> tags, bool isOnline = false)
```

Instantiates a new instance of `AvIpEndpointInfoContainer`.

**Parameters**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `id` | `string` | | The unique ID of the device. Used for internal referencing. |
| `label` | `string` | | The user-friendly name of the device. |
| `icon` | `string` | | The image tag used for referencing the UI icon. |
| `tags` | `List<string>` | | A collection of custom tags used by the subscribed service. |
| `isOnline` | `bool` | `false` | `true` = device is currently connected; `false` = device offline. |
