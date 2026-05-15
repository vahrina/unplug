using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using eject_flow.Models;

namespace eject_flow.Services;

public static class UsbDeviceService
{
    public static List<UsbDevice> Enumerate()
    {
        var usbPnpIds = GetUsbControllerDeviceIds();
        var installDates = GetPnpInstallDates();
        var devices = new List<UsbDevice>();

        using var diskSearcher = new ManagementObjectSearcher(
            "SELECT DeviceID, PNPDeviceID, Model, Size FROM Win32_DiskDrive");

        foreach (ManagementObject disk in diskSearcher.Get())
        {
            using (disk)
            {
                var pnpDeviceId = disk["PNPDeviceID"] as string ?? "";
                if (!usbPnpIds.Contains(pnpDeviceId)) continue;

                var deviceId = disk["DeviceID"] as string ?? "";
                var model    = disk["Model"]    as string ?? "";
                ulong diskSize = 0;
                if (disk["Size"] is not null && ulong.TryParse(disk["Size"]!.ToString(), out var sz))
                    diskSize = sz;

                var letters    = new List<string>();
                string? firstLabel = null;
                ulong totalUsed = 0;
                ulong totalFree = 0;

                foreach (var partition in disk.GetRelated("Win32_DiskPartition").OfType<ManagementObject>())
                {
                    using (partition)
                    {
                        foreach (var logical in partition.GetRelated("Win32_LogicalDisk").OfType<ManagementObject>())
                        {
                            using (logical)
                            {
                                var letter = logical["DeviceID"] as string;
                                if (!string.IsNullOrEmpty(letter))
                                    letters.Add(letter);

                                firstLabel ??= logical["VolumeName"] as string;

                                if (logical["Size"] is not null &&
                                    ulong.TryParse(logical["Size"]!.ToString(), out var volSize))
                                {
                                    ulong free = 0;
                                    if (logical["FreeSpace"] is not null &&
                                        ulong.TryParse(logical["FreeSpace"]!.ToString(), out var volFree))
                                        free = volFree;
                                    totalFree += free;
                                    totalUsed += volSize - free;
                                }
                            }
                        }
                    }
                }

                // match PNP install date — WMI stores dates as yyyyMMddHHmmss.ffffff+offset
                installDates.TryGetValue(pnpDeviceId, out var connectedAt);
                var hasOpenHandles = letters.Any(VolumeHasOpenHandles);

                devices.Add(new UsbDevice
                {
                    PhysicalDeviceId = deviceId,
                    PnpDeviceId      = pnpDeviceId,
                    Model            = model,
                    SizeBytes        = diskSize,
                    DriveLetters     = letters,
                    VolumeLabel      = string.IsNullOrWhiteSpace(firstLabel) ? null : firstLabel,
                    UsedBytes        = totalUsed,
                    FreeBytes        = totalFree,
                    ConnectedAt      = connectedAt,
                    HasOpenHandles   = hasOpenHandles
                });
            }
        }

        return devices;
    }

    // -------------------------------------------------------------------------
    // Pull InstallDate from Win32_PnPEntity for all USB devices at once.
    // InstallDate is a CIM_DATETIME string: yyyyMMddHHmmss.ffffff+offset
    // -------------------------------------------------------------------------
    private static Dictionary<string, DateTime> GetPnpInstallDates()
    {
        var map = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, InstallDate FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%' OR DeviceID LIKE 'SCSI%'");

            foreach (ManagementObject entity in searcher.Get())
            {
                using (entity)
                {
                    var deviceId = entity["DeviceID"] as string ?? "";
                    if (string.IsNullOrEmpty(deviceId)) continue;

                    if (entity["InstallDate"] is string raw && !string.IsNullOrEmpty(raw))
                    {
                        // ManagementDateTimeConverter handles the WMI datetime format reliably
                        var dt = ManagementDateTimeConverter.ToDateTime(raw);
                        map[deviceId] = dt.ToLocalTime();
                    }
                }
            }
        }
        catch { /* best effort */ }
        return map;
    }

    // -------------------------------------------------------------------------
    // Open-handle detection via exclusive CreateFile on the volume path
    // -------------------------------------------------------------------------
    private static bool VolumeHasOpenHandles(string driveLetter)
    {
        var path = @"\\.\" + driveLetter.TrimEnd('\\');
        try
        {
            var handle = NativeMethods.CreateFile(
                path,
                NativeMethods.GENERIC_READ,
                0,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (handle == NativeMethods.INVALID_HANDLE_VALUE)
            {
                var err = Marshal.GetLastWin32Error();
                return err == 5 || err == 32;
            }

            NativeMethods.CloseHandle(handle);
            return false;
        }
        catch { return false; }
    }

    // -------------------------------------------------------------------------
    // USB controller device IDs
    // -------------------------------------------------------------------------
    private static HashSet<string> GetUsbControllerDeviceIds()
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Dependent FROM Win32_USBControllerDevice");

            foreach (ManagementObject rel in searcher.Get())
            {
                using (rel)
                {
                    var dependent = rel["Dependent"] as string ?? "";
                    var start = dependent.IndexOf('"');
                    var end   = dependent.LastIndexOf('"');
                    if (start >= 0 && end > start)
                    {
                        var raw = dependent.Substring(start + 1, end - start - 1);
                        ids.Add(raw.Replace("\\\\", "\\"));
                    }
                }
            }
        }
        catch
        {
            using var fallback = new ManagementObjectSearcher(
                "SELECT PNPDeviceID FROM Win32_DiskDrive WHERE PNPDeviceID LIKE 'USBSTOR%'");

            foreach (ManagementObject disk in fallback.Get())
            {
                using (disk)
                {
                    var id = disk["PNPDeviceID"] as string ?? "";
                    if (!string.IsNullOrEmpty(id)) ids.Add(id);
                }
            }
        }
        return ids;
    }

    private static class NativeMethods
    {
        public const uint GENERIC_READ  = 0x80000000;
        public const uint OPEN_EXISTING = 3;
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFile(
            string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);
    }
}
