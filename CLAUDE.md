# WitRPC Project Guidelines

Guidelines for the WitRPC repository (`OutWit.Communication.*` / `OutWit.InterProcess.*` packages). The conventions below are the shared OutWit house style.

## Core Dependencies

### OutWit.Common (Required)

All projects must reference `OutWit.Common`. All domain models must inherit from `ModelBase`.

```csharp
using OutWit.Common.Abstract;
using OutWit.Common.Values;

public class Person : ModelBase
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    // Required: value-based comparison
    public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
    {
        if (modelBase is not Person other)
            return false;

        return Id.Is(other.Id) && Name.Is(other.Name);
    }

    // Required: for immutable-style updates via With()
    public override ModelBase Clone()
    {
        return new Person { Id = Id, Name = Name };
    }
}
```

**Key patterns**:
- Use `obj1.Is(obj2)` for value comparison (not `Equals()`)
- Use `obj.With(x => x.Property, newValue)` for immutable updates
- Use `[ToString]` attribute for declarative logging

### OutWit.Common.MVVM (Required for UI Projects)

UI projects must use MVVM pattern with `OutWit.Common.MVVM`:

```bash
# Base package
dotnet add package OutWit.Common.MVVM

# Platform-specific
dotnet add package OutWit.Common.MVVM.WPF
dotnet add package OutWit.Common.MVVM.Avalonia
dotnet add package OutWit.Common.MVVM.Blazor
```

**Rules**:
- **No code-behind** — all logic in ViewModels, bind everything in XAML
- **ViewModelBase<T>** — all ViewModels inherit from `ViewModelBase<T>` where `T` is `ApplicationViewModel`
- **ApplicationViewModel** — central store for all other ViewModels

```csharp
// ApplicationViewModel - root ViewModel containing all others
public class ApplicationViewModel : ViewModelBase<ApplicationViewModel>
{
    public MainViewModel Main { get; }
    public SettingsViewModel Settings { get; }

    public ApplicationViewModel() : base(null!)
    {
        Main = new MainViewModel(this);
        Settings = new SettingsViewModel(this);
    }
}

// Child ViewModels reference ApplicationViewModel
public class MainViewModel : ViewModelBase<ApplicationViewModel>
{
    public MainViewModel(ApplicationViewModel appVm) : base(appVm)
    {
        InitCommands();
    }

    private void InitCommands()
    {
        SaveCommand = new RelayCommand(_ => Save(), _ => CanSave);
    }

    public RelayCommand SaveCommand { get; private set; } = null!;
}
```

### Custom Controls (WPF / Avalonia)

For custom controls, use platform-specific packages with source generators:

```bash
# WPF
dotnet add package OutWit.Common.MVVM.WPF

# Avalonia
dotnet add package OutWit.Common.MVVM.Avalonia
```

**Rules**:
- Mark control classes as `partial`
- Use `[StyledProperty]` for DependencyProperty/StyledProperty generation
- Use `[AttachedProperty]` for attached properties
- Use convention-based callbacks: `On{PropertyName}Changed`, `{PropertyName}Coerce`

```csharp
// WPF Example
using OutWit.Common.MVVM.WPF.Attributes;

public partial class CustomButton : Button
{
    [StyledProperty(DefaultValue = "Click Me")]
    public string Label { get; set; }

    [StyledProperty(AffectsMeasure = true, BindsTwoWayByDefault = true)]
    public double IconSize { get; set; }

    // Convention: On{PropertyName}Changed - auto-discovered
    private static void OnIconSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CustomButton)d;
        // Handle change
    }
}

// Avalonia Example
using OutWit.Common.MVVM.Avalonia.Attributes;

public partial class CustomButton : Button
{
    [StyledProperty(DefaultValue = "Click Me")]
    public string Label { get; set; }

    // Use DirectProperty for frequently changing values (better performance)
    [DirectProperty(DefaultValue = 0)]
    public int Counter { get; set; }
}

// Attached Properties (both platforms)
public static partial class MyAttachedProperties
{
    [AttachedProperty(DefaultValue = false)]
    public static bool IsHighlighted { get; set; }
}
```

### OutWit.Common.Aspects (For ViewModels)

Add reference when you need `INotifyPropertyChanged` support. Use `[Notify]` attribute for auto-notification.

**Note**: `ModelBase` already implements `INotifyPropertyChanged`.

```csharp
using OutWit.Common.Aspects;

public class MyViewModel : ModelBase
{
    public MyViewModel()
    {
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Type-safe property check
        if (e.IsProperty((MyViewModel vm) => vm.Name))
            UpdateStatus();
    }

    [Notify]
    public string Name { get; set; } = null!;

    [Notify]
    public bool IsLoading { get; set; }
}
```

**Key points**:
- `[Notify]` auto-raises `PropertyChanged` on setter
- Use `e.IsProperty((T vm) => vm.Prop)` for type-safe property name checks
- No manual `RaisePropertyChanged()` calls needed

## Code Style

This project follows strict code style conventions. Always adhere to these rules when writing or modifying code.

