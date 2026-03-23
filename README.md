# Re_Stick-It

Lightweight desktop sticky notes for Windows, built with WPF on `.NET 8`.

## Features

- Multiple notes with independent size, position, and content
- Rich text persistence (RTF + plain text fallback)
- Auto-save with debounce (typing/move/resize safe)
- System tray integration:
  - New note
  - Minimize/restore/show all notes
  - Save all notes
  - Exit app
- Single-instance app behavior
- Restore notes on startup (with monitor-aware placement)
- Optional “keep notes inside desktop area”
- Preferences for theme, taskbar/tray visibility, startup behavior, and default font
- Note skins (built-in + user-defined)
- Optional sticky target behavior for notes

## Tech Stack

- `C# 12`
- `.NET 8`
- `WPF` (Windows Desktop)

## Getting Started

### Prerequisites

- Windows 10/11
- `.NET 8 SDK`
- Visual Studio 2022/2026 with WPF workload

### Build & Run

1. Clone the repo
2. Open the solution in Visual Studio
3. Set `Re_Stick-It` as startup project
4. Build and run (`F5`)

## Project Structure (high level)

- `Re_Stick-It/App.xaml.cs` — app lifecycle, window spawning, save queue, tray behavior
- `Re_Stick-It/Notes/` — note windows and note management UI
- `Re_Stick-It/Persistence/` — JSON storage + persisted models
- `Re_Stick-It/Services/` — tray, theme, startup registry, monitor affinity, etc.
- `Re_Stick-It/Sticky/` — sticky target support/services

## Persistence

Application state is persisted as JSON and includes:

- Notes (geometry, content, title, color/skin, font, sticky metadata)
- App preferences
- User-defined skins

## Screenshots

> I mean, you've seen the small squares of papeer having adhesive on them with all the wild color choices out in the real world, right? 
> Ok, fine, I'll get around to making some screenshots sooner or later...

## Roadmap

- [ ] Search/filter notes
- [ ] Import/export notes
- [ ] Optional cloud sync
- [ ] Keyboard shortcut customization

## Contributing

- Feel free to fix any mistakes I've made (including this haircut! You're just lucky you don't have to see it).
- Um, I wasn't originally going to make this available to the public for all the reasons, but i did, so don't make regret it by making me fix a bunch of code! 
- Insstead, just make co-pilot vibe it out.

## Conclusions

- Ok, now get off your computer or phone and go outside. 

- Better yet, go and ask her/him out!!

- You'll only regret it if you don't!!
