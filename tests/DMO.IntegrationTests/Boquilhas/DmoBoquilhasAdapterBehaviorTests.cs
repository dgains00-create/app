using System.Diagnostics;
using Xunit;

namespace DMO.IntegrationTests.Boquilhas;

/// <summary>
/// K5/T2/T3 (AC-K5/AC-T2/AC-T3; D2 preservation) — BEHAVIORAL proof of the page-owned adapter
/// (<c>src/DMO.Web/wwwroot/js/dmo-boquilhas.js</c>, the REAL shipped file) executed with a real
/// JavaScript engine (node) against a minimal document/window/fetch stub.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §11.3/§16/§24.3 and the accepted D2 pattern (conflict presentation +
/// explicit "Recarregar estado atual" recovery; no automatic retry, no auto-merge, no silent
/// overwrite; the observed version refreshes only on success), the shared Tool orchestration
/// (never auto-select, ambiguity explicit, contextual create returns the canonical
/// <c>tool_id</c> and restores the SAME origin state). Environment-gated exactly like the accepted
/// D1/D2 harnesses: when <c>node</c> is not available the test is skipped (the rendered rows and
/// the full suites still guard the surface).
/// </remarks>
public sealed class DmoBoquilhasAdapterBehaviorTests
{
    /// <summary>The behavioral harness (JS), relative to the repository root.</summary>
    private const string HarnessRelativePath =
        "tests/DMO.IntegrationTests/Boquilhas/dmo-boquilhas-adapter.behavior.mjs";

    /// <summary>The REAL shipped adapter under test, relative to the repository root.</summary>
    private const string AdapterRelativePath = "src/DMO.Web/wwwroot/js/dmo-boquilhas.js";

    /// <summary>
    /// K5/T2/T3 — the adapter recovers from stale-version with the explicit reload and never
    /// auto-retries; non-stale refusals keep the errors presentation; the picker never
    /// auto-selects (even one candidate); the contextual create returns the canonical tool_id and
    /// preserves/restores the origin state.
    /// </summary>
    [SkippableFact]
    public void K5T2T3_TheAdapterRecoversFromStaleVersionAndHonoursTheSharedPickerRules()
    {
        Skip.If(
            !TryLocateNode(out var nodeExecutable),
            "node is not available on this environment; the JS behavioral harness is skipped " +
            "(the rendered rows and the full suites still guard the P2-T07 surface).");

        var harnessPath = Path.Combine(
            DMO.IntegrationTests.JobOn.P2T04ProductionScan.RepositoryRoot(),
            HarnessRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var adapterPath = Path.Combine(
            DMO.IntegrationTests.JobOn.P2T04ProductionScan.RepositoryRoot(),
            AdapterRelativePath.Replace('/', Path.DirectorySeparatorChar));

        var startInfo = new ProcessStartInfo
        {
            FileName = nodeExecutable,
            Arguments = $"\"{harnessPath}\" \"{adapterPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)!;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(TimeSpan.FromSeconds(60)))
        {
            process.Kill(entireProcessTree: true);
            throw new Xunit.Sdk.XunitException(
                $"The adapter behavioral harness timed out.\nstdout:\n{stdout}\nstderr:\n{stderr}");
        }

        Assert.True(
            process.ExitCode == 0,
            $"The adapter behavioral harness FAILED (exit {process.ExitCode}).\nstdout:\n{stdout}\nstderr:\n{stderr}");
    }

    /// <summary>Probes <c>node</c> on PATH (a real availability probe, never a silent assumption).</summary>
    private static bool TryLocateNode(out string executable)
    {
        try
        {
            var probe = new ProcessStartInfo("node", "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(probe)!;
            process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();

            executable = process.WaitForExit(TimeSpan.FromSeconds(15)) && process.ExitCode == 0
                ? "node"
                : string.Empty;

            return executable.Length > 0;
        }
        catch (Exception exception) when (
            exception is System.ComponentModel.Win32Exception
            || exception is InvalidOperationException
            || exception is System.IO.IOException)
        {
            executable = string.Empty;
            return false;
        }
    }
}