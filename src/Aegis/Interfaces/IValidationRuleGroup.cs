using Aegis.Models.License;
using Aegis.Models.Utils;

namespace Aegis.Interfaces;

public interface IValidationRuleGroup
{
    IEnumerable<IValidationRule> Rules { get; }
    
    LicenseLoadResult<T> Validate<T>(T license, Dictionary<string, string?>? validationParams = null) where T : BaseLicense;
}