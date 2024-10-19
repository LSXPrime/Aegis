using Microsoft.Win32;

namespace Aegis.Sample.Validation.DateTime.Windows;


public interface IDateTimeProvider
{
    System.DateTime UtcNow { get; }
}
public class RegistryDateTimeProvider : IDateTimeProvider
{
    private const string RegistryKeyPath = @"SOFTWARE\LSXPrime\Aegis";
    private const string LastStartTimeValueName = "LastStartTime";
    
    public bool Mock { get; set; } // Mock the current system time for testing purposes

    public System.DateTime UtcNow
    {
        get
        {
            var lastStartTime = GetLastStartTimeFromRegistry();
            var currentUtcNow = System.DateTime.UtcNow;

            if (lastStartTime > currentUtcNow)
            {
                return lastStartTime;
            }

            SetLastStartTimeInRegistry(currentUtcNow);
            return currentUtcNow;
        }
    }

    private System.DateTime GetLastStartTimeFromRegistry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        var lastStartTimeTicks = key?.GetValue(LastStartTimeValueName) as long? ?? 0;
        return new System.DateTime(lastStartTimeTicks, DateTimeKind.Utc).AddHours(Mock ? 2 : 0);
    }

    private void SetLastStartTimeInRegistry(System.DateTime dateTime)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        key?.SetValue(LastStartTimeValueName, dateTime.Ticks, RegistryValueKind.QWord);
    }
}