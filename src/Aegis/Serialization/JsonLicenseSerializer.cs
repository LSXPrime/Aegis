using System.Text.Json;
using Aegis.Exceptions;
using Aegis.Interfaces;
using Aegis.Models;

namespace Aegis.Serialization;

public class JsonLicenseSerializer(JsonSerializerOptions? options = null) : ILicenseSerializer
{
    private readonly JsonSerializerOptions _options = options ?? new JsonSerializerOptions
    {
        WriteIndented = true,
    };
    
    public string Serialize(BaseLicense license)
    {
        return JsonSerializer.Serialize(license, _options);
    }

    public BaseLicense? Deserialize(string data)
    {
        ArgumentNullException.ThrowIfNull(data);
        try
        {
            return JsonSerializer.Deserialize<BaseLicense>(data, _options);
        }
        catch (JsonException ex)
        {
            throw new InvalidLicenseFormatException("Invalid license format.", ex);
        }
    }
}