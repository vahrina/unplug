using System;
using System.Collections.Generic;
using System.Linq;
using eject_flow.Interop;
using eject_flow.Models;
using eject_flow.Services;
using Flow.Launcher.Plugin;

namespace eject_flow;

public class Main : IPlugin
{
    private const string Icon = "icon.png";

    private PluginInitContext _context = null!;

    public void Init(PluginInitContext context) => _context = context;

    public List<Result> Query(Query query)
    {
        var term    = (query.Search ?? "").Trim();
        var results = new List<Result>();

        // eject
        List<UsbDevice> devices;
        try { devices = UsbDeviceService.Enumerate(); }
        catch (Exception ex)
        {
            results.Add(ErrorResult("failed to enumerate", ex.Message));
            return results;
        }

        var ejectable = string.IsNullOrEmpty(term)
            ? devices
            : devices.Where(d =>
                d.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || d.DriveLettersDisplay.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (d.VolumeLabel ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)
            ).ToList();

        if (ejectable.Count == 0 && devices.Count == 0)
            results.Add(ErrorResult("no usb drives detected", ""));
        else
            foreach (var d in ejectable)
                results.Add(BuildEjectResult(d));

        return results;
    }

    private Result BuildEjectResult(UsbDevice device)
    {
        var label = string.IsNullOrWhiteSpace(device.VolumeLabel)
            ? device.DisplayName
            : device.VolumeLabel;
        var drive = device.DriveLettersDisplay;
        var warn  = device.HasOpenHandles ? " !!" : "";

        return new Result
        {
            Title    = $"{drive} {label}{warn}",
            SubTitle = device.HasOpenHandles ? $"{device.SpaceDisplay}  ·  drive in use" : device.SpaceDisplay,
            IcoPath  = Icon,
            Action   = _ => ExecuteEject(device)
        };
    }

    private bool ExecuteEject(UsbDevice device)
    {
        var r      = DeviceEjectService.Eject(device.PnpDeviceId);
        var prefix = device.DriveLetters.Count > 0 ? string.Join(", ", device.DriveLetters) + "  " : "";
        switch (r.Status)
        {
            case EjectStatus.Success:
                _context.API.ShowMsg("mnt", $"{prefix}{device.DisplayName} ejected"); break;
            case EjectStatus.Vetoed:
                _context.API.ShowMsg("mnt: eject vetoed", r.Message); break;
            default:
                _context.API.ShowMsg("mnt: error", r.Message); break;
        }
        return true;
    }


    private static Result ErrorResult(string title, string sub) => new()
    {
        Title    = title,
        SubTitle = sub,
        IcoPath  = Icon,
        Action   = _ => true
    };
}
