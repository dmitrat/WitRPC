# OutWit.Communication.Client.Blazor

## Overview

**OutWit.Communication.Client.Blazor** is a Blazor WebAssembly channel factory for **WitRPC** communication over WebSocket.
It bridges the gap between the browser environment and the WitRPC server by handling:

- **RSA/AES encryption** entirely in the browser via the Web Crypto API (optional, enabled by default)
- **Automatic reconnection** with exponential backoff after disconnects
- **Retry policies** for transient failures (server errors, timeouts, I/O)
- **Authentication integration** -- reconnects on sign-in, disconnects on sign-out (optional)
- **One-line DI registration** with optional configuration

All of this is exposed through a single `IChannelFactory` interface that lazily establishes a WebSocket connection and hands back typed service proxies.

#### Install

```ps1
Install-Package OutWit.Communication.Client.Blazor
```

or

```bash
dotnet add package OutWit.Communication.Client.Blazor
```

## Target Frameworks

`net10.0` � `net9.0` � `net8.0` � `net7.0` � `net6.0`

## Getting Started

### 1. Register Services

In `Program.cs`, call `AddWitRpcChannel()` on the service collection. The method registers `ChannelTokenProvider` and `IChannelFactory` in a single call:

```csharp
using OutWit.Communication.Client.Blazor;

// Minimal -- all defaults (encryption + reconnect + retry enabled)
builder.Services.AddWitRpcChannel();

// Custom endpoint and timeout
builder.Services.AddWitRpcChannel(options =>
{
    options.ApiPath = "api";          // WebSocket endpoint path (default: "api")
    options.TimeoutSeconds = 15;      // Connection & request timeout (default: 10)
});
```

#### Connecting to an External Server

By default, the WebSocket URL is derived from `NavigationManager.BaseUri` (same origin as the Blazor app).
To connect to a different server, set `BaseUrl`:

```csharp
builder.Services.AddWitRpcChannel(options =>
{
    options.BaseUrl = "https://api.example.com";  // external WitRPC server
    options.ApiPath = "rpc";                       // -> wss://api.example.com/rpc
});
```

#### Disabling Encryption

Encryption is enabled by default. To disable it (e.g. for development or when TLS is sufficient):

```csharp
builder.Services.AddWitRpcChannel(options =>
{
    options.UseEncryption = false;  // no RSA/AES, uses plain encryptor
});
```

#### Tuning Reconnect & Retry

Both policies are enabled by default with sensible values. Override individual settings as needed:

```csharp
builder.Services.AddWitRpcChannel(options =>
{
    options.Reconnect!.MaxAttempts = 5;              // give up after 5 attempts (0 = unlimited)
    options.Reconnect!.InitialDelay = TimeSpan.FromSeconds(2);

    options.Retry!.MaxRetries = 5;
    options.Retry!.MaxDelay = TimeSpan.FromSeconds(30);
});
```

#### Disabling Reconnect and/or Retry

Set the corresponding property to `null` to disable a policy entirely:

```csharp
builder.Services.AddWitRpcChannel(options =>
{
    options.Reconnect = null;   // no auto-reconnect
    options.Retry = null;       // no per-call retries
});
```

You can disable them independently -- for example, keep auto-reconnect but disable retries:

```csharp
builder.Services.AddWitRpcChannel(options =>
{
    options.Retry = null;  // retries off, reconnect stays with defaults
});
```

#### Minimal Configuration (No Encryption, No Auth, No Resilience)

For the simplest possible setup -- useful during development:

```csharp
builder.Services.AddWitRpcChannel(options =>
{
    options.UseEncryption = false;
    options.Reconnect = null;
    options.Retry = null;
});
```

> **Note**: Blazor's `IAccessTokenProvider` and `AuthenticationStateProvider` are resolved optionally.
> If they are not registered in the DI container (i.e. no Blazor auth packages), the factory works
> without authentication -- no exceptions are thrown.

#### Advanced Builder Customization

For settings not covered by the typed options, use the `ConfigureClient` escape hatch.
It is invoked **after** all typed settings have been applied, so it can override or extend anything:

```csharp
builder.Services.AddWitRpcChannel(options =>
{
    options.ConfigureClient = builder =>
    {
        builder.WithJson();           // switch serialization from MemoryPack to JSON
    };
});
```

The registered service lifetimes are:

| Service | Lifetime | Why |
|---------|----------|-----|
| `ChannelFactoryOptions` | Singleton | Immutable configuration, shared across scopes |
| `ChannelTokenProvider` | Scoped | Bridges the scoped Blazor `IAccessTokenProvider` |
| `IChannelFactory` ? `ChannelFactory` | Scoped | Manages one WebSocket connection per circuit |

