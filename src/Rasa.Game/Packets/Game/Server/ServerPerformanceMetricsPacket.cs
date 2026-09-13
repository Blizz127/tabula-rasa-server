namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// How the world loop is doing, for the client's diagnostics display.
    /// clientmethod.Recv_ServerPerformanceMetrics(avgLoopsPerSecond, peakLoopMs) hands both to
    /// gameclient.SetServerPerfMetrics, alongside SetNetworkResponseTime and the rest of the
    /// network readouts.
    ///
    /// Both go out as doubles, because both parameters are floats. The binding is Boost.Python,
    /// and the caller object it builds carries its signature in its RTTI name:
    ///
    ///   caller&lt;P8GameClient@TRasa@@AEXMM@Z, default_call_policies,
    ///          mpl::vector4&lt;X, AAVGameClient@TRasa@@, M, M&gt;&gt;
    ///
    /// which is void TRasa::GameClient::SetServerPerfMetrics(float, float) written twice - once
    /// as a member function pointer and once as an mpl vector of return type, this, and the two
    /// arguments. An int would be accepted too, since Boost.Python converts one for a float
    /// parameter, but it would throw away the fraction on a measurement that has one.
    /// </summary>
    public class ServerPerformanceMetricsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ServerPerformanceMetrics;

        public double AvgLoopsPerSecond { get; }
        public double PeakLoopMs { get; }

        public ServerPerformanceMetricsPacket(double avgLoopsPerSecond, double peakLoopMs)
        {
            AvgLoopsPerSecond = avgLoopsPerSecond;
            PeakLoopMs = peakLoopMs;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteDouble(AvgLoopsPerSecond);
            pw.WriteDouble(PeakLoopMs);
        }
    }
}
