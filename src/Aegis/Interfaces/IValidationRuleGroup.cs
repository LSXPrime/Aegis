using Aegis.Models;
using Aegis.Models.Utils;

namespace Aegis.Interfaces;

public interface IValidationRuleGroup
{
    IEnumerable<IValidationRule> Rules { get; }
    
    LicenseValidationResult<T> Validate<T>(T license, Dictionary<string, string?>? validationParams = null) where T : BaseLicense;
}