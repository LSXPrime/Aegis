using DeviceId;

namespace Aegis.Utilities;

public static class HardwareUtils
{
    /// <summary>
    /// Gets a unique hardware identifier for the current machine.
    /// </summary>
    /// <returns>A string representing the hardware identifier.</returns>
    public static string GetHardwareId()
    {
        return new DeviceIdBuilder()
            .AddMachineName()
            .AddUserName()
            .AddOsVersion()
            .AddMacAddress(true, true)
            .ToString();
    }
    
    /// <summary>
    /// Validates a hardware identifier against the current machine's hardware identifier.
    /// </summary>
    /// <param name="hardwareId">The hardware identifier to validate.</param>
    /// <returns>True if the hardware identifier matches, false otherwise.</returns>
    public static bool ValidateHardwareId(string hardwareId)
    {
        return GetHardwareId() == hardwareId;
    }
}