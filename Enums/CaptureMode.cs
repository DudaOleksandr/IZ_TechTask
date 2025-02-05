namespace IZTechTask.Enums;

public enum CaptureMode : byte
{
    Contiguous16Bit = 0x00,
    Contiguous24Bit = 0x80,
    Fifo16Bit = 0x01,
    HardwareTriggered24Bit = 0x83,
    HardwareTriggered16Bit = 0x03
}