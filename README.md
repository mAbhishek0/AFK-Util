# AFK Bot

Simulates WASD keyboard inputs at the hardware scan-code level to keep your PC from going idle. Works with DirectX games since it uses `SendInput` with `KEYEVENTF_SCANCODE`.

Built with **WinUI 3** and **.NET 8**.

## Features

- Hardware scan-code input via `SendInput` — DirectX/DirectInput compatible
- Random keys, hold times (1–3s), and wait intervals (10–15s)
- 5-second countdown before starting
- Async input loop, doesn't freeze the UI
- Matches system light/dark theme
