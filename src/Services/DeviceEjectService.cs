using System;
using System.Text;
using eject_flow.Interop;

namespace eject_flow.Services;

public static class DeviceEjectService
{
    public static EjectResult Eject(string pnpDeviceId)
    {
        if (string.IsNullOrWhiteSpace(pnpDeviceId))
            return new EjectResult(EjectStatus.DeviceNotFound, "missing pnp device id");

        try
        {
            int rc = NativeMethods.CM_Locate_DevNodeW(out var devInst, pnpDeviceId, 0);
            if (rc != NativeMethods.CR_SUCCESS)
                return new EjectResult(EjectStatus.DeviceNotFound, $"locate devnode failed (cr={rc})");

            rc = NativeMethods.CM_Get_Parent(out var parentInst, devInst, 0);
            if (rc != NativeMethods.CR_SUCCESS)
                return new EjectResult(EjectStatus.ParentNotFound, $"get parent failed (cr={rc})");

            var vetoName = new StringBuilder(260);
            rc = NativeMethods.CM_Request_Device_EjectW(
                parentInst,
                out var vetoType,
                vetoName,
                vetoName.Capacity,
                0);

            if (rc == NativeMethods.CR_SUCCESS && vetoType == NativeMethods.PnpVetoType.Ok)
                return new EjectResult(EjectStatus.Success, "device ejected");

            var reason = DescribeVeto(vetoType);
            var who = vetoName.Length > 0 ? $" ({vetoName})" : "";
            return new EjectResult(
                EjectStatus.Vetoed,
                $"eject vetoed: {reason}{who}",
                vetoType,
                vetoName.ToString());
        }
        catch (Exception ex)
        {
            return new EjectResult(EjectStatus.Error, $"eject error: {ex.Message}");
        }
    }

    private static string DescribeVeto(NativeMethods.PnpVetoType veto) => veto switch
    {
        NativeMethods.PnpVetoType.Ok => "none",
        NativeMethods.PnpVetoType.TypeUnknown => "unknown type",
        NativeMethods.PnpVetoType.LegacyDevice => "legacy device",
        NativeMethods.PnpVetoType.PendingClose => "close pending",
        NativeMethods.PnpVetoType.WindowsApp => "app blocked",
        NativeMethods.PnpVetoType.WindowsService => "service blocked",
        NativeMethods.PnpVetoType.OutstandingOpen => "files/handles in use",
        NativeMethods.PnpVetoType.Device => "device refused",
        NativeMethods.PnpVetoType.Driver => "driver refused",
        NativeMethods.PnpVetoType.IllegalDeviceRequest => "illegal request",
        NativeMethods.PnpVetoType.InsufficientPower => "insufficient power",
        NativeMethods.PnpVetoType.NonDisableable => "not removable",
        NativeMethods.PnpVetoType.LegacyDriver => "legacy driver",
        NativeMethods.PnpVetoType.InsufficientRights => "insufficient rights",
        _ => "unknown"
    };
}
