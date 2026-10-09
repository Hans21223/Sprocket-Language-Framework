# Sprocket Language Framework

Play [Sprocket](https://store.steampowered.com/app/1674170/Sprocket/) in another language. Thai (ภาษาไทย) is included.
Choose the language in the game: **Settings → General → Language**. It switches at once, and back to English the same
way.

For **Sprocket 0.2.55.5 and 0.2.56.0** with the [Sprocket Mod Loader](https://github.com/Hans21223/Sprocket-Mod-Loader)
(BepInEx 6 IL2CPP). With the optional [Sprocket Mod API](https://github.com/furryaxw/SprocketModAPI) installed, the
language can also be picked on this mod's page in its Mod menu (Settings → General → MODS).

## ภาษาไทย

เล่น Sprocket เป็นภาษาไทย: เมนู ห้องออกแบบรถ การตั้งค่า ชื่อและคำอธิบายชิ้นส่วน ฉากภารกิจ และหน้าจอขณะขับ
รวมถึงข้อความของม็อด [Quality of Life](https://github.com/Hans21223/Sprocket-Quality-of-Life)

1. ติดตั้ง [Sprocket Mod Loader](https://github.com/Hans21223/Sprocket-Mod-Loader) (ถ้ายังไม่มี)
2. ใน Sprocket Mod Manager กด **Add mod** แล้วเลือกไฟล์ ZIP ของม็อดนี้จากหน้า Releases
3. เปิดเกม ไปที่ **Settings → General → Language** แล้วเลือก **ไทย (Thai)**

ตัวอักษรไทยใช้ฟอนต์ Leelawadee UI ที่มากับ Windows จึงไม่ต้องติดตั้งฟอนต์เพิ่ม
เจอคำที่ยังเป็นภาษาอังกฤษหรือคำแปลที่ไม่เหมาะ แจ้งได้ที่ Issues พร้อมภาพหน้าจอ

## What it does

- **The game's text**: menus, the vehicle editor, settings, part names and descriptions, scenarios, the driving HUD and
  key hints. Text is swapped as the game shows it, so nothing in the game's files is changed.
- **Other mods' text** too, where the language file has it. The Thai file covers
  [Quality of Life](https://github.com/Hans21223/Sprocket-Quality-of-Life)'s panels, tooltips and messages.
- **Fonts**: the game's fonts have no Thai (or Chinese) letters, so fonts made from Windows' own **Leelawadee UI** (Thai)
  and **Microsoft YaHei** (Chinese) are used behind them. Nothing is downloaded or shipped.
- **Left alone**: names you type (a design called "Engine" stays "Engine"), and text drawn into pictures.

## Install

With **Sprocket Mod Manager**: **Add mod**, choose the ZIP from [Releases](../../releases). By hand: copy the ZIP's
`BepInEx` folder into the Sprocket folder. Then start the game and pick the language in **Settings → General →
Language**. The choice is remembered (`BepInEx\config\local.sprocket.language.cfg`).

## Language files

Each language is one text file in `BepInEx\plugins\SprocketLanguage\Languages`, named by its code (`th.txt`). Every file
there shows up in the Language list. The format is
[XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator)'s, so an existing Sprocket pack for it works
as it is:

```text
// a comment
Mantlet=หน้ากากปืน
Next mission (ENTER)\nReplay (SPACE)=ภารกิจถัดไป (ENTER)\nเล่นซ้ำ (SPACE)
{{A}}m {{B}}s Elapsed=ผ่านไป {{A}} นาที {{B}} วินาที
r:"^(.+) \[Gunner\]$"=$1 [พลยิง]
```

- One `English=translation` a line. `\n` is a line break, `\=` an equals sign inside the text.
- `{{A}}`, `{{B}}`… stand for numbers, so one line covers "40 km/h" and "12.5 km/h".
- `r:"pattern"=replacement` lines are regular expressions (`$1` for a group), tried when nothing else matches.
- A text of several lines with no line of its own is translated line by line.

Edit the file, then in the game switch the language to English and back: it reads the file again.

The English lines in `th.txt` come from a community Chinese pack for XUnity.AutoTranslator; the Thai is new. Corrections
are welcome as issues or pull requests.

## Building

Needs the .NET 8 SDK and a Sprocket folder with the mod loader installed and started once.

```bash
dotnet build SprocketLanguage -c Release -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Sprocket"
```

```bash
dotnet run --project SprocketLanguage.Tests -c Release
```

The tests read the shipped Thai file without the game. [docs/Making-Mods-for-Sprocket.pdf](docs/Making-Mods-for-Sprocket.pdf)
explains how Sprocket mods like this one are made.

## License

MIT, see [LICENSE](LICENSE).
