using Aegis.Models.License;
using Aegis.Models.Utils;

namespace Aegis.Interfaces;

public interface IValidationRule
{
    LicenseLoadResult<T> Validate<T>(T license, Dictionary<string, string?>? parameters) where T : BaseLicense;
}