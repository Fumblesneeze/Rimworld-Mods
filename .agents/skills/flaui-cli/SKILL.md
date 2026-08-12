---
name: flaui-cli
description: Automate and verify Windows desktop UI applications (WPF, WinForms, Win32) using FlaUiCli. Use when tasks involve interacting with Windows app windows/elements, clicking, typing, screenshots, waits, or desktop UI verification.
---

# FlaUiCli

Use this skill when you need to verify or automate Windows desktop UI applications.

## Required Setup

- This skill is Windows-only.
- Ensure `flaui` is installed and available:

```powershell
irm https://raw.githubusercontent.com/opstudio-eu/FlaUiCli/master/install.ps1 | iex
```

- Verify installation:

```powershell
flaui --help
```

## Core Rules

- For Windows desktop UI verification/automation tasks, use `flaui` instead of guessing UI state.
- Connect to the target process first, then discover elements, then perform actions.
- Prefer non-destructive verification first (screenshots, element tree, reads) before mutating actions.
- Element IDs are session-scoped; reacquire IDs after reconnecting.
- Validate outcomes after each action (`get`, `wait`, `screenshot`) and report the result.
- Disconnect and stop service when done.

## Workflow

1. Prepare and connect:
- `flaui service start`
- `flaui process list`
- `flaui connect --name "AppName"` or `flaui connect --pid 1234`
- `flaui status`

2. Inspect UI state:
- `flaui window list`
- `flaui screenshot --output before.png`
- `flaui element tree --depth 3`
- `flaui element find --aid "SubmitButton" --first`

3. Act on elements:
- `flaui action click <id>`
- `flaui action type <id> "text"`
- `flaui action select <id> "item"`
- `flaui action toggle <id>`

4. Verify outcomes:
- `flaui get text <id>`
- `flaui get value <id>`
- `flaui get state <id>`
- `flaui wait element --name "Success" --timeout 5000`
- `flaui screenshot --output after.png`

5. Clean up:
- `flaui disconnect`
- `flaui service stop`

## Troubleshooting

- If target app is not listed in `process list`, ensure it is running with a visible window.
- If element lookup returns no matches, use `element tree` and broaden search criteria (`--name`, `--type`, `--class`).
- If the command is not found after install, restart the terminal to pick up PATH updates.
