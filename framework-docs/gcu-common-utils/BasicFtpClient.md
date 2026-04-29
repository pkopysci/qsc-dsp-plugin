# BasicFtpClient

**Namespace:** `gcu_common_utils.NetComs`

**Implements:** `IDisposable`

Utility class for checking remote SFTP servers and downloading files. Supports authentication via username/password or SSH private key. All operations are asynchronous and results are communicated through events.

---

## Table of Contents

**Constructors**
- [BasicFtpClient(string host, string username, string password)](#basicftpclientstring-host-string-username-string-password)
- [BasicFtpClient(string host, string username, PrivateKeyFile sshKey)](#basicftpclientstring-host-string-username-privatekeyfile-sshkey)

**Events**
- [FileQueryComplete](#filequerycomplete)
- [ErrorOccurred](#erroroccurred)
- [DownloadComplete](#downloadcomplete)

**Properties**
- [LastErrorMessage](#lasterrormessage)
- [FilesNamesReceived](#filesnamesreceived)
- [IsConnected](#isconnected)

**Methods**
- [Connect()](#connect)
- [Disconnect()](#disconnect)
- [QueryFileNames(string remoteDirectory)](#queryfilenamesstring-remotedirectory)
- [DownloadFile(string remoteFilePath, string localFilePath)](#downloadfilestring-remotefilepath-string-localfilepath)
- [Dispose()](#dispose)

---

## Constructors

### BasicFtpClient(string host, string username, string password)

```csharp
public BasicFtpClient(string host, string username, string password)
```

Creates an instance of `BasicFtpClient` using a username and password for SFTP connection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `host` | `string` | The IP address or hostname of the SFTP server. |
| `username` | `string` | The username used to log into the SFTP server. |
| `password` | `string` | The password used to log into the SFTP server. |

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If any argument is null or empty. |

---

### BasicFtpClient(string host, string username, PrivateKeyFile sshKey)

```csharp
public BasicFtpClient(string host, string username, PrivateKeyFile sshKey)
```

Creates an instance of `BasicFtpClient` using a username and SSH private key for SFTP connection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `host` | `string` | The IP address or hostname of the SFTP server. |
| `username` | `string` | The username used to log into the SFTP server. |
| `sshKey` | `PrivateKeyFile` | The private key used to authenticate with the server. |

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `host` or `username` is null or empty. |
| `ArgumentNullException` | If `sshKey` is null. |

---

## Events

### FileQueryComplete

```csharp
public event EventHandler<EventArgs>? FileQueryComplete
```

Triggered when a response is received from the server after calling `QueryFileNames()` successfully. Response data will be stored in the `FilesNamesReceived` property.

---

### ErrorOccurred

```csharp
public event EventHandler<EventArgs>? ErrorOccurred
```

Triggered whenever there is an error querying, downloading, or connecting to the SFTP server. Error information will be stored in the `LastErrorMessage` property.

---

### DownloadComplete

```csharp
public event EventHandler<EventArgs>? DownloadComplete
```

Triggered when a file download from the remote server has completed successfully.

---

## Properties

### LastErrorMessage

```csharp
public string LastErrorMessage { get; }
```

Error information on the last error event. Set whenever `ErrorOccurred` is raised.

---

### FilesNamesReceived

```csharp
public List<string> FilesNamesReceived { get; }
```

A collection of file names (including extension) that were in the directory provided in the most recent call to `QueryFileNames()`. This will be empty if there are no files or `QueryFileNames()` has not been called.

---

### IsConnected

```csharp
public bool IsConnected { get; }
```

Gets a value indicating whether there is an active connection with the remote server.

---

## Methods

### Connect()

```csharp
public void Connect()
```

Attempts to connect to the remote SFTP server. Does nothing if the client is already connected. Raises `ErrorOccurred` and sets `LastErrorMessage` if the connection fails.

---

### Disconnect()

```csharp
public void Disconnect()
```

Attempts to disconnect from the remote server. Does nothing if there is no active client connection.

---

### QueryFileNames(string remoteDirectory)

```csharp
public void QueryFileNames(string remoteDirectory)
```

Queries the remote server for the names and extensions of all files in the target directory. Subdirectories are excluded. Results are stored in `FilesNamesReceived` and `FileQueryComplete` is raised on success. Does nothing if there is no active connection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `remoteDirectory` | `string` | The full directory path on the remote server. |

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `remoteDirectory` is null or empty. |

---

### DownloadFile(string remoteFilePath, string localFilePath)

```csharp
public void DownloadFile(string remoteFilePath, string localFilePath)
```

Downloads the given file from the remote server to the provided local path. Raises `ErrorOccurred` if the file does not exist or an error occurs. Raises `DownloadComplete` on success. Does nothing if there is no active connection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `remoteFilePath` | `string` | The full path to the file on the remote server, including file extension. |
| `localFilePath` | `string` | The full local path where the file will be saved, including file extension. |

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If either argument is null or empty. |

---

### Dispose()

```csharp
public void Dispose()
```

Releases all resources used by the `BasicFtpClient`. Disconnects from the server if currently connected before disposing the underlying SFTP client.
