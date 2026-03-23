# Re_Stick-It

Lightweight desktop sticky notes for Windows, built with WPF on `.NET 8`, blah, blah, blah.

## So one day I was sitting in class, using an Appple and I noticed the version of 'Post-It" notes they get to use and play around with.
## And then I looked at the (a)version that Windows has and I got super jealous! It wasn't fair that Swift programs look way more polished and dont make me feel embarrassed to use them in public.

### Then the teacher got mad at me for interrupting and insanely yelling about how embarrassing the windows post-it note situation was. 
### The professor was not impressed with my lack of arguments presented.
### Six or eight months later I suddenly remembered that I needed to make a post-it note program for the raisins.  
-<u>But this one would more accurately mimick the real world.</u>-

### THIS meant: 
- no resizing
- only the colors i could find at the store
- you can stick your notes to any surface and they will stay there for the most part (unless you have a ton of extra monitors/screens like i do (5 total!), that coding got really difficult)
- no videos or music! how tf would you do that in real life!?!? get real!!
- theres probably a bunch of other things I thought of and already forgot about too.

### So, before you read the next section -OR- use the app, just keep in mind I was aiming high for realism.

## So, then I found that f(*&ng program 'Notezilla' and saw it is anti-free and how much they they think it is worth.   

## I ignorantly turned and stated to a random passerby, "I can do what you do!!! You better watch out!"

## I startled them so profusely that they punched me to get away--strong tactic. 

### i thought about what I had done and the response I got in return. I have to say, yes, I agree with that silent cheap-shotting-stranger: screw capitalism!

#
#

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