> `EncryptorClientWeb` is **not** registered in DI. When `UseEncryption = true`, a fresh instance is created per connection inside `ChannelFactory`, ensuring clean RSA/AES key material for each session.

### 2. Obtain a Service Proxy

Inject `IChannelFactory` into any component or service and call `GetServiceAsync<T>()`. The factory lazily connects on first use:

```csharp
@inject IChannelFactory ChannelFactory

@code {
    private IMyService? _service;

    protected override async Task OnInitializedAsync()
    {
        _service = await ChannelFactory.GetServiceAsync<IMyService>();
    }

    private async Task DoWork()
    {
        var result = await _service!.SomethingAsync();
    }
}
```

### 3. Force Reconnection

If you need to re-establish the connection (e.g. after a configuration change or server restart):

```csharp
await ChannelFactory.ReconnectAsync();
```

This tears down the existing `WitClient`, creates a new one with fresh encryption keys (if enabled), and reconnects.

## Connection Lifecycle

`ChannelFactory` manages the WebSocket connection through a `SemaphoreSlim` gate to ensure thread safety.

```
                 GetServiceAsync<T>()
                        |
                        v
              +--------------------+
              | Client == null?    |----- no ----> return proxy
              +--------------------+
                     | yes
                     v
              +----------------+
              |  m_gate.Wait   |     (semaphore -- one connect at a time)
              +----------------+
                     |
              +------------------------+
              | UseEncryption?         |
              |  yes -> new            |     per-connection EncryptorClientWeb
              |  EncryptorClientWeb    |     generates RSA-2048 via JS
              |     .InitAsync()       |     exports JWK -> byte[]
              |  no  -> skip           |     uses default plain encryptor
              +------------------------+
                     |
              +------------------------+
              |  WitClientBuilder      |
              |    .Build(options)     |
              |  -- WebSocket(url)     |     ws:// or wss:// from BaseUrl ?? NavigationManager
              |  -- MemoryPack         |     binary serialization
              |  -- Encryptor?         |     EncryptorClientWeb (if enabled)
              |  -- TokenProvider      |     ChannelTokenProvider
              |  -- AutoReconnect      |     configurable or null to disable
              |  -- RetryPolicy        |     configurable or null to disable
              +------------------------+
                     |
              +------------------------+
              |  client.ConnectAsync   |     with configured timeout
              +------------------------+
                     |
              +--------------------+
              |  m_gate.Release    |
              +--------------------+
```

The WebSocket URL is derived from `Options.BaseUrl` (if set) or `NavigationManager.BaseUri` (same origin):

```
https://example.com/           ->  wss://example.com/api       (BaseUrl = null, same origin)
http://localhost:5000/         ->  ws://localhost:5000/api      (BaseUrl = null, same origin)
https://api.example.com        ->  wss://api.example.com/api   (BaseUrl = "https://api.example.com")
```

## Authentication Integration

`ChannelFactory` subscribes to `AuthenticationStateProvider.AuthenticationStateChanged` during construction (only if `AuthenticationStateProvider` is available in DI):

| Event | Action |
|-------|--------|
| User signs in (`IsAuthenticated == true`) | Calls `ReconnectAsync()` -- tears down old connection, creates new one with fresh token |
| User signs out (`IsAuthenticated == false`) | Disconnects and sets `Client = null` |
| Factory disposed | Unsubscribes from the event, disconnects, disposes semaphore |

`ChannelTokenProvider` obtains access tokens from Blazor's `IAccessTokenProvider` and makes them available to the WitRPC transport layer:

```csharp
// Internally:
var result = await TokenProvider.RequestAccessToken(new AccessTokenRequestOptions());
if (result.TryGetToken(out var token))
    return token.Value;
```

If `IAccessTokenProvider` is not registered (no Blazor auth), an empty string is returned.
If the token request fails, an empty string is returned and an error is logged.

## Resilience

Both policies are **enabled by default** (`new()`) and can be individually disabled by setting the corresponding property to `null`.

### Auto-Reconnect (`ChannelReconnectOptions`)

Fires whenever the underlying WebSocket disconnects unexpectedly.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxAttempts` | `int` | `0` (unlimited) | Maximum reconnection attempts. `0` = unlimited. |
| `InitialDelay` | `TimeSpan` | 1 s | Delay before the first reconnection attempt. |
| `MaxDelay` | `TimeSpan` | 2 min | Upper bound for the backoff delay. |
| `BackoffMultiplier` | `double` | 2.0 | Multiplier applied after each failed attempt. |
| `ReconnectOnDisconnect` | `bool` | `true` | Auto-reconnect after unexpected disconnect. |

Disable: `options.Reconnect = null;`

### Retry Policy (`ChannelRetryOptions`)

Individual RPC calls are retried on transient failures.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxRetries` | `int` | 3 | Maximum retry attempts before giving up. |
| `InitialDelay` | `TimeSpan` | 500 ms | Delay before the first retry. |
| `MaxDelay` | `TimeSpan` | 10 s | Upper bound for the backoff delay. |
| `BackoffMultiplier` | `double` | 2.0 | Multiplier applied after each failed retry. |

