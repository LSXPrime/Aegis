using System.Globalization;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Models;
using Aegis.Utilities;

[assembly: InternalsVisibleTo("Aegis.Tests")]

namespace Aegis;

public static class LicenseManager
{
    public static BaseLicense? Current { get; internal set; }

    // Validation Endpoint
    private static readonly HttpClient HttpClient = new();
    private static string _serverBaseEndpoint = "https://your-api-url/api/licenses";
    private static readonly string ValidationEndpoint = $"{_serverBaseEndpoint}/validate";
    private static readonly string HeartbeatEndpoint = $"{_serverBaseEndpoint}/heartbeat";
    private static readonly string DisconnectEndpoint = $"{_serverBaseEndpoint}/disconnect";
    private static TimeSpan _heartbeatInterval = TimeSpan.FromMinutes(5); // Should be less than server timeout
    private static Timer? _heartbeatTimer;

    /// <summary>
    /// Sets the base endpoint for the licensing server.
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
    /// Sets the heartbeat interval for concurrent licenses.
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
    /// Saves a license to a file.
    /// </summary>
    /// <typeparam name="T">The type of license to save.</typeparam>
    /// <param name="license">The license object to save.</param>
    /// <param name="filePath">The path to the file to save the license to.</param>
    /// <exception cref="ArgumentNullException">Thrown if the license or file path is null.</exception>
    public static void SaveLicense<T>(T license, string filePath) where T : BaseLicense
    {
        ArgumentNullException.ThrowIfNull(license);
        ArgumentNullException.ThrowIfNull(filePath);

        // Save the combined data to the specified file
        File.WriteAllBytes(filePath, SaveLicense(license));
    }

    /// <summary>
    /// Saves a license to a byte array.
    /// </summary>
    /// <typeparam name="T">The type of license to save.</typeparam>
    /// <param name="license">The license object to save.</param>
    /// <returns>A byte array containing the encrypted and signed license data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the license is null.</exception>
    public static byte[] SaveLicense<T>(T license) where T : BaseLicense
    {
        ArgumentNullException.ThrowIfNull(license);
        // Serialize the license object
        var licenseData =
            JsonSerializer.SerializeToUtf8Bytes(license, new JsonSerializerOptions { WriteIndented = true });

        // Encrypt and sign the license data (including checksum calculation)
        return EncryptAndSignLicenseData(licenseData);
    }

    /// <summary>
    /// Loads a license from a file.
    /// </summary>
    /// <param name="filePath">The path to the file containing the license data.</param>
    /// <param name="validationMode">The validation mode to use (Online or Offline).</param>
    /// <returns>The loaded license object, or null if the license is invalid.</returns>
    /// <exception cref="InvalidLicenseSignatureException">Thrown if the license signature is invalid.</exception>
    /// <exception cref="InvalidLicenseFormatException">Thrown if the license file format is invalid.</exception>
    /// <exception cref="LicenseValidationException">Thrown if the license validation fails.</exception>
    public static async Task<BaseLicense?> LoadLicenseAsync(string filePath,
        ValidationMode validationMode = ValidationMode.Offline)
    {
        // Read the combined data from the file
        var licenseData = await File.ReadAllBytesAsync(filePath);

        // Load the license from the combined data
        Current = await LoadLicenseAsync(licenseData, validationMode);

        return Current;
    }

