# OutWit.Communication.Server.Rest

## Overview

The `OutWit.Communication.Server.Rest` library extends WitRPC to support RESTful communication. It provides tools to expose services via REST APIs with advanced features like authentication, request processing, and logging. This package is ideal for scenarios requiring lightweight, scalable HTTP-based communication.

## Features

### 1. REST API Server
- Seamlessly expose services over HTTP.
- Automatically maps HTTP requests to service methods.

### 2. Authentication and Authorization
- Token-based authentication.
- Customizable access control mechanisms.

### 3. Customizable Request Processing
- Supports both GET and POST requests.
- Allows custom logic for processing incoming requests and parameters.

### 4. Logging and Debugging
- Integrated support for logging errors and request/response cycles.

## Installation

Install the package via NuGet:
```bash
Install-Package OutWit.Communication.Server.Rest
```

## Getting Started

### Basic Setup
```csharp
using OutWit.Communication.Server.Rest;

var server = WitServerRestBuilder.Build(options =>
{
    options.WithUrl("http://localhost:5000/");
    options.WithService(new MyService());
    options.WithAccessToken("my-secret-token");
    options.WithLogger(new ConsoleLogger());
    options.WithTimeout(TimeSpan.FromMinutes(5));
});

server.StartWaitingForConnection();
```

### Exposing a Service
Define your service with public methods to be exposed via REST API.
```csharp
public class MyService
{
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }
}
```
Access this service with:
```http
GET http://localhost:5000/Greet?name=John
```
Response:
```json
"Hello, John!"
```

### Advanced Configuration

#### Custom Request Processor
You can define custom logic for processing incoming requests:
```csharp
using OutWit.Communication.Server.Rest;

var server = WitServerRestBuilder.Build(options =>
{
    options.WithUrl("http://localhost:5000/");
    options.WithRequestProcessor(new CustomRequestProcessor());
});
```

#### Disable Authorization
Disable authorization entirely for public APIs:
```csharp
var server = WitServerRestBuilder.Build(options =>
{
    options.WithUrl("http://localhost:5000/");
    options.WithoutAuthorization();
});
```

#### Logging Support
Integrate logging for debugging and monitoring:
```csharp
var server = WitServerRestBuilder.Build(options =>
{
    options.WithUrl("http://localhost:5000/");
    options.WithLogger(new MyCustomLogger());
});
```

## API Reference

### `WitServerRest`
The core server class that handles HTTP requests and responses.

#### Methods
- `StartWaitingForConnection()`: Starts listening for incoming connections.
- `StopWaitingForConnection()`: Stops the server and releases resources.

### `WitServerRestBuilder`
Provides a fluent interface for configuring and creating a `WitServerRest` instance.

#### Configuration Options
- `.WithUrl(string url)`: Sets the server's base URL.
- `.WithService<TService>(TService service)`: Binds a service instance to the server.
- `.WithAccessToken(string token)`: Enables token-based authentication.
- `.WithoutAuthorization()`: Disables authentication.
- `.WithLogger(ILogger logger)`: Configures logging.
- `.WithTimeout(TimeSpan timeout)`: Sets request timeout.

## Error Handling
Errors encountered during request processing are logged and returned as HTTP responses with appropriate status codes.

Example error response:
```json
{
  "status": 400,
  "message": "Failed to process request"
}
```
