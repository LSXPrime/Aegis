using System.Globalization;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Interfaces;
using Aegis.Models.License;
using Aegis.Models.Utils;
using Aegis.Serialization;
using Aegis.Utilities;

[assembly: InternalsVisibleTo("Aegis.Tests")]

namespace Aegis;

public static class LicenseManager
{
    // Validation Endpoint
    private static readonly HttpClient HttpClient = new();
    private static string _serverBaseEndpoint = "https://your-api-url/api/licenses";
    private static readonly string ValidationEndpoint = $"{_serverBaseEndpoint}/validate";
    private static readonly string HeartbeatEndpoint = $"{_serverBaseEndpoint}/heartbeat";
    private static readonly string DisconnectEndpoint = $"{_serverBaseEndpoint}/disconnect";
    private static TimeSpan _heartbeatInterval = TimeSpan.FromMinutes(5); // Should be less than server timeout
    private static Timer? _heartbeatTimer;
    private static ILicenseSerializer _serializer = new JsonLicenseSerializer();
    private static bool _builtInValidation = true;
    public static BaseLicense? Current { get; private set; }

    /// <summary>
    ///     Sets the base endpoint for the licensing server.
    /// </summary>
    /// <param name="serverBaseEndpoint">The base endpoint for the licensing server.</param>
    /// <exception cref="ArgumentNullException">Thrown if the server base endpoint is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the server base endpoint is empty.</exception>
    public static void SetServerBaseEndpoint(string serverBaseEndpoint)
    {
        switch (serverBaseEndpoint)
        {
            case null:
                throw new ArgumentNullException(nameof(serverBaseEndpoint));
            case "":
                throw new ArgumentException("Server base endpoint cannot be empty.", nameof(serverBaseEndpoint));
        }

        if (serverBaseEndpoint.EndsWith('/'))
            serverBaseEndpoint = serverBaseEndpoint[..^1];
        _serverBaseEndpoint = serverBaseEndpoint;
    }

    /// <summary>
    ///     Sets the heartbeat interval for concurrent licenses.
    /// </summary>
    /// <param name="heartbeatInterval">The heartbeat interval.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the heartbeat interval is negative.</exception>
    public static void SetHeartbeatInterval(TimeSpan heartbeatInterval)
    {
        if (heartbeatInterval < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(heartbeatInterval), "Heartbeat interval cannot be negative.");

        _heartbeatInterval = heartbeatInterval;
    }

    /// <summary>
    ///     Sets the serializer for license serialization.
    /// </summary>
    /// <param name="serializer">The serializer that implements the <see cref="ILicenseSerializer"/> interface.</param>
    /// <exception cref="ArgumentNullException">Thrown if the serializer is null.</exception>
    public static void SetSerializer(ILicenseSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        _serializer = serializer;
        LicenseValidator.SetSerializer(_serializer);
    }
    
    /// <summary>
    ///     Sets the hardware identifier for license validation and generation.
    /// </summary>
    /// <param name="identifier">The hardware identifier that implements the <see cref="IHardwareIdentifier"/> interface.</param>
    /// <exception cref="ArgumentNullException">Thrown if the identifier is null.</exception>
    public static void SetHardwareIdentifier(IHardwareIdentifier identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        LicenseValidator.SetHardwareIdentifier(identifier);
        LicenseGenerator.SetHardwareIdentifier(identifier);
    }

    /// <summary>
    ///     Sets whether to use built-in validation or not.
    /// </summary>
    /// <param name="value">Whether to use built-in validation or not.</param>
    public static void SetBuiltInValidation(bool value) => _builtInValidation = value;

