using System.IO;

namespace OutWit.InterProcess.Tests.Agent
{
    /// <summary>
    /// The graceful-exit marker: the agent writes it from its ProcessExit
    /// handler, which runs on a clean exit but not on a kill -- so a test can
    /// tell the two apart after the fact. Shared source between the agent and
    /// the tests.
    /// </summary>
    public static class TestAgentMarker
    {
        public static string PathFor(int processId)
        {
            return Path.Combine(Path.GetTempPath(), $"witrpc-testagent-{processId}.graceful");
        }
    }
}
