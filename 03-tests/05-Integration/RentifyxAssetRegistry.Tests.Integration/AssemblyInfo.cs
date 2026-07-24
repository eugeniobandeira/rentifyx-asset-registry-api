using Xunit;

// Program.cs's top-level statements set the static Serilog Log.Logger once per entry-point
// invocation. WebApplicationFactory re-invokes that entry point per test class; running two
// classes' factories concurrently races on that static state and can make the host silently fail
// to start ("entry point exited without ever building an IHost", swallowed by Program.cs's broad
// catch). Serialize test classes in this assembly to avoid it.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