Additionally, the following are applied automatically and can be overridden via `ConfigureClient`:

| Setting | Value |
|---------|-------|
| `BackoffType` | `Exponential` |
| Retried statuses | `InternalServerError` |
| Retried exceptions | `TimeoutException`, `IOException` |

Disable: `options.Retry = null;`

## Encryption

Encryption is controlled by `UseEncryption` (default: `true`). When enabled, the library implements end-to-end encryption between the browser and the WitRPC server using a two-phase handshake. When disabled, the default plain encryptor is used (no JS interop calls).

### Phase 1 -- RSA Key Exchange

1. `EncryptorClientWeb.InitAsync()` calls `cryptoInterop.generateKeys(2048)` via JS interop
2. The browser generates an RSA-OAEP key pair using the Web Crypto API
3. Public and private keys are exported in JWK format
4. `DualNameJsonConverter` re-serializes the keys, converting Base64Url to standard Base64 and mapping JWK field names (`n`, `e`, `d`, `p`, `q`, `dp`, `dq`, `qi`) to .NET-compatible property names (`mod`, `exp`, `d`, `p`, `q`, `dp`, `dq`, `iq`)
5. The public key is sent to the server, which uses it to encrypt a symmetric AES key

### Phase 2 -- AES Symmetric Encryption

1. The server encrypts an AES-CBC key + IV with the client's RSA public key
2. `EncryptorClientWeb.DecryptRsa()` decrypts it in the browser
3. `ResetAes()` stores the symmetric key and IV
4. All subsequent communication uses `Encrypt()` / `Decrypt()` with AES-CBC

### RSA Parameters Mapping

| JWK name | `JsonPropertyName` | C# property | Description |
|----------|---------------------|-------------|-------------|
| `n` | `"n"` | `mod` | RSA modulus |
| `e` | `"e"` | `exp` | Public exponent |
| `d` | `"d"` | `d` | Private exponent |
| `p` | `"P"` | `p` | First prime factor |
| `q` | `"q"` | `q` | Second prime factor |
| `dp` | `"dp"` | `dp` | d mod (p - 1) |
| `dq` | `"dq"` | `dq` | d mod (q - 1) |
| `qi` | `"qi"` | `iq` | CRT coefficient |

## JavaScript Interop

The library ships `wwwroot/js/cryptoInterop.js` which is automatically available as a static web asset. It exposes the following functions on `window.cryptoInterop`:

| Function | Parameters | Returns | Description |
|----------|------------|---------|-------------|
| `generateKeys` | `keySize` (int) | `void` | Generates RSA-OAEP key pair with SHA-256 |
| `getPublicKey` | -- | `string` (JWK JSON) | Exports public key |
| `getPrivateKey` | -- | `string` (JWK JSON) | Exports private key |
| `decryptRSA` | `encryptedBase64` (string) | `string` (Base64) | RSA-OAEP decryption |
| `encryptAes` | `base64Key`, `base64Iv`, `base64Data` | `string` (Base64) | AES-CBC encryption |
| `decryptAes` | `base64Key`, `base64Iv`, `base64EncryptedData` | `string` (Base64) | AES-CBC decryption |

