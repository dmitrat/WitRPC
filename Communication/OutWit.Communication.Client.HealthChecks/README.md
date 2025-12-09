
# OutWit.Communication.Client.HealthChecks

ASP.NET Core health checks integration for WitRPC client, providing health monitoring for WitRPC client connectivity.

### Overview

**OutWit.Communication.Client.HealthChecks** provides health check implementations for monitoring WitRPC client connections. It integrates with the ASP.NET Core health checks framework, allowing you to monitor client connectivity, authorization status, and reconnection state.

Key features:
- **Connection monitoring** - Check if clients are connected and authorized
- **State reporting** - Reports connection state (Connected, Disconnected, Reconnecting, Failed)
- **Rich health data** - Returns detailed information about client status
- **Easy integration** - Works with standard ASP.NET Core health check endpoints

### Installation

```shell
Install-Package OutWit.Communication.Client.HealthChecks
```

### Usage

#### Basic Health Check Registration

Add a health check for a specific WitRPC client:

```csharp
services.AddWitRpcClient("my-service", options =>
{
    options.WithWebSocket("ws://localhost:5000");
    options.WithJson();
    options.WithAutoReconnect();
});

services.AddHealthChecks()
    .AddWitRpcClient("my-service");

// Map health check endpoint
app.MapHealthChecks("/health");
```

#### Custom Health Check Name

Specify a custom name for the health check:

```csharp
services.AddHealthChecks()
    .AddWitRpcClient("my-service", name: "witrpc-connectivity");
```

#### Custom Failure Status

Specify the health status to report on failure:

```csharp
services.AddHealthChecks()
    .AddWitRpcClient("my-service", 
        failureStatus: HealthStatus.Degraded);
```

#### With Tags

Add tags for filtering health checks:

```csharp
services.AddHealthChecks()
    .AddWitRpcClient("my-service", 
        tags: new[] { "witrpc", "external" });

// Map filtered health checks
app.MapHealthChecks("/health/witrpc", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("witrpc")
});
```

#### Multiple Clients

Monitor multiple WitRPC clients:

```csharp
services.AddWitRpcClient("service-a", options => { /* ... */ });
services.AddWitRpcClient("service-b", options => { /* ... */ });

services.AddHealthChecks()
    .AddWitRpcClient("service-a")
    .AddWitRpcClient("service-b");
```

### Health Check Response

The health check returns the following data:

| Field | Description |
|-------|-------------|
| `clientName` | Name of the client configuration |
| `connectionState` | Current state: Connected, Disconnected, Reconnecting, Failed |
| `isInitialized` | Whether the client has completed initialization |
| `isAuthorized` | Whether the client has completed authorization |

**Health Status Mapping:**
- **Healthy** - Client is connected and authorized
- **Degraded** - Client is reconnecting
- **Unhealthy** - Client is disconnected or reconnection failed

### Example Response

```json
{
  "status": "Healthy",
  "results": {
    "witrpc-client-my-service": {
      "status": "Healthy",
      "description": "WitRPC client 'my-service' is connected and authorized.",
      "data": {
        "clientName": "my-service",
        "connectionState": "Connected",
        "isInitialized": true,
        "isAuthorized": true
      }
    }
  }
}
```

### Further Documentation

For more about WitRPC and health monitoring, see the official documentation on [witrpc.io](https://witrpc.io/).