# unplug me!

flow launcher plugin to list & eject connected usb, uas & scsi devices

## usage

prefix: `mnt`

- `mnt` - list all connected usb drives
- `mnt <term>` - filter by model/drive letter/label

## build

```powershell
cd src
dotnet build -c release
```

copy all contents from `src/bin/release` into your flow launcher plugin folder, e.g. `src/bin/release` --> `plugins/unplug/`
