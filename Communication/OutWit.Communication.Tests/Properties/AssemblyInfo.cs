using NUnit.Framework;

// Disable parallel test execution to avoid port/resource conflicts
[assembly: Parallelizable(ParallelScope.None)]

// Alternative: limit to one worker at a time
// [assembly: LevelOfParallelism(1)]
