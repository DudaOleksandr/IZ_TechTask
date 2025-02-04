namespace IZTechTask.Enums;

public enum MessageType : byte
{
    // Host
    SetControlItem = 0b000,
    RequestCurrentControlItem = 0b001,
    RequestControlItemRange = 0b010,
    DataItemAckFromHost = 0b011,
    HostDataItem0 = 0b100,
    HostDataItem1 = 0b101,
    HostDataItem2 = 0b110,
    HostDataItem3 = 0b111,
    // Target
    ResponseToSetOrRequest = 0b000,
    UnsolicitedControlItem = 0b001,
    ResponseToRequestRange = 0b010,
    DataItemAckFromTarget = 0b011,
    TargetDataItem0 = 0b100,
    TargetDataItem1 = 0b101,
    TargetDataItem2 = 0b110,
    TargetDataItem3 = 0b111
}