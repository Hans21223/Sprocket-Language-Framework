Sprocket Language Framework 0.2.0 - Sprocket in another language (Thai included), for Sprocket 0.2.55.5 and 0.2.56.0
https://github.com/Hans21223/Sprocket-Language-Framework

INSTALL
Needs the Sprocket Mod Loader (BepInEx 6 IL2CPP). Add the zip in Sprocket Mod Manager, or copy its BepInEx folder into
the Sprocket folder.

USE
In the game: Settings > General > Language, then pick ไทย (Thai). It switches at once; pick English to go back.
เลือกภาษาในเกม: Settings > General > Language แล้วเลือก ไทย (Thai)

- The game's menus, editor, settings, part names and descriptions, scenarios and driving HUD, and Quality of Life's
  text, where the language file has them. Names you type are never translated.
- Thai letters use Windows' own Leelawadee UI font; nothing to install.

SPROCKET MOD API (optional)
With the Sprocket Mod API (github.com/furryaxw/SprocketModAPI) installed, Language is in its Mod menu (Settings >
General > MODS) and the language can be picked there too. Without it, nothing changes.

LANGUAGE FILES
Languages\<code>.txt next to SprocketLanguage.dll (th.txt for Thai), in XUnity.AutoTranslator's text format:
English=translation on each line, \n for a line break, {{A}} for a number. Every file there shows in the Language
list. After editing a file, switch the language to English and back to read it again.

Missing or odd text? Open an issue on GitHub with a screenshot.
