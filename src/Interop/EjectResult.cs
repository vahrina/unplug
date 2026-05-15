namespace eject_flow.Interop;

public enum EjectStatus
{
    Success,
    Vetoed,
    DeviceNotFound,
    ParentNotFound,
    Error
}

public sealed record EjectResult(
    EjectStatus Status,
    string Message,
    NativeMethods.PnpVetoType? VetoType = null,
    string? VetoName = null);