    /// <summary>
    ///     Saves a license to a file.
    /// </summary>
    /// <typeparam name="T">The type of license to save.</typeparam>
    /// <param name="license">The license object to save.</param>
    /// <param name="filePath">The path to the file to save the license to.</param>
    /// <param name="secretKey">The private key for signing.</param>
    /// <exception cref="ArgumentNullException">Thrown if the license or file path is null.</exception>
    public static void SaveLicenseToPath<T>(T license, string filePath, string? secretKey = null) where T : BaseLicense
    {
        ArgumentNullException.ThrowIfNull(license);
        ArgumentNullException.ThrowIfNull(filePath);
        if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri) == false || string.IsNullOrEmpty(uri.LocalPath) ||
            !uri.IsFile)
            throw new ArgumentException("Invalid file path.", nameof(filePath));

        // Save the combined data to the specified file
        File.WriteAllBytes(filePath, SaveLicense(license, secretKey));
    }

    /// <summary>
    ///     Saves a license to a byte array.
    /// </summary>
    /// <typeparam name="T">The type of license to save.</typeparam>
    /// <param name="license">The license object to save.</param>
    /// <param name="privateKey">The private key for signing.</param>
    /// <returns>A byte array containing the encrypted and signed license data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the license is null.</exception>
    public static byte[] SaveLicense<T>(T license, string? privateKey = null) where T : BaseLicense
    {
        ArgumentNullException.ThrowIfNull(license);

        // Serialize the license object
        var licenseData = Encoding.UTF8.GetBytes(_serializer.Serialize(license));

        // Generate a unique AES secret key
        var aesKey = SecurityUtils.GenerateAesKey();

        // Encrypt the license data using AES
        var encryptedData = SecurityUtils.EncryptData(licenseData, aesKey);

        // Calculate SHA256 hash of the encrypted data
        var hash = SecurityUtils.CalculateSha256Hash(encryptedData);

        // Sign the hash using RSA private key
        var signature = SecurityUtils.SignData(hash, privateKey ?? LicenseUtils.GetLicensingSecrets().PrivateKey);

        // Combine hash, signature, encrypted data, and AES key
        return CombineLicenseData(hash, signature, encryptedData, aesKey);
    }

    /// <summary>
    ///     Loads a license from a file.
    /// </summary>
    /// <param name="filePath">The path to the file containing the license data.</param>
    /// <param name="validationMode">The validation mode to use (Online or Offline).</param>
    /// <returns>The loaded license object, or null if the license is invalid.</returns>
    /// <exception cref="InvalidLicenseSignatureException">Thrown if the license signature is invalid.</exception>
    /// <exception cref="InvalidLicenseFormatException">Thrown if the license file format is invalid.</exception>
    /// <exception cref="LicenseValidationException">Thrown if the license validation fails.</exception>
    public static async Task<LicenseLoadResult<BaseLicense>> LoadLicenseAsync(string filePath,
        ValidationMode validationMode = ValidationMode.Offline)
    {
        // Read the combined data from the file
        var licenseData = await File.ReadAllBytesAsync(filePath);

        // Load the license from the combined data
        return await LoadLicenseAsync(licenseData, validationMode);
    }

    /// <summary>
    ///     Loads a license from a byte array.
    /// </summary>
    /// <param name="licenseData">The byte array containing the license data.</param>
    /// <param name="validationMode">The validation mode to use (Online or Offline).</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <returns>The loaded license object, or null if the license is invalid.</returns>
    /// <exception cref="InvalidLicenseSignatureException">Thrown if the license signature is invalid.</exception>
    /// <exception cref="InvalidLicenseFormatException">Thrown if the license file format is invalid.</exception>
    /// <exception cref="LicenseValidationException">Thrown if the license validation fails.</exception>
    public static async Task<LicenseLoadResult<BaseLicense>> LoadLicenseAsync(byte[] licenseData,
        ValidationMode validationMode = ValidationMode.Offline, Dictionary<string, string?>? validationParams = null)
    {
        var verificationResult = LicenseValidator.VerifyLicenseData(licenseData);
        var license = verificationResult.License;
        if (verificationResult.Status != LicenseStatus.Valid || license == null)
            return new LicenseLoadResult<BaseLicense>(LicenseStatus.Invalid, null, verificationResult.Exception);

        // Set the current license based on type
        license = license.Type switch
        {
            LicenseType.Standard => license as StandardLicense,
            LicenseType.Trial => license as TrialLicense,
            LicenseType.NodeLocked => license as NodeLockedLicense,
            LicenseType.Subscription => license as SubscriptionLicense,
            LicenseType.Concurrent => license as ConcurrentLicense,
            LicenseType.Floating => license as FloatingLicense,
            _ => license
        };

        // Validate the license, VerifyLicenseData will be called again, but it's mandatory to keep LicenseValidator independent.
        var result = await ValidateLicenseAsync(license!, licenseData, validationMode,
            validationParams ?? GetValidationParams(license!)!);

        // Set the current license
        if (result.Status == LicenseStatus.Valid)
        {
            Current = license;
            FeatureManager.SetLicense(license);
        }

        return result;
    }

    /// <summary>
    ///     Closes connection to the licensing server and releases any resources.
    /// </summary>
    public static async Task CloseAsync()
    {
        if (_heartbeatTimer != null)
        {
            await _heartbeatTimer.DisposeAsync();
            _heartbeatTimer = null;
        }

        if (Current is { Type: LicenseType.Concurrent })
            await SendDisconnectAsync();

        HttpClient.Dispose();
        Current = null;
    }

    // Helper methods

    /// <summary>
    ///     Validates the license asynchronously.
    /// </summary>
    /// <param name="license">The license object to validate.</param>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="validationMode">The validation mode to use (Online or Offline).</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    /// <exception cref="LicenseValidationException">Thrown if the license validation fails.</exception>
    private static async Task<LicenseLoadResult<BaseLicense>> ValidateLicenseAsync(BaseLicense license, byte[] licenseData,
        ValidationMode validationMode, Dictionary<string, string?>? validationParams)
    {
        if (license.Type == LicenseType.Concurrent)
            _heartbeatTimer ??= new Timer(state => { _ = SendHeartbeat(); }, null, TimeSpan.Zero, _heartbeatInterval);
        
        switch (validationMode)
        {
            case ValidationMode.Online:
                return await ValidateLicenseOnlineAsync(license, licenseData);
            case ValidationMode.Offline:
                return ValidateLicenseOffline(license, licenseData, validationParams);
            default:
                return new LicenseLoadResult<BaseLicense>(LicenseStatus.Invalid, null, new ArgumentOutOfRangeException(nameof(validationMode), validationMode, null));
        }
    }

    /// <summary>
    ///     Validates the license online.
    /// </summary>
    /// <param name="license">The license object to validate.</param>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    /// <exception cref="LicenseValidationException">Thrown if the online validation fails.</exception>
    private static async Task<LicenseLoadResult<BaseLicense>> ValidateLicenseOnlineAsync(BaseLicense license, byte[] licenseData,
        Dictionary<string, string?>? validationParams = null)
    {
        // Prepare the request content 
        var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(license.LicenseKey), "licenseKey");
        var validationContent = GetValidationParams(license, validationParams);
        formData.Add(new StringContent(JsonSerializer.Serialize(validationContent)), "validationParams");
        formData.Add(new ByteArrayContent(licenseData), "licenseFile", "license.bin");

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("X-API-KEY", LicenseUtils.GetLicensingSecrets().ApiKey);

        var response = await HttpClient.PostAsync(ValidationEndpoint, formData);
        var result = new LicenseLoadResult<BaseLicense>(LicenseStatus.Valid, license);
        if (!response.IsSuccessStatusCode)
            result.Exception = new LicenseValidationException(
                $"Online validation failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");

        return result;
    }

    /// <summary>
    ///     Validates the license offline.
    /// </summary>
    /// <param name="license">The license object to validate.</param>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <exception cref="LicenseValidationException">Thrown if the offline validation fails.</exception>
    private static LicenseLoadResult<BaseLicense> ValidateLicenseOffline(BaseLicense license, byte[] licenseData,
        Dictionary<string, string?>? validationParams = null)
    {
        var licenseStatus = LicenseStatus.Valid;
        if (_builtInValidation)
        {
            licenseStatus = license.Type switch
            {
                LicenseType.Standard => LicenseValidator.ValidateStandardLicense(licenseData,
                    validationParams?["UserName"]!, validationParams?["LicenseKey"]!),
                LicenseType.Trial => LicenseValidator.ValidateTrialLicense(licenseData),
                LicenseType.NodeLocked => LicenseValidator.ValidateNodeLockedLicense(licenseData,
                    validationParams?["HardwareId"]),
                LicenseType.Subscription => LicenseValidator.ValidateSubscriptionLicense(licenseData),
                LicenseType.Floating => LicenseValidator.ValidateFloatingLicense(licenseData,
                    validationParams?["UserName"]!, int.Parse(validationParams?["MaxActiveUsersCount"]!)),
                LicenseType.Concurrent => LicenseValidator.ValidateConcurrentLicense(licenseData,
                    validationParams?["UserName"]!, int.Parse(validationParams?["MaxActiveUsersCount"]!)),
                _ => LicenseStatus.Invalid
            };
        }

        if (licenseStatus == LicenseStatus.Valid || !_builtInValidation)
            licenseStatus = LicenseValidator.ValidateLicenseRules(license, validationParams) ? LicenseStatus.Valid : LicenseStatus.Invalid;
        
        return new LicenseLoadResult<BaseLicense>(licenseStatus, license);
    }

    /// <summary>
    ///     Gets the validation parameters for the license.
    /// </summary>
    /// <param name="license">The license object.</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <returns>A dictionary of validation parameters.</returns>
    private static Dictionary<string, string> GetValidationParams(BaseLicense? license,
        Dictionary<string, string?>? validationParams = null)
    {
        return license switch
        {
            StandardLicense standardLicense => new Dictionary<string, string>
            {
                { "UserName", validationParams?["UserName"] ?? standardLicense.UserName },
                { "LicenseKey", validationParams?["LicenseKey"] ?? standardLicense.LicenseKey }
            },
            NodeLockedLicense nodeLockedLicense => new Dictionary<string, string>
                { { "HardwareId", validationParams?["HardwareId"] ?? nodeLockedLicense.HardwareId } },
            SubscriptionLicense subscriptionLicense => new Dictionary<string, string>
            {
                { "UserName", subscriptionLicense.UserName },
                {
                    "SubscriptionStartDate",
                    subscriptionLicense.SubscriptionStartDate.ToString(CultureInfo.InvariantCulture)
                },
                { "SubscriptionDuration", subscriptionLicense.SubscriptionDuration.ToString() }
            },
            FloatingLicense floatingLicense => new Dictionary<string, string>
            {
                { "UserName", floatingLicense.UserName },
                { "MaxActiveUsersCount", floatingLicense.MaxActiveUsersCount.ToString() }
            },
            ConcurrentLicense concurrentLicense => new Dictionary<string, string>
            {
                { "UserName", validationParams?["UserName"] ?? concurrentLicense.UserName },
                { "MaxActiveUsersCount", concurrentLicense.MaxActiveUsersCount.ToString() }
            },
            TrialLicense trialLicense => new Dictionary<string, string>
            {
                { "TrialPeriod", trialLicense.TrialPeriod.ToString() }
            },
            _ => new Dictionary<string, string>()
        };
    }

    /// <summary>
    ///     Sends a heartbeat to the licensing server.
    /// </summary>
    /// <returns>A task that represents the asynchronous heartbeat operation.</returns>
    /// <exception cref="HeartbeatException">Thrown if the heartbeat fails.</exception>
    private static async Task SendHeartbeat()
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("X-API-KEY", LicenseUtils.GetLicensingSecrets().ApiKey);
        var response = await HttpClient.PostAsync(HeartbeatEndpoint, null);
        if (!response.IsSuccessStatusCode)
            throw new HeartbeatException(
                $"Heartbeat failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
    }

    /// <summary>
    ///     Sends a disconnect message to the licensing server.
    /// </summary>
    /// <returns>A task that represents the asynchronous disconnect operation.</returns>
    /// <exception cref="HeartbeatException">Thrown if the disconnect fails.</exception>
    private static async Task SendDisconnectAsync()
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("X-API-KEY", LicenseUtils.GetLicensingSecrets().ApiKey);
        var response = await HttpClient.PostAsync(DisconnectEndpoint, null);
        if (!response.IsSuccessStatusCode)
            throw new HeartbeatException(
                $"Concurrent user disconnect failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
    }

    private static byte[] CombineLicenseData(byte[] hash, byte[] signature, byte[] encryptedData, byte[] aesKey)
    {
        var combinedData = new byte[
            4 + hash.Length + // Length of hash + hash data
            4 + signature.Length + // Length of signature + signature data
            4 + encryptedData.Length + // Length of encrypted data + data
            4 + aesKey.Length // Length of AES key + key data
        ];

        var offset = 0;

        // Copy hash length and hash data
        var hashLengthBytes = BitConverter.GetBytes(hash.Length);
        Array.Copy(hashLengthBytes, 0, combinedData, offset, 4);
        offset += 4;
        Array.Copy(hash, 0, combinedData, offset, hash.Length);
        offset += hash.Length;

        // Copy signature length and signature data
        var signatureLengthBytes = BitConverter.GetBytes(signature.Length);
        Array.Copy(signatureLengthBytes, 0, combinedData, offset, 4);
        offset += 4;
        Array.Copy(signature, 0, combinedData, offset, signature.Length);
        offset += signature.Length;

        // Copy encrypted data length and encrypted data
        var encryptedDataLengthBytes = BitConverter.GetBytes(encryptedData.Length);
        Array.Copy(encryptedDataLengthBytes, 0, combinedData, offset, 4);
        offset += 4;
        Array.Copy(encryptedData, 0, combinedData, offset, encryptedData.Length);
        offset += encryptedData.Length;

        // Copy AES key length and AES key data
        var aesKeyLengthBytes = BitConverter.GetBytes(aesKey.Length);
        Array.Copy(aesKeyLengthBytes, 0, combinedData, offset, 4);
        offset += 4;
        Array.Copy(aesKey, 0, combinedData, offset, aesKey.Length);

        return combinedData;
    }
}