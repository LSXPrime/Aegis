using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Interfaces;
using Aegis.Models.License;
using Aegis.Models.Utils;

namespace Aegis.Sample.Validation.DateTime.Windows;

public class TrialPeriodValidationRule(IDateTimeProvider dateTimeProvider) : IValidationRule
{
    public LicenseLoadResult<T> Validate<T>(T license, Dictionary<string, string?>? parameters)
        where T : BaseLicense
    {
        if (license is not TrialLicense trialLicense)
            return new LicenseLoadResult<T>(LicenseStatus.Invalid, license);

        var trialEndTime = trialLicense.IssuedOn.Add(trialLicense.TrialPeriod);

        return trialEndTime < dateTimeProvider.UtcNow
            ? new LicenseLoadResult<T>(LicenseStatus.Expired, null, new ExpiredLicenseException("Trial period has expired."))
            : new LicenseLoadResult<T>(LicenseStatus.Valid, license);
    }
}