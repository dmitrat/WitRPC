using NUnit.Framework;

// Disable parallel test execution to avoid port/resource conflicts
[assembly: Parallelizable(ParallelScope.None)]

// Alternative: limit to one worker at a time
// [assembly: LevelOfParallelism(1)]

// No [assembly: Timeout(...)] here, though it is the obvious thing to reach for.
// NUnit implements Timeout with Thread.Abort, which .NET Core removed, so the
// attribute does not fail the slow test -- it fails every test in the assembly
// with "TargetFramework doesn't support timeout on tests".
//
// The waits are therefore bounded where they are made: CONNECT_TIMEOUT on every
// ConnectAsync, a limit on disposal in TearDown, and StopWithin around a server
// stop that can block. Anything added later has to follow the same rule, because
// a single unbounded wait parks the whole run and leaves an orphaned testhost
// holding bin/ against the next build.
