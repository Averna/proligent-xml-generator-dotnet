using Xunit;

// Disable parallel test execution because XmlScenarioGenerationTests temporarily replaces
// Util.Default.UuidFactory with a sequential counter. Other test classes that create model
// objects (e.g. ProcessRunTests) would consume UUIDs from that counter if they ran in parallel,
// causing non-deterministic fixture mismatches.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
