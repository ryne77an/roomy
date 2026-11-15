using System.Globalization;

namespace Roomy.Core;

/// <summary>
/// Tiny two-language string table (English + Russian). Chosen once at startup from the OS UI
/// language or the --lang flag, so XAML can resolve strings while it is being parsed.
/// </summary>
public static class Loc
{
    public const string English = "en";
    public const string Russian = "ru";

    public static string Code { get; private set; } = English;
    public static bool IsRu => Code == Russian;

    public static string Pick(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            var code = requested.Trim().ToLowerInvariant();
            if (code.StartsWith("ru", StringComparison.Ordinal)) return Russian;
            if (code.StartsWith("en", StringComparison.Ordinal)) return English;
        }
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase)
            ? Russian
            : English;
    }

    /// <summary>Applies the UI language and the matching number/date formatting culture.</summary>
    public static void Use(string? requested)
    {
        Code = Pick(requested);
        var culture = CultureInfo.GetCultureInfo(Code == Russian ? "ru-RU" : "en-US");
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
    }

    public static string T(string key)
    {
        if ((Code == Russian ? Ru : En).TryGetValue(key, out var value)) return value;
        return En.TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string T(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(key), args);

    /// <summary>Every key the app knows about (used by the localization tests).</summary>
    public static IReadOnlyCollection<string> Keys => En.Keys;

    internal static IReadOnlyDictionary<string, string> Table(string code) => code == Russian ? Ru : En;

    /// <summary>Russian plural form for a count: 1 файл / 2 файла / 5 файлов.</summary>
    public static string N(long n, string one, string few, string many)
    {
        var form = PluralIndex(Code, n);
        return form switch
        {
            0 => T(one),
            1 => T(few),
            _ => T(many),
        };
    }

    /// <summary>0 = one, 1 = few, 2 = many. English only distinguishes one and many.</summary>
    internal static int PluralIndex(string code, long n)
    {
        if (code != Russian) return n == 1 ? 0 : 2;
        var mod10 = n % 10;
        var mod100 = n % 100;
        if (mod10 == 1 && mod100 != 11) return 0;
        if (mod10 is >= 2 and <= 4 && mod100 is < 12 or > 14) return 1;
        return 2;
    }

    static readonly Dictionary<string, string> En = new(StringComparer.Ordinal)
    {
        // ---------- shared ----------
        ["Cancel"] = "Cancel",
        ["RestartAsAdmin"] = "Restart as admin",
        ["BadgeAdmin"] = "ADMIN",
        ["BadgeSafe"] = "SAFE",
        ["BadgeReview"] = "YOUR CALL",
        ["BadgeNeedsAdmin"] = "NEEDS ADMIN",
        ["TooltipSettings"] = "Settings",
        ["TooltipShowInExplorer"] = "Show in File Explorer",
        ["TooltipOpenInExplorer"] = "Open in File Explorer",
        ["TooltipUp"] = "Up one folder",
        ["TooltipMinimize"] = "Minimize",
        ["TooltipMaximize"] = "Maximize",
        ["TooltipClose"] = "Close",

        // ---------- home ----------
        ["HomeTitle"] = "Let's make some room.",
        ["HomeSubtitle"] = "Roomy finds what's filling up your PC and tells you, in plain English, what's safe to delete.",
        ["HomeScan"] = "Scan my PC",
        ["HomeScanHint"] = "Takes a few seconds. Nothing gets deleted until you say so.",

        // ---------- drives ----------
        ["DriveLocalDisk"] = "Local Disk",
        ["DriveSubtitle"] = "{0} drive",
        ["DriveFreeRest"] = "free of {0}",
        ["DriveFreeShort"] = "{0} free",
        ["DriveUsed"] = "{0} used",
        ["DrivePercentFull"] = "{0}% full",
        ["DriveAlmostFull"] = "Almost full",
        ["DriveGettingFull"] = "Getting full",
        ["DriveLookingGood"] = "Looking good",

        // ---------- scanning ----------
        ["ScanTitle"] = "Looking around your PC…",
        ["ScanFoundPrefix"] = "Found ",
        ["ScanFoundSuffix"] = " you can safely clean so far",
        ["ScanChecking"] = "Checking {0}…",
        ["ScanFailed"] = "The scan hit a problem: {0}",

        // ---------- results ----------
        ["ResultsWeFound"] = "We found",
        ["ResultsSafeToClean"] = "safe to clean",
        ["TabCleanup"] = "Cleanup",
        ["TabSpaceMap"] = "Space map",
        ["AdminBannerTitle"] = "Windows keeps some of the biggest leftovers locked.",
        ["AdminBannerBody"] = "Restart Roomy as admin to check Windows Update files too.",
        ["NothingFoundTitle"] = "Your PC is already squeaky clean ✨",
        ["NothingFoundBody"] = "Check the Space map tab to see what's actually using your space.",
        ["SelectedToClean"] = "Selected to clean",
        ["ScanAgain"] = "Scan again",
        ["CleanUpAmount"] = "Clean up {0}",
        ["PickToClean"] = "Pick something to clean",
        ["ReviewMore"] = "Plus {0} more that's worth a look. That part is your call.",
        ["ReviewNone"] = "That's everything we found.",
        ["HiddenItems"] = "+ {0} more {1} (included when the box is ticked)",
        ["RemoveWithWindows"] = "Remove it with Windows",
        ["WindowsCleanupPath"] = "Settings → Storage → Temporary files → Previous Windows installation(s)",
        ["UninstallInSteam"] = "Uninstall in Steam",

        // ---------- space map ----------
        ["SpaceEmptyTitle"] = "See exactly what's using your space",
        ["SpaceEmptyBody"] = "Pick a drive and hit Scan. Big drives can take a minute.",
        ["SpaceScanning"] = "Mapping your drive…",
        ["SpaceBiggestTitle"] = "Biggest files",
        ["SpaceBiggestBody"] = "The single files taking up the most room.",
        ["SpaceProgress"] = "{0} files · {1}",
        ["SpaceFilesInFolder"] = "Files in this folder ({0})",
        ["SpaceSystemFiles"] = "System files (pagefile, hibernation & co.)",
        ["SpaceLocked"] = "Locked system files Roomy can't look inside",
        ["SpaceScan"] = "Scan",
        ["SpaceRescan"] = "Rescan",
        ["SpaceStarting"] = "Starting…",
        ["SpaceMapFailed"] = "Couldn't map that drive: {0}",

        // ---------- cleaning ----------
        ["CleanTitle"] = "Cleaning up…",
        ["CleanHint"] = "Files that are in use get skipped, never forced.",
        ["CleanStep"] = "Cleaning {0}…",
        ["CleanGettingStarted"] = "Getting started…",
        ["CleanAllDone"] = "All done",
        ["CleanStopped"] = "Cleaning stopped: {0}",
        ["ConfirmDeleteTitle"] = "Delete these for good?",
        ["ConfirmDeleteBody"] = "You picked {0} {1} from your Downloads ({2}). They'll be deleted permanently, not moved to the Recycle Bin, so make sure you don't need them.",
        ["ConfirmDeleteYes"] = "Delete permanently",
        ["LetCleanupFinishClose"] = "Let the cleanup finish before closing Roomy.",
        ["LetCleanupFinishQuit"] = "Let the cleanup finish before quitting Roomy.",

        // ---------- done ----------
        ["DoneFreed"] = "You just freed",
        ["DoneSubtitle"] = "Your {0} drive now has {1} free.",
        ["Before"] = "Before",
        ["After"] = "After",
        ["ShareResult"] = "Share my result",
        ["Done"] = "Done",
        ["Skipped"] = "{0} was in use by open apps, so we left it alone. Close them and scan again to grab it.",
        ["ShareCopied"] = @"Copied! Paste it anywhere. Also saved to Pictures\Roomy.",
        ["ShareFailed"] = "Couldn't make the image: {0}",

        // ---------- file kinds ----------
        ["FileOne"] = "file",
        ["FileFew"] = "file",
        ["FileMany"] = "files",
        ["ItemOne"] = "item",
        ["ItemFew"] = "item",
        ["ItemMany"] = "items",
        ["DetailFiles"] = "{0} {1}",
        ["DetailItems"] = "{0} {1}",

        // ---------- ago ----------
        ["AgoToday"] = "today",
        ["AgoYesterday"] = "yesterday",
        ["AgoDays"] = "{0} days ago",
        ["AgoWeeks"] = "{0} weeks ago",
        ["AgoMonths"] = "{0} months ago",
        ["AgoYear"] = "a year ago",
        ["AgoYears"] = "{0} years ago",
        ["AgoShortToday"] = "today",
        ["AgoShortDays"] = "{0} days",
        ["AgoShortWeeks"] = "{0} weeks",
        ["AgoShortMonths"] = "{0} months",
        ["AgoShortYear"] = "a year",
        ["AgoShortYears"] = "{0} years",

        // ---------- category copy ----------
        ["CatRecycleTitle"] = "Recycle Bin",
        ["CatRecycleBody"] = "Stuff you already deleted. It still takes up space until the bin is emptied.",
        ["CatTempTitle"] = "Temporary files",
        ["CatTempBody"] = "Leftovers apps create while they work and forget to clean up. Only files older than a day.",
        ["CatBrowserTitle"] = "Browser cache",
        ["CatBrowserBody"] = "Saved copies of websites. You stay logged in. Sites may just load a little slower the first time.",
        ["CatAppsTitle"] = "App caches",
        ["CatAppsBody"] = "Cached data from Discord, VS Code, Slack and friends. They rebuild it on their own when needed.",
        ["CatShadersTitle"] = "Graphics shader cache",
        ["CatShadersBody"] = "Game graphics your GPU pre-built. Safe to clear. Games rebuild it, but the next launch may stutter a bit.",
        ["CatCrashTitle"] = "Crash reports",
        ["CatCrashBody"] = "Reports Windows saves whenever an app crashes. Only useful to developers.",
        ["CatWindowsTitle"] = "Windows Update leftovers",
        ["CatWindowsBody"] = "Update files Windows already installed but keeps around just in case, plus Windows' own temp files.",
        ["CatInstallersTitle"] = "Old installers",
        ["CatInstallersBody"] = "Setup files in your Downloads you've most likely already run. Only ones older than a week.",
        ["CatDownloadsTitle"] = "Big old downloads",
        ["CatDownloadsBody"] = "Large files in your Downloads you haven't touched in over a month: old videos, ISOs, zips.",
        ["CatGamesTitle"] = "Games you haven't played",
        ["CatGamesBody"] = "Steam games you haven't opened in 2+ months. Uninstall through Steam. You can always reinstall.",
        ["CatWindowsOldTitle"] = "Previous Windows version",
        ["CatWindowsOldBody"] = "A backup of your old Windows from the last big update. You only need it if you want to roll back.",

        // ---------- scanner notes ----------
        ["NoteAppsOpen"] = "Open right now: {0}. Some files are locked — close them first to clean everything.",
        ["NoteWindowsLocked"] = "Windows locks these. Restart Roomy as admin to check them.",
        ["NoteUpdating"] = "Windows is installing updates right now, so the update cache was skipped.",
        ["StepRecycle"] = "Recycle Bin",
        ["StepTemp"] = "Temporary files",
        ["StepBrowser"] = "Browser cache",
        ["StepApps"] = "App caches",
        ["StepShaders"] = "Shader cache",
        ["StepCrash"] = "Crash reports",
        ["StepWindows"] = "Windows Update",
        ["StepInstallers"] = "Old installers",
        ["StepDownloads"] = "Big downloads",
        ["StepGames"] = "Unplayed games",
        ["StepWindowsOld"] = "Old Windows",
        ["ItemYourTemp"] = "Your temp folder",
        ["ItemTeamsClassic"] = "Microsoft Teams (classic)",
        ["ItemVSCode"] = "VS Code",
        ["ItemSteamWeb"] = "Steam's web browser",
        ["ItemSteamGames"] = "Steam games",
        ["ItemAppCrashDumps"] = "App crash dumps",
        ["ItemWindowsErrorReports"] = "Windows error reports",
        ["ItemDownloadedUpdates"] = "Downloaded updates",
        ["ItemDeliveryOptimization"] = "Delivery Optimization",
        ["ItemWindowsTemp"] = "Windows temp folder",
        ["ItemSystemDumps"] = "System crash dumps",
        ["DetailDownloaded"] = "Downloaded {0}",
        ["DetailUntouched"] = "Untouched for {0}",
        ["DetailLastPlayed"] = "Last played {0}",
        ["DetailNeverPlayed"] = "Never played",

        // ---------- settings ----------
        ["SettingsTitle"] = "Roomy settings",
        ["SettingsHeader"] = "A little help in the background",
        ["SettingsHeaderHint"] = "Choose when Roomy checks in. Everything stays on this PC.",
        ["SetTray"] = "Keep Roomy in the system tray",
        ["SetTrayHint"] = "Closing the window keeps checks running. Use Quit Roomy in the tray to exit.",
        ["SetStartup"] = "Start with Windows",
        ["SetStartupHint"] = "Start quietly in the tray when you sign in.",
        ["SetStartupButton"] = "Windows startup settings",
        ["SetLowSpace"] = "Tell me when my system drive is low",
        ["SetLowSpaceHint"] = "A quick free-space check. At most one reminder per day.",
        ["SetBelow"] = "Below",
        ["SetGbEvery"] = "GB, check every",
        ["Set30"] = "30 minutes",
        ["Set60"] = "60 minutes",
        ["SetWeekly"] = "Check for cache buildup weekly",
        ["SetWeeklyHint"] = "Checks temp files and app caches while you're idle and plugged in. Reminds you when at least 1 GB could be cleared. You review and choose what to delete.",
        ["SetPause"] = "Pause for a day",
        ["SetResume"] = "Resume",
        ["SetNotifButton"] = "Windows notification settings",
        ["SetSave"] = "Save settings",
        ["SetVersion"] = "Roomy {0}",
        ["SetVersionName"] = "Roomy version",
        ["SetThresholdName"] = "Low-space threshold in GB",
        ["SetIntervalName"] = "Check interval",
        ["PauseUntil"] = "Reminders paused until {0}.",
        ["PauseDefault"] = "Reminders follow your Windows notification settings.",
        ["StartBlocked"] = "Windows has disabled Roomy at startup. You can enable it in Windows Startup apps.",
        ["StartPolicy"] = "Your organization controls this startup setting.",
        ["StartWindowsManages"] = "Windows manages startup for this app. Change that setting in Windows first.",
        ["StartDenied"] = "Windows didn't allow the startup change.",
        ["ThresholdInvalid"] = "Choose a free-space threshold from 1 to 500 GB.",
        ["SaveFailed"] = "Couldn't save settings: {0}",
        ["SaveFailedCheckStartup"] = " Check Windows Startup apps to confirm its current setting.",
        ["StartupReadFailed"] = "Couldn't read the startup setting: {0}",
        ["StatusDemo"] = "Demo settings are temporary. Startup and Windows notifications are disabled.",
        ["StatusElevated"] = "Roomy is running as administrator. Settings are saved and apply the next time you start Roomy normally; background checks don't run in this mode.",
        ["CantFindExe"] = "Can't find Roomy's executable.",
        ["StartupNeedsExe"] = "Run the Roomy executable to enable startup.",
        ["StartupPathTooLong"] = "Move Roomy to a shorter folder path before enabling startup.",

        // ---------- tray & notifications ----------
        ["TrayOpen"] = "Open Roomy",
        ["TrayScan"] = "Scan now",
        ["TrayPause"] = "Pause reminders until tomorrow",
        ["TrayResume"] = "Resume reminders",
        ["TraySettings"] = "Settings",
        ["TrayQuit"] = "Quit Roomy",
        ["NotifyLowTitle"] = "Your {0} drive is getting full",
        ["NotifyLowBody"] = "{0} remaining. Open Roomy to see what you can clear.",
        ["NotifyWeeklyTitle"] = "Roomy found {0} of cleanup candidates",
        ["NotifyWeeklyBody"] = "Temporary files and app caches have built up. Review them when you're ready.",
        ["NotifyReview"] = "Review",
        ["NotifyTomorrow"] = "Tomorrow",
        ["NotifyTurnOff"] = "Turn off",
        ["NotifyUnavailable"] = "Windows notifications aren't available: {0}",
        ["NotifyFailed"] = "Couldn't send a Windows notification: {0}",
        ["SaveRemindersFailed"] = "Couldn't save your reminder settings: {0}",

        // ---------- app ----------
        ["AppCrashTitle"] = "Roomy hit a snag",
        ["AppAlreadyOpening"] = "Roomy is already opening. Try again in a moment.",
        ["DemoAdminToast"] = "That button restarts Roomy with admin rights.",
        ["NoAdminToast"] = "No worries, Roomy keeps working without admin.",
        ["PreferenceLoadError"] = "Couldn't read your saved settings. Background checks are off until you save them again.",

        // ---------- share card ----------
        ["ShareCardFreed"] = "I just freed",
        ["ShareCardFooter"] = "Free · No ads · No account",
        ["ShareCardOnPC"] = "on my PC",
        ["ShareCardOnDrive"] = "on my {0} drive",

        // ---------- demo data ----------
        ["DemoDriveLabelGames"] = "Games",
    };

    static readonly Dictionary<string, string> Ru = new(StringComparer.Ordinal)
    {
        // ---------- shared ----------
        ["Cancel"] = "Отмена",
        ["RestartAsAdmin"] = "Перезапустить от администратора",
        ["BadgeAdmin"] = "АДМИН",
        ["BadgeSafe"] = "БЕЗОПАСНО",
        ["BadgeReview"] = "РЕШАТЬ ВАМ",
        ["BadgeNeedsAdmin"] = "НУЖЕН АДМИН",
        ["TooltipSettings"] = "Настройки",
        ["TooltipShowInExplorer"] = "Показать в Проводнике",
        ["TooltipOpenInExplorer"] = "Открыть в Проводнике",
        ["TooltipUp"] = "На уровень выше",
        ["TooltipMinimize"] = "Свернуть",
        ["TooltipMaximize"] = "Развернуть",
        ["TooltipClose"] = "Закрыть",

        // ---------- home ----------
        ["HomeTitle"] = "Освободим немного места.",
        ["HomeSubtitle"] = "Roomy покажет, что занимает диск, и простыми словами объяснит, что можно удалить без риска.",
        ["HomeScan"] = "Проверить компьютер",
        ["HomeScanHint"] = "Займёт пару секунд. Ничего не удаляется без вашего подтверждения.",

        // ---------- drives ----------
        ["DriveLocalDisk"] = "Локальный диск",
        ["DriveSubtitle"] = "Диск {0}",
        ["DriveFreeRest"] = "свободно из {0}",
        ["DriveFreeShort"] = "{0} свободно",
        ["DriveUsed"] = "{0} занято",
        ["DrivePercentFull"] = "заполнено {0}%",
        ["DriveAlmostFull"] = "Почти заполнен",
        ["DriveGettingFull"] = "Заполняется",
        ["DriveLookingGood"] = "Всё в порядке",

        // ---------- scanning ----------
        ["ScanTitle"] = "Осматриваем компьютер…",
        ["ScanFoundPrefix"] = "Найдено ",
        ["ScanFoundSuffix"] = " — это можно безопасно удалить",
        ["ScanChecking"] = "Проверяем: {0}…",
        ["ScanFailed"] = "Во время проверки возникла проблема: {0}",

        // ---------- results ----------
        ["ResultsWeFound"] = "Мы нашли",
        ["ResultsSafeToClean"] = "можно безопасно удалить",
        ["TabCleanup"] = "Очистка",
        ["TabSpaceMap"] = "Карта места",
        ["AdminBannerTitle"] = "Часть самых больших остатков Windows не отдаёт без прав администратора.",
        ["AdminBannerBody"] = "Перезапустите Roomy от администратора, чтобы проверить и файлы обновлений Windows.",
        ["NothingFoundTitle"] = "На компьютере и так чисто ✨",
        ["NothingFoundBody"] = "Откройте «Карту места», чтобы увидеть, что на самом деле занимает диск.",
        ["SelectedToClean"] = "Выбрано к удалению",
        ["ScanAgain"] = "Проверить снова",
        ["CleanUpAmount"] = "Очистить {0}",
        ["PickToClean"] = "Выберите, что очистить",
        ["ReviewMore"] = "Ещё {0} — стоит посмотреть. Решать вам.",
        ["ReviewNone"] = "Это всё, что мы нашли.",
        ["HiddenItems"] = "Ещё {0} {1} (учитываются, если отметить галочку)",
        ["RemoveWithWindows"] = "Удалить средствами Windows",
        ["WindowsCleanupPath"] = "Параметры → Память → Временные файлы → Предыдущие установки Windows",
        ["UninstallInSteam"] = "Удалить в Steam",

        // ---------- space map ----------
        ["SpaceEmptyTitle"] = "Посмотрите, что именно занимает место",
        ["SpaceEmptyBody"] = "Выберите диск и нажмите «Сканировать». Большой диск может занять минуту.",
        ["SpaceScanning"] = "Строим карту диска…",
        ["SpaceBiggestTitle"] = "Самые большие файлы",
        ["SpaceBiggestBody"] = "Отдельные файлы, которые занимают больше всего места.",
        ["SpaceProgress"] = "{0} файлов · {1}",
        ["SpaceFilesInFolder"] = "Файлы в этой папке ({0})",
        ["SpaceSystemFiles"] = "Системные файлы (подкачка, гибернация)",
        ["SpaceLocked"] = "Закрытые системные файлы",
        ["SpaceScan"] = "Сканировать",
        ["SpaceRescan"] = "Сканировать снова",
        ["SpaceStarting"] = "Запуск…",
        ["SpaceMapFailed"] = "Не удалось построить карту диска: {0}",

        // ---------- cleaning ----------
        ["CleanTitle"] = "Очищаем…",
        ["CleanHint"] = "Занятые файлы пропускаются, принудительно ничего не удаляется.",
        ["CleanStep"] = "Очищаем: {0}…",
        ["CleanGettingStarted"] = "Начинаем…",
        ["CleanAllDone"] = "Готово",
        ["CleanStopped"] = "Очистка прервана: {0}",
        ["ConfirmDeleteTitle"] = "Удалить их навсегда?",
        ["ConfirmDeleteBody"] = "Вы выбрали {0} {1} из «Загрузок» ({2}). Они будут удалены безвозвратно, минуя Корзину. Убедитесь, что они вам больше не нужны.",
        ["ConfirmDeleteYes"] = "Удалить навсегда",
        ["LetCleanupFinishClose"] = "Дождитесь окончания очистки, прежде чем закрывать Roomy.",
        ["LetCleanupFinishQuit"] = "Дождитесь окончания очистки, прежде чем выходить из Roomy.",

        // ---------- done ----------
        ["DoneFreed"] = "Вы освободили",
        ["DoneSubtitle"] = "На диске {0} теперь {1} свободно.",
        ["Before"] = "До",
        ["After"] = "После",
        ["ShareResult"] = "Поделиться результатом",
        ["Done"] = "Готово",
        ["Skipped"] = "{0} занято открытыми программами, поэтому мы его не трогали. Закройте их и проверьте снова.",
        ["ShareCopied"] = @"Скопировано! Вставьте куда угодно. Также сохранено в «Изображения\Roomy».",
        ["ShareFailed"] = "Не удалось создать картинку: {0}",

        // ---------- file kinds ----------
        ["FileOne"] = "файл",
        ["FileFew"] = "файла",
        ["FileMany"] = "файлов",
        ["ItemOne"] = "элемент",
        ["ItemFew"] = "элемента",
        ["ItemMany"] = "элементов",
        ["DetailFiles"] = "{0} {1}",
        ["DetailItems"] = "{0} {1}",

        // ---------- ago ----------
        ["AgoToday"] = "сегодня",
        ["AgoYesterday"] = "вчера",
        ["AgoDays"] = "{0} дн. назад",
        ["AgoWeeks"] = "{0} нед. назад",
        ["AgoMonths"] = "{0} мес. назад",
        ["AgoYear"] = "год назад",
        ["AgoYears"] = "{0} г. назад",
        ["AgoShortToday"] = "сегодня",
        ["AgoShortDays"] = "{0} дн.",
        ["AgoShortWeeks"] = "{0} нед.",
        ["AgoShortMonths"] = "{0} мес.",
        ["AgoShortYear"] = "год",
        ["AgoShortYears"] = "{0} лет",

        // ---------- category copy ----------
        ["CatRecycleTitle"] = "Корзина",
        ["CatRecycleBody"] = "То, что вы уже удалили. Место освободится, когда корзина опустеет.",
        ["CatTempTitle"] = "Временные файлы",
        ["CatTempBody"] = "Остатки, которые программы создают по ходу работы и забывают убрать. Только файлы старше суток.",
        ["CatBrowserTitle"] = "Кэш браузеров",
        ["CatBrowserBody"] = "Сохранённые копии сайтов. Вы остаётесь в аккаунтах, просто сайты в первый раз откроются чуть медленнее.",
        ["CatAppsTitle"] = "Кэш приложений",
        ["CatAppsBody"] = "Кэш Discord, VS Code, Slack и других программ. Они сами создадут его заново, когда понадобится.",
        ["CatShadersTitle"] = "Кэш графических шейдеров",
        ["CatShadersBody"] = "Графика, которую GPU подготовил заранее. Удалять безопасно: игры соберут её заново, но первый запуск может подтормаживать.",
        ["CatCrashTitle"] = "Отчёты о сбоях",
        ["CatCrashBody"] = "Отчёты, которые Windows сохраняет при падении программ. Полезны только разработчикам.",
        ["CatWindowsTitle"] = "Остатки обновлений Windows",
        ["CatWindowsBody"] = "Файлы уже установленных обновлений, которые Windows хранит на всякий случай, и её собственные временные файлы.",
        ["CatInstallersTitle"] = "Старые установщики",
        ["CatInstallersBody"] = "Установочные файлы в «Загрузках», которые вы, скорее всего, уже запускали. Только старше недели.",
        ["CatDownloadsTitle"] = "Большие старые загрузки",
        ["CatDownloadsBody"] = "Крупные файлы в «Загрузках», которых вы не касались больше месяца: старые видео, ISO, архивы.",
        ["CatGamesTitle"] = "Игры, в которые вы не играли",
        ["CatGamesBody"] = "Игры Steam, которые не запускались 2+ месяца. Удаление — через Steam, потом можно установить заново.",
        ["CatWindowsOldTitle"] = "Предыдущая версия Windows",
        ["CatWindowsOldBody"] = "Копия прежней Windows после крупного обновления. Нужна, только если захотите откатиться.",

        // ---------- scanner notes ----------
        ["NoteAppsOpen"] = "Сейчас открыто: {0}. Часть файлов заблокирована — закройте программы, чтобы очистить всё.",
        ["NoteWindowsLocked"] = "Windows не отдаёт эти файлы. Перезапустите Roomy от администратора.",
        ["NoteUpdating"] = "Windows сейчас устанавливает обновления, поэтому кэш обновлений пропущен.",
        ["StepRecycle"] = "Корзина",
        ["StepTemp"] = "Временные файлы",
        ["StepBrowser"] = "Кэш браузеров",
        ["StepApps"] = "Кэш приложений",
        ["StepShaders"] = "Кэш шейдеров",
        ["StepCrash"] = "Отчёты о сбоях",
        ["StepWindows"] = "Обновления Windows",
        ["StepInstallers"] = "Старые установщики",
        ["StepDownloads"] = "Большие загрузки",
        ["StepGames"] = "Непройденные игры",
        ["StepWindowsOld"] = "Старая Windows",
        ["ItemYourTemp"] = "Ваша папка временных файлов",
        ["ItemTeamsClassic"] = "Microsoft Teams (классический)",
        ["ItemVSCode"] = "VS Code",
        ["ItemSteamWeb"] = "Браузер Steam",
        ["ItemSteamGames"] = "Игры Steam",
        ["ItemAppCrashDumps"] = "Дампы аварийных завершений",
        ["ItemWindowsErrorReports"] = "Отчёты об ошибках Windows",
        ["ItemDownloadedUpdates"] = "Загруженные обновления",
        ["ItemDeliveryOptimization"] = "Оптимизация доставки",
        ["ItemWindowsTemp"] = "Временная папка Windows",
        ["ItemSystemDumps"] = "Системные дампы",
        ["DetailDownloaded"] = "Загружено {0}",
        ["DetailUntouched"] = "Не трогали {0}",
        ["DetailLastPlayed"] = "Последний запуск: {0}",
        ["DetailNeverPlayed"] = "Ни разу не запускали",

        // ---------- settings ----------
        ["SettingsTitle"] = "Настройки Roomy",
        ["SettingsHeader"] = "Немного помощи в фоне",
        ["SettingsHeaderHint"] = "Выберите, когда Roomy проверяет диск. Все данные остаются на этом компьютере.",
        ["SetTray"] = "Оставлять Roomy в системном трее",
        ["SetTrayHint"] = "После закрытия окна проверки продолжаются. Выход — «Выйти из Roomy» в трее.",
        ["SetStartup"] = "Запускать при входе в Windows",
        ["SetStartupHint"] = "Тихо стартовать в трее при входе в систему.",
        ["SetStartupButton"] = "Параметры автозапуска Windows",
        ["SetLowSpace"] = "Предупреждать, когда на системном диске мало места",
        ["SetLowSpaceHint"] = "Быстрая проверка свободного места. Не больше одного напоминания в день.",
        ["SetBelow"] = "Меньше",
        ["SetGbEvery"] = "ГБ, проверять каждые",
        ["Set30"] = "30 минут",
        ["Set60"] = "60 минут",
        ["SetWeekly"] = "Раз в неделю искать накопленный кэш",
        ["SetWeeklyHint"] = "Проверяет временные файлы и кэш программ, пока компьютер простаивает и подключён к сети. Напомнит, если можно освободить хотя бы 1 ГБ. Что удалять — решаете вы.",
        ["SetPause"] = "Пауза на день",
        ["SetResume"] = "Возобновить",
        ["SetNotifButton"] = "Параметры уведомлений Windows",
        ["SetSave"] = "Сохранить настройки",
        ["SetVersion"] = "Roomy {0}",
        ["SetVersionName"] = "Версия Roomy",
        ["SetThresholdName"] = "Порог свободного места в ГБ",
        ["SetIntervalName"] = "Интервал проверки",
        ["PauseUntil"] = "Напоминания на паузе до {0}.",
        ["PauseDefault"] = "Напоминания подчиняются настройкам уведомлений Windows.",
        ["StartBlocked"] = "Windows отключила автозапуск Roomy. Включить его можно в разделе «Автозагрузка» параметров Windows.",
        ["StartPolicy"] = "Этой настройкой управляет ваша организация.",
        ["StartWindowsManages"] = "Автозапуском этого приложения управляет Windows. Сначала измените настройку там.",
        ["StartDenied"] = "Windows не разрешила изменить автозапуск.",
        ["ThresholdInvalid"] = "Укажите порог свободного места от 1 до 500 ГБ.",
        ["SaveFailed"] = "Не удалось сохранить настройки: {0}",
        ["SaveFailedCheckStartup"] = " Проверьте автозагрузку в параметрах Windows.",
        ["StartupReadFailed"] = "Не удалось прочитать настройку автозапуска: {0}",
        ["StatusDemo"] = "Демо-настройки временные. Автозапуск и уведомления Windows отключены.",
        ["StatusElevated"] = "Roomy запущена от администратора. Настройки сохранятся и заработают при следующем обычном запуске; фоновые проверки в этом режиме не выполняются.",
        ["CantFindExe"] = "Не удалось найти исполняемый файл Roomy.",
        ["StartupNeedsExe"] = "Чтобы включить автозапуск, запустите Roomy.exe.",
        ["StartupPathTooLong"] = "Перенесите Roomy в папку с более коротким путём, прежде чем включать автозапуск.",

        // ---------- tray & notifications ----------
        ["TrayOpen"] = "Открыть Roomy",
        ["TrayScan"] = "Проверить сейчас",
        ["TrayPause"] = "Пауза напоминаний до завтра",
        ["TrayResume"] = "Возобновить напоминания",
        ["TraySettings"] = "Настройки",
        ["TrayQuit"] = "Выйти из Roomy",
        ["NotifyLowTitle"] = "Диск {0} заполняется",
        ["NotifyLowBody"] = "Осталось {0}. Откройте Roomy, чтобы увидеть, что можно освободить.",
        ["NotifyWeeklyTitle"] = "Roomy нашла {0} того, что можно очистить",
        ["NotifyWeeklyBody"] = "Накопились временные файлы и кэш программ. Посмотрите, когда будет удобно.",
        ["NotifyReview"] = "Посмотреть",
        ["NotifyTomorrow"] = "Завтра",
        ["NotifyTurnOff"] = "Отключить",
        ["NotifyUnavailable"] = "Уведомления Windows недоступны: {0}",
        ["NotifyFailed"] = "Не удалось показать уведомление Windows: {0}",
        ["SaveRemindersFailed"] = "Не удалось сохранить настройки напоминаний: {0}",

        // ---------- app ----------
        ["AppCrashTitle"] = "Roomy споткнулась",
        ["AppAlreadyOpening"] = "Roomy уже открывается. Попробуйте через мгновение.",
        ["DemoAdminToast"] = "Эта кнопка перезапускает Roomy с правами администратора.",
        ["NoAdminToast"] = "Не страшно: Roomy работает и без прав администратора.",
        ["PreferenceLoadError"] = "Не удалось прочитать сохранённые настройки. Фоновые проверки выключены, пока вы не сохраните настройки заново.",

        // ---------- share card ----------
        ["ShareCardFreed"] = "Я освободил(а)",
        ["ShareCardFooter"] = "Бесплатно · Без рекламы · Без аккаунта",
        ["ShareCardOnPC"] = "на своём ПК",
        ["ShareCardOnDrive"] = "на диске {0}",

        // ---------- demo data ----------
        ["DemoDriveLabelGames"] = "Игры",
    };
}
