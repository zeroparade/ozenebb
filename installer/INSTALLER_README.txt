Esoteric Ebb Voice Override

Install:
1. Extract the release ZIP into the folder containing Esoteric Ebb.exe.
2. Double-click Install.bat.
3. Wait for the success message, then launch the game.

The installer downloads the pinned BepInEx IL2CPP x64 build when needed, then installs the complete sharded voice pack. Later voice updates download only changed shards; in-game mod updates verify a GitHub checksum and require a restart.

Controls:
F1 toggles custom voices. Original VO is blocked while custom voices are on.
F6 marks the latest line for live repair.
F7 replays the latest live-fix or shipped WAV.
F8 toggles live-fix capture and lookup.
F9 installs mod and voice-pack updates. Restart the game after a mod update.
F10 reports the latest dialogue key, speaker, and text.
F11 toggles recurring update notifications.
F12 toggles playback diagnostics.

F2-F4 are unused.

Voice-pack state is stored in BepInEx/config/spore.esotericebb.voicepacks.json. Live repair files and events are stored under BepInEx/voice-live-fix.
