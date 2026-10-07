# Soundboard

**Work in progress.** Both the project and this README are unfinished and will change a lot.

A Windows soundboard written in C# with Avalonia UI (.NET 10).

Launch it, pick a folder, and it scans that folder and all subfolders for audio files (mp3, wav, ogg). Each file becomes a button. Click to play, and click again to play it over itself.

- Subfolders become category tabs, each with its own color
- Multiple categories can be selected at once to filter the grid
- Stop All button
- Sounds mix together, so overlapping playback works

Uses NAudio for playback, CommunityToolkit.Mvvm for MVVM.

## Critical

- [x] **Unicode path support**: Support audio files and directories with non-ANSI / Unicode characters in paths (e.g. accents, special symbols) by passing .NET UTF-16 `FileStream`s into the reader instead of ANSI paths.
- [ ] **FL Studio Vorbis-in-WAV format (`0x674F` / `0x6750`)**: Support decoding FL Studio WAV files that embed raw `OggS` Vorbis bitstreams in their data chunks via on-demand streaming `SubStream`.
- [x] **Hook fade in/out**: Connect and fix fade in / fade out functionality during sound playback.

## Roadmap / Next

- [x] Dark mode support
- [x] General UI redesign (Avalonia port)
- [x] Refactor viewmodel - split to services
- [x] Expand Color Palette
- [ ] Cleanup
- [x] Volume
- [ ] Category panel min/max sizing (quantized to fit integer number of category rows: 1 min, 4-5 max)
- [ ] Resizable category panel via GridSplitter with row snapping
- [ ] Indicate how many instances of a sound play at a time

