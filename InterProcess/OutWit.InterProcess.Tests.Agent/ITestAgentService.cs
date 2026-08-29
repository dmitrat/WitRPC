namespace OutWit.InterProcess.Tests.Agent
{
    /// <summary>
    /// The contract the integration tests drive across the process boundary.
    /// This source file is compiled into both the agent and the test assembly;
    /// the two copies are different .NET types, which is exactly what the
    /// contract-scoped method ids are for -- dispatch works by stable name, not
    /// by assembly identity.
    /// </summary>
    public interface ITestAgentService
    {
        /// <summary>The agent's process id, so a test can watch the real process.</summary>
        int GetProcessId();

        /// <summary>Round-trip probe.</summary>
        /// <param name="message">Anything.</param>
        /// <returns>The same message.</returns>
        string Echo(string message);

        /// <summary>Makes the agent die abruptly shortly after replying.</summary>
        void Crash();

        /// <summary>Makes the agent exit cleanly (exit code 0) after the given delay.</summary>
        /// <param name="delayMs">Milliseconds before the exit.</param>
        void ExitAfter(int delayMs);
    }
}
