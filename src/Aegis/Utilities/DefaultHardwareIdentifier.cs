using System.Net.NetworkInformation;
using Aegis.Interfaces;

namespace Aegis.Utilities;

public class DefaultHardwareIdentifier : IHardwareIdentifier
{
    /// <summary>
    ///     Gets a unique hardware identifier for the current machine.
    /// </summary>
    /// <returns>A string representing the hardware identifier.</returns>
    public string GetHardwareIdentifier()
    {
        return $"{Environment.MachineName}-{Environment.UserName}-{Environment.OSVersion}-{GetMacAddress()}";
    }

    /// <summary>
    ///     Validates a hardware identifier against the current machine's hardware identifier.
    /// </summary>
    /// <param name="hardwareId">The hardware identifier to validate.</param>
    /// <returns>True if the hardware identifier matches, false otherwise.</returns>
    public bool ValidateHardwareIdentifier(string hardwareId)
    {
        return GetHardwareIdentifier() == hardwareId;
    }

    private static string GetMacAddress()
    {
        var values = NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => (x.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) && x.Name != "docker0")
            .Select(x => x.GetPhysicalAddress().ToString())
            .Where(x => x != "000000000000")
            .Select(x =>
            {
                if (x.Length != 12 && x.Length != 16)
                    return x;

                var parts = Enumerable.Range(0, x.Length / 2).Select(i => x.Substring(i * 2, 2));
                return string.Join(":", parts.ToArray());
            })
            .ToArray();

        return values.Length > 0
            ? string.Join(",", values.ToArray())
            : "000000000000";
    }
}