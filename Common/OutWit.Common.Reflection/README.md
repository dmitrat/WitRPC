# OutWit.Common.Reflection

## EventUtils: Universal Event Handling Made Easy

The `EventUtils` class provides utilities for working with .NET events, enabling developers to create universal handlers for any event in a highly reusable and reflective manner. This is particularly useful in scenarios requiring dynamic event handling or generalized solutions for complex event-driven architectures.

## Features

### 1. Retrieve All Events of a Type
The `GetAllEvents` method recursively retrieves all events from a given type, including:
- Events defined on the type itself
- Events defined in its base types
- Events from implemented interfaces

```csharp
Type type = typeof(SomeClass);
IEnumerable<EventInfo> allEvents = type.GetAllEvents();
foreach (var eventInfo in allEvents)
{
    Console.WriteLine(eventInfo.Name);
}
```

### 2. Create Universal Event Handlers
The `CreateUniversalHandler` method allows you to create a delegate for any event using a universal handler. This handler acts as a single entry point for all events of a type, dynamically handling event calls.

#### Key Benefits:
- Dynamically binds to event handlers at runtime.
- Enables centralized logging, debugging, or analytics for all events.
- Simplifies handling events with unknown signatures at compile-time.

#### Example Usage
```csharp
using OutWit.Common.Reflection;

public class Example
{
    public static void Main()
    {
        var obj = new SomeClass();

        EventInfo eventInfo = typeof(SomeClass).GetEvent("SomeEvent")!;

        var handler = eventInfo.CreateUniversalHandler(
            obj,
            (sender, eventName, parameters) =>
            {
                Console.WriteLine($"Event {eventName} triggered on {sender} with parameters: {string.Join(", ", parameters)}");
            });

        eventInfo.AddEventHandler(obj, handler);

        obj.TriggerSomeEvent(); // Triggers the universal handler
    }
}

public class SomeClass
{
    public event EventHandler? SomeEvent;

    public void TriggerSomeEvent()
    {
        SomeEvent?.Invoke(this, EventArgs.Empty);
    }
}
```

### Method Details

#### `GetAllEvents`
```csharp
public static IEnumerable<EventInfo> GetAllEvents(this Type type)
```
- **Description**: Recursively retrieves all events from a type, including those from base types and interfaces.
- **Returns**: A collection of `EventInfo` objects.

#### `CreateUniversalHandler`
```csharp
public static Delegate CreateUniversalHandler<TSender>(
    this EventInfo me,
    TSender sender,
    UniversalEventHandler<TSender> handler
) where TSender : class;
```
- **Description**: Creates a dynamic delegate that binds a universal handler to an event.
- **Parameters**:
  - `me`: The `EventInfo` describing the event.
  - `sender`: The object raising the event.
  - `handler`: The universal event handler delegate.
- **Returns**: A `Delegate` matching the event's signature.

#### `UniversalEventHandler`
A delegate used for the universal handler:
```csharp
public delegate void UniversalEventHandler<in TSender>(
    TSender sender,
    string eventName,
    object[] parameters
) where TSender : class;
```

- **Parameters**:
  - `sender`: The object that raised the event.
  - `eventName`: The name of the event.
  - `parameters`: The event arguments passed during invocation.

## Installation
Include the `OutWit.Common.Reflection` namespace in your project to access `EventUtils`.
