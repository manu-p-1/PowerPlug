using System;
using System.Management.Automation;
using System.Net.Sockets;
using PowerPlug.Attributes;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets.Networking
{
    /// <summary>
    /// <para type="synopsis">Tests TCP port connectivity to a remote host</para>
    /// <para type="description">Test-Port performs a TCP connection test to a specified host and port.
    /// Unlike Test-Connection (ICMP only), this cmdlet tests actual service availability on specific ports.
    /// Works cross-platform on Windows, macOS, and Linux.</para>
    /// <example>
    /// <para>Test if a web server is listening</para>
    /// <code>Test-Port -Host server01 -Port 443</code>
    /// </example>
    /// <example>
    /// <para>Test multiple ports with a short timeout</para>
    /// <code>80, 443, 8080 | Test-Port -Host server01 -TimeoutMs 500</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "Port")]
    [Alias("tp")]
    [OutputType(typeof(PSObject))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class TestPortCmdlet : PowerPlugCmdletBase
    {
        /// <summary>
        /// <para type="description">The hostname or IP address to test</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        [Alias("ComputerName", "Server")]
        public string HostName { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The TCP port number to test (range: 1-65535)</para>
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true)]
        [ValidateRange(1, 65535)]
        public int Port { get; set; }

        /// <summary>
        /// <para type="description">Connection timeout in milliseconds (default: 2000, range: 100-60000)</para>
        /// </summary>
        [Parameter]
        [ValidateRange(100, 60000)]
        public int TimeoutMs { get; set; } = 2000;

        /// <summary>
        /// Processes the Test-Port PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            var open = false;
            var latencyMs = -1.0;
            var errorMessage = string.Empty;

            try
            {
                using var client = new TcpClient();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var connectTask = client.ConnectAsync(HostName, Port);
                var completed = connectTask.Wait(TimeoutMs);
                sw.Stop();

                if (completed && !connectTask.IsFaulted)
                {
                    open = true;
                    latencyMs = sw.Elapsed.TotalMilliseconds;
                }
                else if (!completed)
                {
                    errorMessage = "Connection timed out";
                }
                else if (connectTask.Exception != null)
                {
                    errorMessage = connectTask.Exception.InnerException?.Message
                                   ?? connectTask.Exception.Message;
                }
            }
            catch (SocketException ex)
            {
                errorMessage = ex.Message;
            }
            catch (AggregateException ex) when (ex.InnerException is SocketException sockEx)
            {
                errorMessage = sockEx.Message;
            }

            var result = new PSObject();
            result.Properties.Add(new PSNoteProperty("Host", HostName));
            result.Properties.Add(new PSNoteProperty("Port", Port));
            result.Properties.Add(new PSNoteProperty("Open", open));
            result.Properties.Add(new PSNoteProperty("LatencyMs", open ? Math.Round(latencyMs, 2) : (object)null!));
            result.Properties.Add(new PSNoteProperty("Error", string.IsNullOrEmpty(errorMessage) ? null : errorMessage));

            WriteObject(result);
        }
    }
}
