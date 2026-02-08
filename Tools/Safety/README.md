# Safety Tools

This folder contains safety helpers to avoid losing work and to keep a clear log.

## Main command

`powershell -ExecutionPolicy Bypass -File Tools/Safety/SessionGuard.ps1`

## Start a session

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Safety/SessionGuard.ps1 `
  -Action start `
  -Task "Fix draw/discard/shadow" `
  -Plan "Read logs, checkpoint, patch scripts, build"
```

What it does:
- creates `Docs/SessionLogs/Session-<id>.md`
- creates an initial checkpoint in `Docs/Checkpoints/<id>-start`
- writes `Docs/SessionHistory.csv`

## Create an extra checkpoint

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Safety/SessionGuard.ps1 `
  -Action checkpoint `
  -Paths Assets/Scripts/UI/GameBootstrap.cs,Assets/Scenes/Game.unity
```

## Finish a session

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Safety/SessionGuard.ps1 `
  -Action finish `
  -Status resolved `
  -Result "Visual restored and build passing"
```

## Check active session

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Safety/SessionGuard.ps1 -Action status
```

## List checkpoints

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Safety/SessionGuard.ps1 -Action list-checkpoints
```
