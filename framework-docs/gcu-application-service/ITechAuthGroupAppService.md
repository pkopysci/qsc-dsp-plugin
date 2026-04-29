# ITechAuthGroupAppService

**Namespace:** `gcu_application_service`

Interface for adding technician-level access during runtime. Used to broadcast requests that lock all user interfaces not tagged as a technician control point.

---

## Table of Contents

**Events**
- [NonTechLockoutStateChangeRequest](#nontech-lockoutstatechangerequest)

---

## Events

### NonTechLockoutStateChangeRequest

```csharp
event EventHandler<GenericSingleEventArgs<bool>>? NonTechLockoutStateChangeRequest
```

Triggered when the application service requires non-technician interfaces to be locked or unlocked. The event arg is `true` to lock and `false` to unlock.
