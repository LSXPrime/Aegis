using Aegis.Models;

namespace Aegis.Interfaces;

public interface ILicenseSerializer
{
    string Serialize(BaseLicense license);
    BaseLicense? Deserialize(string data);
}