### Class Structure Order

```
1. Constants
2. Events
3. Fields
4. Constructors
5. Initialization (InitDefault, InitEvents, InitCommands)
6. Methods (grouped by meaning, private at end of each region)
7. Interface implementations (each in own region)
8. Properties (at the very end)
```

### File Content Rules

#### One Class Per File

- Each class must be in its own file
- **Exception**: A class-specific enum can be in the same file if it's only used as a field/property of that class
- If an enum is used in multiple places → separate file

#### File Size Limits

| Lines | Status | Action |
|-------|--------|--------|
| < 400 | 🟢 Green | All good |
| 400–600 | 🟡 Yellow | Attention needed |
| > 600 | 🔴 Red | Refactor required |

**When file exceeds 600 lines**, consider:
1. Extract to class hierarchy (base class + derived)
2. Extract helper classes
3. Use `partial` class (last resort)

### Naming Conventions

#### Fields and Constants

| Type | Style | Example |
|------|-------|---------|
| Private instance fields | `m_` prefix | `m_storage`, `m_disposed` |
| Private static fields | `m_` prefix | `m_instance`, `m_cache` |
| Public static fields | PascalCase | `Instance`, `DefaultProvider` |
| Static readonly fields | UPPER_CASE | `DEFAULT_INSTANCE`, `EMPTY_ARRAY` |
| Constants | UPPER_CASE | `DEFAULT_PAGE_SIZE`, `MAX_KEY_LENGTH` |

#### File Naming (Extended Interface Suffix Principle)

**Goal**: Keep related files visually grouped in Solution Explorer.

**Rule**: Name implementations with base/interface name FIRST, variant LAST.

**Wrong** (files scattered alphabetically):
```
SimpleWalReplayVisitor.cs      // S...
TransactionalWalReplayVisitor.cs  // T...
```

**Correct** (files grouped together near IWalReplayVisitor):
```
WalReplayVisitorSimple.cs
WalReplayVisitorTransactional.cs
```

**Examples**:

| Interface/Base Class | Implementations |
|---------------------|-----------------|
| `IStorage` | `StorageFile.cs`, `StorageMemory.cs`, `StorageEncrypted.cs` |
| `IPageCache` | `PageCacheLru.cs`, `PageCacheClock.cs` |
| `ICryptoProvider` | `CryptoProviderAesGcm.cs`, `CryptoProviderBouncyCastle.cs` |

**Exception**: If too many implementations, use folders with reversed naming:
```
Storage/
  FileStorage.cs
  MemoryStorage.cs
  EncryptedStorage.cs
```

### Regions

Always use `#region` / `#endregion` with empty lines after opening and before closing:

```csharp
#region Constants

private const int PAGE_SIZE = 4096;

#endregion
```

### Access Modifiers

- Always explicit (`private`, `internal`, `public`)
- Default to `private`
- Use `sealed` for non-inheritable classes
- Use `readonly` for immutable fields

### Nullable Reference Types

- Always enabled via csproj
- Explicit nullability: `byte[]?` for nullable returns
- Avoid `!` operator where possible

## Test Style (NUnit 4)

### Naming

- **Test class**: Ends with `Tests` (plural) → `StorageFileTests`
- **Test method**: Ends with `Test` (singular), PascalCase, no underscores → `StorageFileHasCorrectProviderKeyTest`

### Structure

```csharp
[TestFixture]
public class StorageFileTests
{
    private string m_testDir = null!;

    [SetUp]
    public void Setup() { }

    [TearDown]
    public void TearDown() { }

    #region Read Tests

    [Test]
    public void ReadPageReturnsCorrectDataTest() { }

    #endregion
}
```

### File Location

Mirror main project structure:
```
OutWit.Database.Core/Storage/StorageFile.cs
OutWit.Database.Core.Tests/Storage/StorageFileTests.cs
```

## ViewModel Style (MVVM)

### Element Order

```
1. Constants
2. Events
3. Fields
4. Constructors
5. Initialization (InitDefault, InitEvents, InitCommands)
6. Functions (command implementations)
7. Tools (UpdateStatus)
8. Event Handlers
9. Properties
10. Commands
11. Services
```

### Command Enablement

Use `UpdateStatus()` pattern — never call `RaiseCanExecuteChanged()` directly:

```csharp
#region Tools

private void UpdateStatus()
{
    CanSave = !string.IsNullOrWhiteSpace(Name) && !IsLoading;
}

#endregion

#region Event Handlers

private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.IsProperty((MyViewModel vm) => vm.Name))
        UpdateStatus();
}

#endregion
```

## Extension Methods

Separate base classes from extensions for builder patterns:

```csharp
// Main class
public sealed class WitDatabaseBuilder { }

// Extensions in same project
public static class WitDatabaseBuilderExtensions { }

// Extensions in another project
public static class WitDatabaseBuilderBouncyCastleExtensions { }
```

## XML Documentation

Required for all public methods with `<summary>`, `<param>`, `<returns>`, `<exception>`.
