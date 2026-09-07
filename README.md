# SteamSaveSync

Windows desktop application for automatically discovering Steam games and preparing two-way game-save synchronization between a gaming PC and a handheld PC.

## V1 goals
- Scan all Steam libraries.
- Discover installed games and AppIDs.
- Detect known save-game locations.
- Let the user enable or disable synchronization per game.
- Integrate with Syncthing as the transport layer.
- Protect against conflicts and preserve backups.

## Planned stack
- C# / .NET 8
- WPF desktop UI
- Windows background tray mode

> Initial project scaffold.