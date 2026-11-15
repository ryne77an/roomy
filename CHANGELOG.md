# Changelog

## 0.2.0

- **Russian UI.** The whole app, share card, tray menu and Windows notifications are translated. The language follows the Windows display language; `--lang ru` / `--lang en` overrides it.
- Localization lives in one table and is covered by tests: missing keys, empty strings and mismatched `{0}` placeholders fail the build.
- **Temp folder guard.** `%TEMP%` cleanup now refuses folders that don't look like a temp location, so a redirected `TMP` variable can't point the cleaner at real documents.
- **Windows Update guard.** The update cache is skipped while Windows is actively installing updates.
- **Settings window layout fix.** Long hints wrapped incorrectly and cards were clipped on narrow windows; the options list now scrolls vertically only.
- Release builds now publish `SHA256SUMS.txt`.
- The installer ships English and Russian wizard languages.
- Smaller fixes: localized number/date formatting ("32,5 ГБ"), translatable plural forms ("1 файл / 2 файла / 5 файлов"), accessible names for the window buttons.

## 0.1.2

- **Settings window.** Choose whether Roomy starts with Windows and how it checks in the background.
- **Low disk space alerts.** Optional background check that lets you know when a drive is running low.
- **Weekly cache check.** Optional reminder when at least 1 GB of temp files and app caches could be cleared. Nothing is deleted without you reviewing it.
- Only one copy of Roomy runs at a time; opening it again brings the existing window forward.
- The Settings window now shows the Roomy version.
- Clearer wording throughout the app.

## 0.1.1

- New graphite look for the app.
- Updated screenshots and demo.

## 0.1.0

- First release: scan, plain-English results, space map, biggest files and share card.
