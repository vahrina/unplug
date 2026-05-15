using System.Runtime.InteropServices;
using System.Text;

namespace eject_flow.Interop;

public static class NativeMethods
{
    public const int CR_SUCCESS = 0;

    public enum PnpVetoType
    {
        Ok = 0,
        TypeUnknown = 1,
        LegacyDevice = 2,
        PendingClose = 3,
        WindowsApp = 4,
        WindowsService = 5,
        OutstandingOpen = 6,
        Device = 7,
        Driver = 8,
        IllegalDeviceRequest = 9,
        InsufficientPower = 10,
        NonDisableable = 11,
        LegacyDriver = 12,
        InsufficientRights = 13
    }

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false)]
    public static extern int CM_Locate_DevNodeW(
        out uint pdnDevInst,
        string pDeviceID,
        uint ulFlags);

    [DllImport("cfgmgr32.dll", ExactSpelling = true, SetLastError = false)]
    public static extern int CM_Get_Parent(
        out uint pdnDevInst,
        uint dnDevInst,
        uint ulFlags);

    [DllImport("cfgmgr32.dll", ExactSpelling = true, SetLastError = false)]
    public static extern int CM_Reenumerate_DevNode(
        uint dnDevInst,
        uint ulFlags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false)]
    public static extern int CM_Request_Device_EjectW(
        uint dnDevInst,
        out PnpVetoType pVetoType,
        StringBuilder? pszVetoName,
        int ulNameLength,
        uint ulFlags);
}
