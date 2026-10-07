# Soundboard

**Work in progress.** Both the project and this README are unfinished and will change a lot.

A Windows soundboard written in C# with Avalonia UI (.NET 10).

Launch it, pick a folder, and it scans that folder and all subfolders for audio files (mp3, wav, ogg). Each file becomes a button. Click to play, and click again to play it over itself.

- Subfolders become category tabs, each with its own color
- Multiple categories can be selected at once to filter the grid
- Stop All button
- Sounds mix together, so overlapping playback works

Uses NAudio for playback, CommunityToolkit.Mvvm for MVVM, and YamlDotNet for settings.

## Roadmap / Next

- [x] Dark mode support
- [x] General UI redesign (Avalonia port)
- [x] Refactor viewmodel - split to services- [ ] 
- [ ] Expand Color Palette
- [ ] Cleanup
- [ ] Volume
- [ ] Indicate how many instances of a sound play at a time
