using Aegis.Exceptions;
using Aegis.Models;
using Aegis.Utilities;

namespace Aegis.Sample.Validation.DateTime.Windows;

internal static class Program
{
    private static async Task Main()
    {
        // 1. Load Licensing Secrets (make sure you've generated them)
        var secretPath = Path.GetTempFileName();
        LicenseUtils.GenerateLicensingSecrets("your-secret-key", secretPath);
        LicenseUtils.LoadLicensingSecrets("your-secret-key", secretPath);

        // 2. Add the TrialPeriodValidationRule
        var dateTimeProvider = new RegistryDateTimeProvider();
        LicenseValidator.AddValidationRule(new TrialPeriodValidationRule(dateTimeProvider));

        // 3. Generate or Load your Trial License
        var licensePath = Path.GetTempFileName();
        LicenseGenerator.GenerateTrialLicense(TimeSpan.FromHours(1))
            .WithIssuer("Aegis Software")
            .WithFeature("TrialFeature", true)
            .SaveLicense(licensePath);
        Console.WriteLine("A new trial license has been generated.");

        // 4. Load the valid Trial License
        var license = await LoadLicense(licensePath);
        if (license == null)
            throw new ExpiredLicenseException("Failed to load Trial License.");
        Console.WriteLine("Loaded Trial License. Using Trial Features...");
        await LicenseManager.CloseAsync();

        // 5. Load the expired Trial License
        dateTimeProvider.Mock = true;
        license = await LoadLicense(licensePath);
        if (license != null)
            throw new LicenseValidationException("The loaded license is valid.");
        Console.WriteLine("Trial period has expired.");
    }

    private static async Task<BaseLicense?> LoadLicense(string licensePath)
    {
        try
        {
            return await LicenseManager.LoadLicenseAsync(licensePath);
        }
        catch
        {
            Console.WriteLine("License Loading failed");
        }

        return null;
    }
}