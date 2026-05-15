using System;
using System.Collections.Generic;

namespace eject_flow.Models;

public sealed class UsbDevice
{
    public string PhysicalDeviceId { get; init; } = "";
    public string PnpDeviceId      { get; init; } = "";
    public string Model            { get; init; } = "";
    public ulong  SizeBytes        { get; init; }
    public List<string> DriveLetters { get; init; } = new();
    public string? VolumeLabel     { get; init; }
    public ulong UsedBytes         { get; init; }
    public ulong FreeBytes         { get; init; }
    public DateTime? ConnectedAt   { get; init; }
    public bool HasOpenHandles     { get; init; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Model) ? "usb device" : Model.Trim();

    public string DriveLettersDisplay =>
        DriveLetters.Count == 0 ? "no mounted volumes" : string.Join(", ", DriveLetters);

    public string SpaceDisplay =>
        SizeBytes > 0 ? $"{FormatBytes(UsedBytes)} / {FormatBytes(SizeBytes)}" : "";

    private static string FormatBytes(ulong bytes)
    {
        if (bytes == 0) return "0 b";
        string[] units = { "b", "kb", "mb", "gb", "tb" };
        double s = bytes;
        int u = 0;
        while (s >= 1024 && u < units.Length - 1) { s /= 1024; u++; }
        return $"{s:0.#} {units[u]}";
    }
}
