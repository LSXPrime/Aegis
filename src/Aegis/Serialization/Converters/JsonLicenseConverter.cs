using System.Text.Json;
using System.Text.Json.Serialization;
using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Models.License;

namespace Aegis.Serialization.Converters;

public class JsonLicenseConverter : JsonConverter<BaseLicense>
{
    public override BaseLicense Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Read the license type discriminator
        Utf8JsonReader readerClone = reader;
        if (!readerClone.Read() || readerClone.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object.");
        
        if(!readerClone.Read() || readerClone.TokenType != JsonTokenType.PropertyName || readerClone.GetString() != "Type")
            throw new JsonException("Expected Type property.");

        readerClone.Read();

        var licenseType = (LicenseType)Enum.Parse(typeof(LicenseType), readerClone.GetString()!);

        // Deserialize based on the discriminator
        return licenseType switch
        {
            LicenseType.Standard => JsonSerializer.Deserialize<StandardLicense>(ref reader, options)!,
            LicenseType.Trial => JsonSerializer.Deserialize<TrialLicense>(ref reader, options)!,
            LicenseType.NodeLocked => JsonSerializer.Deserialize<NodeLockedLicense>(ref reader, options)!,
            LicenseType.Subscription => JsonSerializer.Deserialize<SubscriptionLicense>(ref reader, options)!,
            LicenseType.Floating => JsonSerializer.Deserialize<FloatingLicense>(ref reader, options)!,
            LicenseType.Concurrent => JsonSerializer.Deserialize<ConcurrentLicense>(ref reader, options)!,
            _ => throw new InvalidLicenseFormatException($"Unknown license type: {licenseType}")
        };
    }


    public override void Write(Utf8JsonWriter writer, BaseLicense value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}