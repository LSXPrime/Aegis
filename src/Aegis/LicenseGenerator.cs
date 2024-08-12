using Aegis.Models;
using Aegis.Utilities;

namespace Aegis;

public static class LicenseGenerator
{
    /// <summary>
    /// Generates a standard license.
    /// </summary>
    /// <param name="userName">The username for the license.</param>
    /// <returns>A new StandardLicense object.</returns>
    public static StandardLicense GenerateStandardLicense(string userName)
    {
        return new StandardLicense(userName);
    }

    /// <summary>
    /// Generates a trial license.
    /// </summary>
    /// <param name="trialPeriod">The trial period for the license.</param>
    /// <returns>A new TrialLicense object.</returns>
    public static TrialLicense GenerateTrialLicense(TimeSpan trialPeriod)
    {
        return new TrialLicense(trialPeriod);
    }

    /// <summary>
    /// Generates a node-locked license.
    /// </summary>
    /// <param name="hardwareId">The hardware ID to lock the license to. If null, the current machine's hardware ID will be used.</param>
    /// <returns>A new NodeLockedLicense object.</returns>
    public static NodeLockedLicense GenerateNodeLockedLicense(string? hardwareId = null)
    {
        hardwareId ??= HardwareUtils.GetHardwareId();
        return new NodeLockedLicense(hardwareId);
    }
    
    /// <summary>
    /// Generates a subscription license.
    /// </summary>
    /// <param name="userName">The username for the license.</param>
    /// <param name="subscriptionDuration">The duration of the subscription.</param>
    /// <returns>A new SubscriptionLicense object.</returns>
    public static SubscriptionLicense GenerateSubscriptionLicense(string userName, TimeSpan subscriptionDuration)
    {
        return new SubscriptionLicense(userName, subscriptionDuration);
    }
    
    /// <summary>
    /// Generates a floating license.
    /// </summary>
    /// <param name="userName">The username for the license.</param>
    /// <param name="maxActiveUsersCount">The maximum number of concurrent users allowed.</param>
    /// <returns>A new FloatingLicense object.</returns>
    public static FloatingLicense GenerateFloatingLicense(string userName, int maxActiveUsersCount)
    {
        return new FloatingLicense(userName, maxActiveUsersCount);
    }
    
    /// <summary>
    /// Generates a concurrent license.
    /// </summary>
    /// <param name="userName">The username for the license.</param>
    /// <param name="maxActiveUsersCount">The maximum number of concurrent users allowed.</param>
    /// <returns>A new ConcurrentLicense object.</returns>
    public static ConcurrentLicense GenerateConcurrentLicense(string userName, int maxActiveUsersCount)
    {
        return new ConcurrentLicense(userName, maxActiveUsersCount);
    }
}