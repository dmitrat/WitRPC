# OutWit.Common.Proxy

`OutWit.Common.Proxy` provides foundational classes and interfaces for creating proxy objects using static code generation. The library is designed for environments where dynamic generation (e.g., `Castle DynamicProxy`) is not feasible, such as AoT compilation or Blazor.

## Features

- **Interface for creating interceptors (`IProxyInterceptor`)**:
  Allows developers to configure interception of method, property, and event calls.

- **`ProxyTargetAttribute`**:
  Used to mark interfaces that should be processed by the proxy generator.

- **`IProxyInvocation` Interface**:
  Provides details about the invocation, including method name, parameters, return values, and their types.

## Getting Started

### Installation
Add `OutWit.Common.Proxy` to your project via NuGet:
```bash
dotnet add package OutWit.Common.Proxy
```

### Usage

1. Define an interface and mark it with the `ProxyTargetAttribute`:

   ```csharp
   using OutWit.Common.Proxy;

   [ProxyTarget]
   public interface IExampleService
   {
       string GetData(int id);
       event EventHandler DataChanged;
   }
   ```

2. Implement a class that implements `IProxyInterceptor` to handle calls:

   ```csharp
   public class ExampleInterceptor : IProxyInterceptor
   {
       public void Intercept(IProxyInvocation invocation)
       {
           Console.WriteLine($"Intercepted method: {invocation.MethodName}");
           if (invocation.MethodName == "GetData")
           {
               invocation.ReturnValue = $"Data for ID {invocation.Parameters[0]}";
           }
       }
   }
   ```

3. Integrate the `OutWit.Common.Proxy.Generator` library to generate the proxy for your interface (refer to `README.md` for `OutWit.Common.Proxy.Generator`).

---

## Interfaces and Classes

- **`ProxyTargetAttribute`**:
  An attribute to mark interfaces that are processed by the proxy generator.

- **`IProxyInterceptor`**:
  Interface for creating a handler for method, property, and event calls.

- **`IProxyInvocation`**:
  Interface that describes a method/property/event invocation.

- **`ProxyInvocation`**:
  Implementation of `IProxyInvocation`.

---