    /// <summary>
    /// Loads a license from a byte array.
    /// </summary>
    /// <param name="licenseData">The byte array containing the license data.</param>
    /// <param name="validationMode">The validation mode to use (Online or Offline).</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <returns>The loaded license object, or null if the license is invalid.</returns>
    /// <exception cref="InvalidLicenseSignatureException">Thrown if the license signature is invalid.</exception>
    /// <exception cref="InvalidLicenseFormatException">Thrown if the license file format is invalid.</exception>
    /// <exception cref="LicenseValidationException">Thrown if the license validation fails.</exception>
    public static async Task<BaseLicense?> LoadLicenseAsync(byte[] licenseData,
        ValidationMode validationMode = ValidationMode.Offline, Dictionary<string, string?>? validationParams = null)
    {
        // Split the data back into encrypted data and signature
        var (encryptedLicenseData, signature, checksumData) = SplitEncryptedDataAndSignature(licenseData);

        // Verify and decrypt the license data
        if (!SecurityUtils.VerifySignature(encryptedLicenseData, signature,
                LicenseUtils.GetLicensingSecrets().PublicKey))
            throw new InvalidLicenseSignatureException("License signature verification failed.");

        var decryptedData =
            SecurityUtils.DecryptData(encryptedLicenseData, LicenseUtils.GetLicensingSecrets().PrivateKey);

        // Verify the checksum
        if (!SecurityUtils.VerifyChecksum(decryptedData, Encoding.UTF8.GetString(checksumData)))
            throw new InvalidLicenseSignatureException("License checksum verification failed.");


        var license = JsonSerializer.Deserialize<BaseLicense>(decryptedData);
        if (license == null)
            throw new InvalidLicenseFormatException("Invalid license file format.");
        
        // Set the current license based on type
        license = license!.Type switch
        {
            LicenseType.Standard => license as StandardLicense,
            LicenseType.Trial => license as TrialLicense,
            LicenseType.NodeLocked => license as NodeLockedLicense,
            LicenseType.Subscription => license as SubscriptionLicense,
            LicenseType.Concurrent => license as ConcurrentLicense,
            LicenseType.Floating => license as FloatingLicense,
            _ => license
        };

        // Validate the license
        await ValidateLicenseAsync(license!, licenseData, validationMode,
            validationParams ?? GetValidationParams(license!)!);

        return license;
    }

    /// <summary>
    /// Checks if a feature is enabled in the current license.
    /// </summary>
    /// <param name="featureName">The name of the feature to check.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    public static bool IsFeatureEnabled(string featureName)
    {
        return Current != null && Current.Features.ContainsKey(featureName) && Current.Features[featureName];
    }

    /// <summary>
    /// Throws an exception if a feature is not allowed in the current license.
    /// </summary>
    /// <param name="featureName">The name of the feature to check.</param>
    /// <exception cref="FeatureNotLicensedException">Thrown if the feature is not allowed.</exception>
    public static void ThrowIfNotAllowed(string featureName)
    {
        if (!IsFeatureEnabled(featureName))
            throw new FeatureNotLicensedException($"Feature '{featureName}' is not allowed in your licensing model.");
    }

    // Helper methods

    /// <summary>
    /// Encrypts and signs the license data.
    /// </summary>
    /// <param name="licenseData">The license data to encrypt and sign.</param>
    /// <returns>The encrypted and signed license data.</returns>
    private static byte[] EncryptAndSignLicenseData(byte[] licenseData)
    {
        // Calculate the checksum
        var checksum = SecurityUtils.CalculateChecksum(licenseData);

        var encryptedData = SecurityUtils.EncryptData(licenseData, LicenseUtils.GetLicensingSecrets().PrivateKey);
        var signature = SecurityUtils.SignData(encryptedData, LicenseUtils.GetLicensingSecrets().PrivateKey);

        // Combine encrypted data, checksum, and signature into a single byte array
        var encryptedLicenseData =
            new byte[encryptedData.Length + checksum.Length + signature.Length + 8]; // 8 bytes for lengths

        Array.Copy(encryptedData, 0, encryptedLicenseData, 0, encryptedData.Length); // Copy encrypted data

        Array.Copy(Encoding.UTF8.GetBytes(checksum), 0, encryptedLicenseData, encryptedData.Length,
            checksum.Length); // Copy checksum
        Array.Copy(BitConverter.GetBytes(checksum.Length), 0, encryptedLicenseData,
            encryptedData.Length + checksum.Length, 4); // Copy checksum length

        Array.Copy(signature, 0, encryptedLicenseData, encryptedData.Length + checksum.Length + 4,
            signature.Length); // Copy Signature
        Array.Copy(BitConverter.GetBytes(signature.Length), 0, encryptedLicenseData, encryptedLicenseData.Length - 4,
            4); // Copy signature length

        return encryptedLicenseData;
    }

