using Microsoft.Win32;
using Roomy.Core;
using Windows.ApplicationModel;

namespace Roomy.Services;

public sealed record StartupStatus(bool Enabled, bool Blocked, string? Message = null);

public static class StartupRegistration
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    const string Name = "Roomy";
    public const string TaskId = "RoomyStartup";

    public static bool IsPackaged
    {
        get { try { _ = Package.Current.Id; return true; } catch (InvalidOperationException) { return false; } }
    }

    public static async Task<StartupStatus> ReadAsync()
    {
        if (IsPackaged)
        {
            var task = await StartupTask.GetAsync(TaskId);
            return FromState(task.State);
        }

        using var run = Registry.CurrentUser.OpenSubKey(RunKey);
        using var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey);
        var blocked = approved?.GetValue(Name) is byte[] { Length: > 0 } bytes && (bytes[0] == 3 || bytes[0] == 7);
        return new(run?.GetValue(Name) is string && !blocked, blocked,
            blocked ? Loc.T("StartBlocked") : null);
    }

    public static async Task<StartupStatus> SetEnabledAsync(bool enabled)
    {
        if (IsPackaged)
        {
            var task = await StartupTask.GetAsync(TaskId);
            if (enabled) return FromState(await task.RequestEnableAsync());
            task.Disable();
            return FromState(task.State);
        }

        if (enabled)
        {
            var status = await ReadAsync();
            if (status.Blocked) return status;
            var exe = Environment.ProcessPath ?? throw new InvalidOperationException(Loc.T("CantFindExe"));
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            key.SetValue(Name, StartupCommand(exe));
        }
        else
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(Name, throwOnMissingValue: false);
        }
        return await ReadAsync();
    }

    internal static string StartupCommand(string exe)
    {
        // Portable downloads may be renamed. Only reject a framework host or invalid executable path.
        if (!Path.IsPathFullyQualified(exe) || exe.Contains('"') ||
            !string.Equals(Path.GetExtension(exe), ".exe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileNameWithoutExtension(exe), "dotnet", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(Loc.T("StartupNeedsExe"));
        var command = $"\"{exe}\" --background";
        if (command.Length > 260) throw new InvalidOperationException(Loc.T("StartupPathTooLong"));
        return command;
    }

    static StartupStatus FromState(StartupTaskState state) => new(
        state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy,
        state is StartupTaskState.DisabledByUser or StartupTaskState.DisabledByPolicy or StartupTaskState.EnabledByPolicy,
        state switch
        {
            StartupTaskState.DisabledByUser => Loc.T("StartBlocked"),
            StartupTaskState.DisabledByPolicy or StartupTaskState.EnabledByPolicy => Loc.T("StartPolicy"),
            _ => null,
        });
}
