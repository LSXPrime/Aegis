using Aegis.Models.License;

namespace Aegis.Interfaces;

public interface ILicenseSerializer
{
    string Serialize(BaseLicense license);
    BaseLicense? Deserialize(string data);
}