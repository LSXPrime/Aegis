using Aegis.Enums;
using Aegis.Models;
using Aegis.Utilities;

namespace Aegis.Sample.Console;

public static class Program
{
    public static async Task Main()
    {
        System.Console.Clear();
        // 1. Load Licensing Secrets
        var secretPath = Path.GetTempFileName();
        LicenseUtils.GenerateLicensingSecrets("MySecretTestKey", secretPath, "12345678-90ab-cdef-ghij-klmnopqrst");
        LicenseUtils.LoadLicensingSecrets("MySecretTestKey", secretPath);

        // 2. Choose License Model and Generate License
        System.Console.WriteLine("Select a license model:");
        System.Console.WriteLine("1. Standard");
        System.Console.WriteLine("2. Trial");
        System.Console.WriteLine("3. Node-Locked");
        System.Console.WriteLine("4. Subscription");
        System.Console.WriteLine("5. Floating");
        System.Console.WriteLine("6. Concurrent");

        var choice = System.Console.ReadLine();
        BaseLicense? license;
        var licensePath = Path.GetTempFileName();

        switch (choice)
        {
            case "1":
                LicenseGenerator.GenerateStandardLicense("TestUser")
                    .WithLicenseKey("SD2D-35G9-1502-X3DG-16VI-ELN2")
                    .WithIssuer("Aegis Software")
                    .WithExpiryDate(DateTime.UtcNow.AddDays(30))
                    .WithFeature("PremiumFeature", true)
                    .SaveLicense(licensePath);
                break;
            case "2":
                LicenseGenerator.GenerateTrialLicense(TimeSpan.FromDays(7)).SaveLicense(licensePath);
                break;
            case "3":
                LicenseGenerator.GenerateNodeLockedLicense().SaveLicense(licensePath);
                break;
            case "4":
                LicenseGenerator.GenerateSubscriptionLicense("TestUser", TimeSpan.FromDays(365))
                    .SaveLicense(licensePath);
                break;
            case "5":
                LicenseGenerator.GenerateFloatingLicense("TestUser", 5).SaveLicense(licensePath);
                break;
            case "6":
                LicenseGenerator.GenerateConcurrentLicense("TestUser", 10).SaveLicense(licensePath);
                break;
            default:
                System.Console.WriteLine("Invalid choice.");
                return;
        }

        // 3. Load and Validate License
        try
        {
            license = await LicenseManager.LoadLicenseAsync(licensePath);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to load or validate license: {ex.Message}");
            return;
        }

        // 4. Access License Information and Feature Flags
        if (license == null)
        {
            System.Console.WriteLine("Invalid license.");
            return;
        }

        System.Console.WriteLine($"License Type: {license.Type}");
        System.Console.WriteLine($"License Key: {license.LicenseKey}");
        System.Console.WriteLine($"Issued On: {license.IssuedOn}");
        System.Console.WriteLine($"Expiration Date: {license.ExpirationDate}");

        switch (license.Type)
        {
            case LicenseType.Standard or LicenseType.Trial:
                // Access feature flags
                System.Console.WriteLine(LicenseManager.IsFeatureEnabled("PremiumFeature")
                    ? "Premium Feature is enabled!"
                    : "Premium Feature is not enabled.");
                break;
            case LicenseType.NodeLocked:
            {
                // Access hardware ID
                var nodeLockedLicense = (NodeLockedLicense)license;
                System.Console.WriteLine($"Hardware ID: {nodeLockedLicense.HardwareId}");
                break;
            }
            case LicenseType.Subscription:
            {
                // Access subscription details
                var subscriptionLicense = (SubscriptionLicense)license;
                System.Console.WriteLine($"Subscription Start Date: {subscriptionLicense.SubscriptionStartDate}");
                System.Console.WriteLine($"Subscription Duration: {subscriptionLicense.SubscriptionDuration}");
                break;
            }
            // Access concurrent user details
            case LicenseType.Floating:
            {
                var floatingLicense = (FloatingLicense)license;
                System.Console.WriteLine($"User Name: {floatingLicense.UserName}");
                System.Console.WriteLine($"Max Active Users: {floatingLicense.MaxActiveUsersCount}");
                break;
            }
            case LicenseType.Concurrent:
            {
                var concurrentLicense = (ConcurrentLicense)license;
                System.Console.WriteLine($"User Name: {concurrentLicense.UserName}");
                System.Console.WriteLine($"Max Active Users: {concurrentLicense.MaxActiveUsersCount}");
                break;
            }
        }

        System.Console.ReadKey();
        _ = Main();
    }
}