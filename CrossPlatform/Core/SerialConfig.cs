using System.IO.Ports;

namespace BCom.CrossPlatform;

/// <summary>
/// Serial communication parameters and standard option sets.
/// </summary>
public sealed class SerialConfig
{
    public static readonly int[] BaudRates = { 1382400, 460800, 230400, 115200, 38400, 9600 };
    public static readonly int[] DataBitsOptions = { 8, 7, 6, 5 };
    public static readonly StopBits[] StopBitsOptions = { StopBits.One, StopBits.Two, StopBits.OnePointFive };
    public static readonly Parity[] ParityOptions = { Parity.None, Parity.Odd, Parity.Even, Parity.Mark, Parity.Space };

    public string PortName { get; set; } = string.Empty;
    public int BaudRate { get; set; } = 115200;
    public int DataBits { get; set; } = 8;
    public StopBits StopBits { get; set; } = StopBits.One;
    public Parity Parity { get; set; } = Parity.None;
    public bool RtsEnable { get; set; }
    public bool DtrEnable { get; set; }
}