    /// <summary>
    /// Splits the encrypted license data into its components: encrypted data, signature, and checksum.
    /// </summary>
    /// <param name="encryptedLicenseData">The encrypted license data.</param>
    /// <returns>A tuple containing the encrypted data, signature, and checksum.</returns>
    private static (byte[] encryptedData, byte[] signature, byte[] checksum) SplitEncryptedDataAndSignature(
        byte[] encryptedLicenseData)
    {
        // Extract lengths
        var signatureLength = BitConverter.ToInt32(encryptedLicenseData, encryptedLicenseData.Length - 4);
        var checksumLength =
            BitConverter.ToInt32(encryptedLicenseData, encryptedLicenseData.Length - signatureLength - 8);


        // Extract data components
        var encryptedDataLength = encryptedLicenseData.Length - signatureLength - checksumLength - 8;
        var encryptedData = new byte[encryptedDataLength];
        var checksumBytes = new byte[checksumLength];
        var signature = new byte[signatureLength];

        Array.Copy(encryptedLicenseData, 0, encryptedData, 0, encryptedDataLength);
        Array.Copy(encryptedLicenseData, encryptedDataLength, checksumBytes, 0, checksumLength);
        Array.Copy(encryptedLicenseData, encryptedDataLength + checksumLength + 4, signature, 0, signatureLength);

        return (encryptedData, signature, checksumBytes);
    }

    /// <summary>
    /// Validates the license asynchronously.
    /// </summary>
    /// <param name="license">The license object to validate.</param>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="validationMode">The validation mode to use (Online or Offline).</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    /// <exception cref="LicenseValidationException">Thrown if the license validation fails.</exception>
    private static async Task ValidateLicenseAsync(BaseLicense license, byte[] licenseData,
        ValidationMode validationMode, Dictionary<string, string?>? validationParams)
    {
        switch (validationMode)
        {
            case ValidationMode.Online:
                await ValidateLicenseOnlineAsync(license, licenseData);
                break;
            case ValidationMode.Offline:
                ValidateLicenseOffline(license, licenseData, validationParams);
                break;
        }

        if (license.Type == LicenseType.Concurrent)
            _heartbeatTimer ??= new Timer(state => { _ = SendHeartbeat(); }, null, TimeSpan.Zero, _heartbeatInterval);
    }

    /// <summary>
    /// Validates the license online.
    /// </summary>
    /// <param name="license">The license object to validate.</param>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    /// <exception cref="LicenseValidationException">Thrown if the online validation fails.</exception>
    private static async Task ValidateLicenseOnlineAsync(BaseLicense license, byte[] licenseData,
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
        if (!response.IsSuccessStatusCode)
            throw new LicenseValidationException(
                $"Online validation failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
    }

    /// <summary>
    /// Validates the license offline.
    /// </summary>
    /// <param name="license">The license object to validate.</param>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <exception cref="LicenseValidationException">Thrown if the offline validation fails.</exception>
    private static void ValidateLicenseOffline(BaseLicense license, byte[] licenseData,
        Dictionary<string, string?>? validationParams = null)
    {
        var (encryptedLicenseData, signature, _) = SplitEncryptedDataAndSignature(licenseData);

        var isLicenseValid = license.Type switch
        {
            LicenseType.Standard => LicenseValidator.ValidateStandardLicense(encryptedLicenseData, signature,
                validationParams?["UserName"]!, validationParams?["SerialNumber"]!),
            LicenseType.Trial => LicenseValidator.ValidateTrialLicense(encryptedLicenseData, signature),
            LicenseType.NodeLocked => LicenseValidator.ValidateNodeLockedLicense(encryptedLicenseData, signature,
                validationParams?["HardwareId"]),
            LicenseType.Subscription => LicenseValidator.ValidateSubscriptionLicense(encryptedLicenseData, signature),
            LicenseType.Floating => LicenseValidator.ValidateFloatingLicense(encryptedLicenseData, signature,
                validationParams?["UserName"]!, int.Parse(validationParams?["MaxActiveUsersCount"]!)),
            LicenseType.Concurrent => LicenseValidator.ValidateConcurrentLicense(encryptedLicenseData, signature,
                validationParams?["UserName"]!, int.Parse(validationParams?["MaxActiveUsersCount"]!)),
                _ => false
        };

        if (!isLicenseValid)
            throw new LicenseValidationException("License validation failed.");
    }

    /// <summary>
    /// Gets the validation parameters for the license.
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
                { "SerialNumber", validationParams?["SerialNumber"] ?? standardLicense.LicenseKey }
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
    /// Sends a heartbeat to the licensing server.
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
    /// Sends a disconnect message to the licensing server.
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

    /// <summary>
    /// Closes connection to the licensing server and releases any resources.
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
    }
}