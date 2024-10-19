using Aegis.Exceptions;
using Aegis.Interfaces;
using Aegis.Models;
using Aegis.Models.Utils;

namespace Aegis.Sample.Validation.DateTime.Windows;

public class TrialPeriodValidationRule(IDateTimeProvider dateTimeProvider) : IValidationRule
{
    public LicenseValidationResult<T> Validate<T>(T license, Dictionary<string, string?>? parameters)
        where T : BaseLicense
    {
        if (license is not TrialLicense trialLicense)
            return new LicenseValidationResult<T>(false, license);

        var trialEndTime = trialLicense.IssuedOn.Add(trialLicense.TrialPeriod);

        return trialEndTime < dateTimeProvider.UtcNow
            ? new LicenseValidationResult<T>(false, null, new ExpiredLicenseException("Trial period has expired."))
            : new LicenseValidationResult<T>(true, license);
    }
}