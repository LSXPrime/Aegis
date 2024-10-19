using Aegis.Models;
using Aegis.Models.Utils;

namespace Aegis.Interfaces;

public interface IValidationRule
{
    LicenseValidationResult<T> Validate<T>(T license, Dictionary<string, string?>? parameters) where T : BaseLicense;
}