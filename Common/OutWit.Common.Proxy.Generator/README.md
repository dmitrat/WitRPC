
# OutWit.Common.Proxy.Generator

`OutWit.Common.Proxy.Generator` is a source generator that automatically creates proxy classes for interfaces marked with the `ProxyTargetAttribute` from the `OutWit.Common.Proxy` library.

## Features

- Automatically generates proxies for interfaces with events, methods, and properties.
- Integrates seamlessly with custom `IProxyInterceptor` implementations.

## Getting Started

### Installation

Add `OutWit.Common.Proxy.Generator` to your project via NuGet:
```bash
dotnet add package OutWit.Common.Proxy.Generator
```

### Usage

1. Ensure your interface is marked with the `ProxyTargetAttribute`:

   ```csharp
   using OutWit.Common.Proxy;

   [ProxyTarget]
   public interface IExampleService
   {
       string GetData(int id);
       event EventHandler DataChanged;
   }
   ```

2. Implement `IProxyInterceptor` to handle invocations:

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

3. Use the generated proxy in your code:

   ```csharp
   var interceptor = new ExampleInterceptor();
   var proxy = new ExampleServiceProxy(interceptor);

   var result = proxy.GetData(42);
   Console.WriteLine(result); // Output: Data for ID 42
   ```

---

## Support for Methods, Properties, and Events

### Methods
When a method is called, a `ProxyInvocation` object is created and passed to the `Intercept` method. The return value of the method is taken from `invocation.ReturnValue`.

### Properties
`OutWit.Common.Proxy.Generator` supports both getters and setters for properties. Each invocation is wrapped in a `ProxyInvocation` object, similar to methods.

### Events
The generator handles subscription (`add`) and unsubscription (`remove`) for events, passing the relevant details to the `Intercept` method.

---