> **Note**: The script uses `window.crypto.subtle` which requires a [secure context](https://developer.mozilla.org/en-US/docs/Web/Security/Secure_Contexts) (HTTPS or localhost). These functions are only called when `UseEncryption = true`.

## Configuration Reference

`ChannelFactoryOptions` provides the following settings:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ApiPath` | `string` | `"api"` | WebSocket endpoint path relative to the application base URL. Leading `/` is trimmed automatically. |
| `TimeoutSeconds` | `int` | `10` | Timeout in seconds for both the initial connection and individual RPC requests. |
| `UseEncryption` | `bool` | `true` | Whether to use RSA/AES encryption via the Web Crypto API. Set to `false` for plain mode. |
| `Reconnect` | `ChannelReconnectOptions?` | `new()` | Auto-reconnect policy. Set to `null` to disable. See [Auto-Reconnect](#auto-reconnect-channelreconnectoptions). |
| `Retry` | `ChannelRetryOptions?` | `new()` | Per-call retry policy. Set to `null` to disable. See [Retry Policy](#retry-policy-channelretryoptions). |
| `ConfigureClient` | `Action<WitClientBuilderOptions>?` | `null` | Escape hatch for advanced builder customization. Called **after** all typed settings. |

## API Reference

### `IChannelFactory`

| Member | Returns | Description |
|--------|---------|-------------|
| `GetServiceAsync<T>()` | `Task<T>` | Returns a typed service proxy. Establishes connection on first call. Throws `InvalidOperationException` if connection cannot be established. |
| `ReconnectAsync()` | `Task` | Disconnects the current client and creates a new connection with fresh encryption keys (if enabled). |
| `DisposeAsync()` | `ValueTask` | Unsubscribes from auth events, disconnects, and disposes internal resources. Safe to call multiple times. |

### `EncryptorClientWeb`

| Member | Returns | Description |
|--------|---------|-------------|
| `InitAsync()` | `Task<bool>` | Generates RSA keys via JS interop. |
| `GetPublicKey()` | `byte[]` | Serialized public key (JSON ? UTF-8 bytes). |
| `GetPrivateKey()` | `byte[]` | Serialized private key (JSON ? UTF-8 bytes). |
| `DecryptRsa(data)` | `Task<byte[]>` | RSA-OAEP decryption via JS interop. |
| `ResetAes(key, iv)` | `bool` | Stores AES-CBC symmetric key and IV for subsequent encrypt/decrypt calls. |
| `Encrypt(data)` | `Task<byte[]>` | AES-CBC encryption via JS interop. Throws if `ResetAes` not called. |
| `Decrypt(data)` | `Task<byte[]>` | AES-CBC decryption via JS interop. Throws if `ResetAes` not called. |
| `Dispose()` | `void` | Clears all key material (public/private keys, AES key/IV). |

### `ChannelTokenProvider`

| Member | Returns | Description |
|--------|---------|-------------|
| `GetToken()` | `Task<string>` | Requests an access token from Blazor's `IAccessTokenProvider`. Returns empty string if provider is not registered or if the request fails. |

## Architecture

```
+-----------------------------------------------------+
|  Blazor WebAssembly Application                     |
|                                                     |
|    builder.Services.AddWitRpcChannel(options => ...) |
|    var svc = await factory.GetServiceAsync<T>();     |
+-----------------------------------------------------+
|  IChannelFactory -> ChannelFactory (sealed)          |
|    +-- NavigationManager  (base URI -> ws:// / wss://)|
|    +-- AuthenticationStateProvider? (sign-in/out)    |
|    +-- IJSRuntime  (Web Crypto API interop)          |
|    +-- ChannelTokenProvider  (bearer access tokens)  |
|    +-- ChannelFactoryOptions  (ApiPath, Timeout,     |
|    |     UseEncryption, Reconnect, Retry,             |
|    |     ConfigureClient)                             |
|    +-- ILogger<ChannelFactory>  (structured logs)    |
|    |                                                  |
|    +-- [per connection] EncryptorClientWeb?           |
|         (created when UseEncryption = true)           |
+-----------------------------------------------------+
|  Encryption Layer  (when UseEncryption = true)       |
|    EncryptorClientWeb <-> cryptoInterop.js           |
|    RSAParametersWeb  (JWK -> .NET field mapping)     |
|    DualNameJsonConverter  (Base64Url -> Base64)       |
|    RsaUtils  (Base64Url padding helper)             |
+-----------------------------------------------------+
|  Transport                                          |
|    OutWit.Communication.Client.WebSocket (NuGet)    |
|    WitClientBuilder  ->  WitClient                   |
|    MemoryPack serialization                         |
|    Auto-reconnect + Retry policies                  |
+-----------------------------------------------------+
```

## Dependencies

The package depends on the following (version shown is for `net10.0`; lower TFMs use matching versions):

| Package | Version (net10.0) | Purpose |
|---------|-------------------|---------|
| `Microsoft.AspNetCore.Components.WebAssembly` | 10.0.2 | Blazor WASM runtime, `NavigationManager`, `IJSRuntime` |
| `Microsoft.AspNetCore.Components.WebAssembly.Authentication` | 10.0.2 | `IAccessTokenProvider`, `AuthenticationStateProvider` |
| `OutWit.Communication.Client.WebSocket` | (project ref) | WitRPC client transport -- `WitClientBuilder`, `WitClient`, resilience policies |

## Attribution

OutWit.Communication.Client.Blazor is part of the **OutWit** ecosystem.  
Copyright (c) 2020-2026 Dmitry Ratner.

## Trademarks

"OutWit" is a trademark of Dmitry Ratner.

## License

Licensed under [Apache-2.0](LICENSE).
