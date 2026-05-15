# unplug me!

> for safety reasons :)

flow launcher plugin to list & eject connected usb, uas & scsi devices

## usage

prefix: `mnt`

- `mnt` - list all connected usb drives
- `mnt <term>` - filter by model/drive letter/label

## build

```powershell
cd src
dotnet build -c Release
```

copy all contents from `src/bin/Release` into your flow launcher plugin folder, e.g. `src/bin/Release` --> `Plugins/eject-flow/`
