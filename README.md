# Esoteric Ebb Voice Override

Source for the Esoteric Ebb IL2CPP voice mod, installer, and sharded voice-pack updater. The shared runtime and distribution infrastructure is ported from [disco_parade](https://github.com/zeroparade/disco_parade); only the game adapter is title-specific.

## Layout

```text
src/
  VoiceOverridePlugin.cs       shared playback, live-fix, hotkeys, updater, diagnostics
  EsotericDialogueAdapter.cs   Esoteric Ebb AddText/localization key resolution
  EsotericEbbVoiceOverride.csproj
installer/
config/
tools/
```

Generated audio is not stored in this source directory. Runtime files are addressed by dialogue key:

```text
BepInEx/voice-overrides/LL_Intro_1.wav
BepInEx/voice-overrides/_dialogue-map.tsv
```

The map format is `dialogue_key<TAB>display_text<TAB>speaker`. The adapter handles speaker labels, DC result prefixes, rich-text spacing, combined narration, and repeated text without consuming stale keys.

## Build

Set `ESOTERIC_EBB_GAME_DIR` to an Esoteric Ebb installation containing BepInEx, or pass `GameDir` to MSBuild explicitly:

```powershell
$env:ESOTERIC_EBB_GAME_DIR = "<path-to-compatible-BepInEx-IL2CPP-game>"
dotnet build .\src\EsotericEbbVoiceOverride.csproj -c Release
```

The built plugin is `src/bin/Release/net6.0/EsotericEbbVoiceOverride.dll`.

## Runtime features

- FMOD streaming with Unity/native fallback.
- Immediate live-fix overrides under `BepInEx/voice-live-fix/overrides`.
- JSONL dialogue/playback events and latest-line reports.
- In-game GitHub plugin and voice-pack update checks with recurring notifications.
- F9 installs checksum-verified plugin releases and changed voice shards; plugin updates activate after restart.
- Installer and in-game downloads retry transient timeouts and server errors without replacing valid local files.
- Original FMOD voice-over suppression while the custom pack is enabled.
- Startup status overlay with installed-line count and core controls.
- F1 custom-voice toggle; F6-F8 live-fix; F9 mod/voice updates; F10 report; F11 notifications; F12 diagnostics.

## Voice-pack publishing

Build stable hash-bucket shards from the installed production pack:

```powershell
python .\tools\build_voice_pack_shards.py `
  --game-root . `
  --output .\voice_packs `
  --base-url "https://huggingface.co/datasets/zeroparade/ozenebb/resolve/main"
```

Unchanged shards are reused. Published audio lives at [zeroparade/ozenebb](https://huggingface.co/datasets/zeroparade/ozenebb).

## Release packaging

The source repository does not commit compiled binaries. Build the project, then create the three GitHub Release assets:

```powershell
.\tools\make-github-release-assets.ps1
```

The lightweight installer ZIP contains no plugin binary. During installation it downloads `EsotericEbbVoiceOverride.dll` and `EsotericEbbVoiceOverride.dll.sha256` from the latest [GitHub Release](https://github.com/zeroparade/ozenebb/releases), verifies SHA-256, installs the plugin, and then downloads changed audio shards from Hugging Face.
