# Safe Coding Protocol

This is the protocol I will follow every time before coding changes in this project.

1. I will state clearly what I am going to change before editing files.
2. I will create a session log and an initial checkpoint before risky edits.
3. I will keep progress updates short and frequent while I work.
4. I will run build/tests after edits.
5. I will always report final status as:
- `resolved` when requested behavior is restored and checks pass.
- `not_resolved` when there are blockers or incomplete validation.

## Required commands

### Start
```powershell
powershell -ExecutionPolicy Bypass -File Tools/Safety/SessionGuard.ps1 -Action start -Task "<task>" -Plan "<plan>"
```

### Extra checkpoint
```powershell
powershell -ExecutionPolicy Bypass -File Tools/Safety/SessionGuard.ps1 -Action checkpoint -Paths Assets/Scripts,Assets/Scenes
```

### Finish
```powershell
powershell -ExecutionPolicy Bypass -File Tools/Safety/SessionGuard.ps1 -Action finish -Status resolved -Result "<result>"
```
