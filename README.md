## Aegis - Robust and Flexible .NET Licensing Solution

Aegis is a comprehensive and versatile licensing library for .NET applications, empowering developers to implement
various licensing models with ease. It offers strong security features, offline validation, and seamless integration
with your existing applications.

### Features

- **Diverse Licensing Models:**
    - **Standard License:** A basic license model for single-user applications.
    - **Trial License:** A time-limited license for evaluation purposes, with a defined trial period.
    - **Node-Locked License:** Licenses tied to specific hardware, preventing unauthorized usage on different machines.
    - **Subscription License:** Licenses valid for a specific duration, suitable for subscription-based services.
    - **Floating License:** Licenses managed on a server, allowing concurrent usage by a limited number of users within
      an organization.
    - **Concurrent License:** Licenses that allow a maximum number of simultaneous users, ideal for applications with
      shared access.
- **Strong Security:** Utilizes RSA encryption, digital signatures, and checksum verification to protect your licenses
  from tampering and unauthorized modifications.
- **License Validation:** Offers both online and offline license validation modes, ensuring flexibility even in
  scenarios without internet connectivity.
- **Custom Validation Rules:** Define and apply custom validation rules using `IValidationRule` and `IValidationRuleGroup` for tailored license requirements.
- **Easy Integration:** Provides a simple and intuitive API for generating, saving, loading, validating, and managing
  licenses within your .NET applications.
- **Ease of Use:** Provides a fluent API for building and managing licenses, simplifying integration into existing
  projects.
- **Built-in Exceptions:** Includes a set of exceptions for common licensing scenarios, simplifying error handling.
- **Advanced Feature Management:**
    - Centralized management of feature-related operations using the `FeatureManager`.
    - Supports various data types for features (Boolean, Integer, Float, String, DateTime, ByteArray).
    - Methods to check feature enablement & retrieve feature values of different types

### Aegis.Server - Backend for Floating and Concurrent Licenses

Aegis.Server is a lightweight backend service designed to handle floating and concurrent license management. It provides
a foundation for building your own licensing server, offering functionalities like:

- **License Generation and Validation:** Generate, validate, activate, and revoke licenses securely.
- **User Authentication:** Securely register and authenticate users for accessing license-related operations.
- **Heartbeat Monitoring:** Track active concurrent license usage and automatically disconnect idle users.

You can integrate Aegis.Server into your preferred web framework or create a custom implementation based on your
specific needs. Aegis.Server provides the core logic for license management, while allowing you to choose how you want
to expose the functionality.

#### Aegis.Server.AspNetCore

A sample implementation of Aegis.Server using ASP.NET Core is available in the `Aegis.Server.AspNetCore` project. This
implementation showcases how to integrate Aegis.Server into an ASP.NET Core application, providing ready-to-use
controllers and middlewares for:

- **RESTful API Endpoints:** Access license management functionalities through a well-defined API.
- **Authentication and Authorization:** Secure access to API endpoints using JWT and API keys.
- **Rate Limiting:** Control the rate of requests to the server.
- **Swagger Documentation:** Explore the API endpoints using the interactive Swagger UI.

The `Aegis.Server.AspNetCore` project can be used as a starting point for building your own licensing server, or you can
adapt the provided code to your specific needs.

### Getting Started

#### Aegis NuGet Package

1. **Install the Aegis NuGet package:**
   ```
   Install-Package Aegis
   ```

2. **Generate Licensing Secrets:**
   ```C#
   // Replace "your-secret-key" and "C:\Path\To\signature.bin" with your desired secret key and save path.
   var signature = LicenseUtils.GenerateLicensingSecrets("your-secret-key", @"C:\Path\To\signature.bin", "your-server-api-key"); 
   ```
   This will generate a new RSA key pair and an encryption key. The public key, private key, and encryption key are
   encrypted with AES using your provided secret key and saved to the specified file path (`signature.bin` in this
   example).

3. **Load Licensing Secrets:**
   ```C#
   // Load the secret keys from the signature file
   var signature = LicenseUtils.LoadLicensingSecrets("your-secret-key", @"C:\Path\To\signature.bin");
   ```
   Use the same secret key to decrypt and load the licensing secrets from the file you created in step 2.

   Alternatively, you can:

    - Load directly from the configuration section:
   ```C#
   var signature = LicenseUtils.LoadLicensingSecrets(config.GetSection("LicensingSecrets"))
   ```
    - Add the keys to your User Secrets and they will be retrieved automatically.

4. **Generate and Save a License (Examples for each license type):**

   **Standard License:**
   ```C#
   using Aegis;
   using Aegis.Models;

   var license = LicenseGenerator.GenerateStandardLicense("John Doe")
       .WithLicenseKey("SD2D-35G9-1502-X3DG-16VI-ELN2")
       .WithIssuer("Aegis Software")
       .WithFeature("Feature1", Feature.FromBool(true))
       .WithFeature("Feature2", Feature.FromString("Enabled"))
       .WithExpiryDate(DateTime.UtcNow.AddDays(30))
       .SaveLicense(@"C:\Path\To\license.bin"); 
   ```

   **Trial License:**
   ```C#
   var license = LicenseGenerator.GenerateTrialLicense(TimeSpan.FromDays(14))
       .WithIssuer("Aegis Software")
       .WithFeature("AllFeatures", Feature.FromBool(true))
       .SaveLicense(@"C:\Path\To\trial_license.bin");
   ```

   **Node-Locked License:**
   ```C#
   var license = LicenseGenerator.GenerateNodeLockedLicense(HardwareUtils.GetHardwareId()) 
       .WithIssuer("Aegis Software")
       .WithExpiryDate(DateTime.UtcNow.AddYears(1))
       .SaveLicense(@"C:\Path\To\nodelocked_license.bin");
   ```

   **Subscription License:**
   ```C#
   var license = LicenseGenerator.GenerateSubscriptionLicense("Jane Smith", TimeSpan.FromDays(365))
       .WithIssuer("Aegis Software")
       .WithFeature("PremiumFeatures", Feature.FromBool(true))
       .SaveLicense(@"C:\Path\To\subscription_license.bin");
   ```

   **Floating License:**
   ```C#
   var license = LicenseGenerator.GenerateFloatingLicense("Acme Corp", 20) // 20 concurrent users allowed
       .WithIssuer("Aegis Software")
       .SaveLicense(@"C:\Path\To\floating_license.bin");
   ```

   **Concurrent License:**
   ```C#
   var license = LicenseGenerator.GenerateConcurrentLicense("Tech Solutions", 5) // 5 concurrent users allowed
       .WithIssuer("Aegis Software")
       .WithExpiryDate(DateTime.UtcNow.AddYears(1))
       .SaveLicense(@"C:\Path\To\concurrent_license.bin");
   ```

5. **Load and Validate a License:**
   ```C#
   try
   {
        var loadedLicenseResult = await LicenseManager.LoadLicenseAsync(@"C:\Path\To\license.bin");
        if (loadedLicenseResult.Status == Aegis.Enums.LicenseStatus.Valid)
        {
            var loadedLicense = loadedLicenseResult.License;
            // License loaded successfully, you can access its properties:
            Console.WriteLine("License Type: " + loadedLicense!.Type);
            Console.WriteLine("Expiration Date: " + loadedLicense.ExpirationDate);
        }
        else
        {
            // Handle invalid license status
            Console.WriteLine($"License status is: {loadedLicenseResult.Status}");
        }
   }
   catch (LicenseValidationException ex)
   {
       // Handle license validation errors (e.g., expired, invalid signature, etc.)
       Console.WriteLine("License Validation Error: " + ex.Message);
   }
   catch (Exception ex)
   {
       // Handle other errors (e.g., file not found, invalid format, etc.)
       Console.WriteLine("Error Loading License: " + ex.Message);
   } 
   ```

#### Aegis.Server NuGet Package

1. **Install the Aegis.Server NuGet package:**
   ```
   Install-Package Aegis.Server
   ```
2. **Integrate Aegis.Server into an ASP.NET Core application:**
   ```csharp
   // In your Startup.cs file:
   services.AddAegisServer();
   ```
3. **Inherit your DbContext from AegisDbContext class:**
   ```csharp
   using Aegis.Server.Data;

   public class ApplicationDbContext(DbContextOptions<AegisDbContext> options) : AegisDbContext(options)
   {
       // ...
   }
   ```
4. **Inject LicenseService in your Controller:**
   ```csharp
   using Aegis.Server.Services;

   public class LicensesController(LicenseService licenseService)
   {
       // ...
   }
   ```

#### Aegis.Server.AspNetCore Sample Implementation

1. **Clone the Aegis repository and acquire Aegis.Server.AspNetCore from Samples directory:**
   ```
   git clone https://github.com/LSXPrime/Aegis.git
   ```

2. **Configure the database connection:**
    - Open the `appsettings.json` file in the `Aegis.Server.AspNetCore` project.
    - Modify the `DefaultConnection` connection string to point to your SQLite database file. For example:
      ```json
      "ConnectionStrings": {
        "DefaultConnection": "Data Source=C:\\Path\\To\\Your\\Database\\Aegis.db"
      }
      ```

3. **Set the JWT settings:**
    - In `appsettings.json`, configure the `JwtSettings` section with your secret key, salt, and token expiration
      settings:
      ```json
      "JwtSettings": {
        "Secret": "your_jwt_secret_key",
        "Salt": "your_password_salt",
        "AccessTokenExpirationInDays": 1,
        "RefreshTokenExpirationInDays": 7
      }
      ```
    - **Security Considerations:** Use strong and unique values for your JWT secret key and password salt. Keep them
      confidential and do not expose them in client-side code.

4. **Build and run the server:**
   ```
   dotnet build
   dotnet run --project Aegis.Server.AspNetCore
   ```

5. **Deployment:**
    - You can deploy Aegis.Server to various environments, such as:
        - **Azure App Service:** Create an App Service in Azure and deploy your application.
        - **Docker Container:** Create a Docker image of your application and run it in a containerized environment.
        - **Self-Hosted:** Deploy your application to a server that you manage.

#### Concurrent and Floating License Activation

1. **Activate a license from your client application:**
   ```C#
   LicenseManager.SetServerBaseEndpoint("https://your-aegis-server-url");
   await LicenseManager.LoadLicenseAsync("path\to\license.bin", ValidationMode.Online); 
   ```
   Replace `"https://your-aegis-server-url"` with the actual URL of your Aegis.Server.AspNetCore deployment.
   The `LoadLicenseAsync` function will now perform online validation against the server.

2. **Heartbeat Handling:**
   For concurrent licenses, Aegis.Server automatically handles heartbeat requests sent from client applications. The
   client library should periodically send heartbeat requests to the server to maintain an active connection. You can
   configure the heartbeat interval using the `LicenseManager.SetHeartbeatInterval()` method.

3. **Idle User Disconnection:**
   Aegis.Server monitors heartbeats and will automatically disconnect users if a heartbeat is not received within a
   specified timeout period.

### Feature Usage

Aegis includes `FeatureManager` to control access to specific features within your application based on the loaded license. Here's how to use it:

**Defining Features in Licenses**

When generating licenses using `LicenseBuilder`, you can define features and their associated values:

```csharp
using Aegis;
using Aegis.Models;

// ...

var license = LicenseGenerator.GenerateStandardLicense("John Doe")
    .WithIssuer("Aegis Software")
    .WithFeature("BasicReporting", Feature.FromBool(true)) // Boolean feature
    .WithFeature("NumberOfUsers", Feature.FromInt(5)) // Integer feature
    .WithFeature("SupportLevel", Feature.FromString("Premium")) // String feature
    .WithFeature("DataLimit", Feature.FromFloat(10.5f)) // Float feature
    .WithFeature("Expiry", Feature.FromDateTime(DateTime.UtcNow.AddDays(30))) // DateTime feature
    .WithFeature("CustomData", Feature.FromByteArray(new byte[] { 0x01, 0x02, 0x03 })) // Byte array feature
    .SaveLicense(@"C:\Path\To\license.bin");
```

**Checking if a Feature is Enabled**

Use the `FeatureManager.IsFeatureEnabled` method to check if a feature is enabled in the currently loaded license:

```csharp
if (FeatureManager.IsFeatureEnabled("BasicReporting"))
{
    // Allow access to basic reporting functionality
    Console.WriteLine("Basic reporting is enabled.");
}
else
{
    // Feature is not enabled
    Console.WriteLine("Basic reporting is not enabled.");
}
```

**Retrieving Feature Values**

The `FeatureManager` provides methods to retrieve feature values of different types:

```csharp
// Get an integer feature value:
int numberOfUsers = FeatureManager.GetFeatureInt("NumberOfUsers");
Console.WriteLine($"Number of users allowed: {numberOfUsers}");

// Get a string feature value:
string supportLevel = FeatureManager.GetFeatureString("SupportLevel");
Console.WriteLine($"Support level: {supportLevel}");

// Get a float feature value:
float dataLimit = FeatureManager.GetFeatureFloat("DataLimit");
Console.WriteLine($"Data Limit: {dataLimit}");

// Get a DateTime feature value:
DateTime expiry = FeatureManager.GetFeatureDateTime("Expiry");
Console.WriteLine($"Feature Expiry: {expiry}");

// Get a byte array feature value:
byte[] customData = FeatureManager.GetFeatureByteArray("CustomData");
Console.WriteLine($"Custom Data Length: {customData.Length}");
```

**Enforcing Feature Restrictions**

Use `FeatureManager.ThrowIfNotAllowed` to throw a `FeatureNotLicensedException` if a feature is not enabled:

```csharp
try
{
    FeatureManager.ThrowIfNotAllowed("AdvancedAnalytics");
    // Code that requires the "AdvancedAnalytics" feature
    Console.WriteLine("Advanced analytics is available.");
}
catch (FeatureNotLicensedException ex)
{
    // Handle the case where the feature is not licensed
    Console.WriteLine($"Error: {ex.Message}"); // Output: Error: The feature 'AdvancedAnalytics' is not included in your license.
}
```

**Important:**

-   The `FeatureManager` relies on the currently loaded license. Make sure to load a license using `LicenseManager.LoadLicenseAsync` before using `FeatureManager`.
-   Feature names are case-sensitive.
-   If a feature is not defined in the license, `IsFeatureEnabled` will return `false`, and the type-specific retrieval methods will return default values (0 for numeric types, null for strings, default `DateTime`, and empty `byte[]`).

### Custom Validation Rules

Aegis allows you to implement custom validation rules to enforce specific licensing requirements beyond the built-in validation logic.
This provides greater flexibility and control over your licensing system.

1. **Implementing a Custom Rule**

   To create a custom validation rule, implement the `IValidationRule` interface:

    ```csharp
    using Aegis.Interfaces;
    using Aegis.Models.License;
    using Aegis.Models.Utils;
    
    public class MyCustomRule : IValidationRule
    {
        public LicenseLoadResult<T> Validate<T>(T license, Dictionary<string, string?>? parameters) where T : BaseLicense
        {
            // Your custom validation logic here.
            // Access license properties, parameters, external resources, etc.
    
            if (/* Validation succeeds */)
            {
                return new LicenseLoadResult<T>(Aegis.Enums.LicenseStatus.Valid, license);
            }
            else
            {
                // Provide an appropriate exception for validation failures.
                return new LicenseLoadResult<T>(Aegis.Enums.LicenseStatus.Invalid, null, new LicenseValidationException("Custom validation failed.")); 
            }
        }
    }
    ```

2. **Registering the Rule**

   Once you have implemented your custom rule, register it with the `LicenseValidator`:

    ```csharp
    LicenseValidator.AddValidationRule(new MyCustomRule());
    ```

3. **Example: Advanced Hardware Validation for Node-Locked Licenses**

   This example demonstrates a more robust hardware validation for node-locked licenses, considering multiple hardware
   factors.

    ```csharp
    using Aegis.Interfaces;
    using Aegis.Models.License;
    using Aegis.Models.Utils;
    using Aegis.Utilities;
    
    public class AdvancedHardwareRule : IValidationRule
    {
        public LicenseLoadResult<T> Validate<T>(T license, Dictionary<string, string?>? parameters) where T : BaseLicense
        {
            if (license is not NodeLockedLicense nodeLockedLicense)
                return new LicenseLoadResult<T>(Aegis.Enums.LicenseStatus.Valid, license);
    
            // Implement your logic to combine and validate multiple hardware factors
            // You can use HardwareUtils.GetHardwareId() as a starting point and add
            // more specific checks for CPU ID, motherboard serial number, etc.
            string combinedHardwareId = GenerateCombinedHardwareId();
    
            if (combinedHardwareId != nodeLockedLicense.HardwareId)
            {
                return new LicenseLoadResult<T>(Aegis.Enums.LicenseStatus.Invalid, null, new HardwareMismatchException("Hardware mismatch detected."));
            }
    
            return new LicenseLoadResult<T>(Aegis.Enums.LicenseStatus.Valid, license);
        }
    
        // Replace with your actual implementation to generate a combined hardware ID.
        private string GenerateCombinedHardwareId()
        {
            // ... your logic to combine multiple hardware factors ...
            return ""; // Replace with the generated combined ID
        }
    }
    ```

   **Usage:**

    ```csharp
    LicenseValidator.AddValidationRule(new AdvancedHardwareRule());
    
    // ... during license loading ...
    var license = await LicenseManager.LoadLicenseAsync("license.bin"); 
    ```

   This example showcases how you can create a custom rule to enhance the security of your node-locked licenses by
   validating against multiple hardware identifiers.

   You can adapt this example and implement your own logic for combining and validating different hardware factors to
   suit your specific needs. Remember to provide clear documentation and error messages within your custom rules to make
   them easy to understand and maintain.

### Custom Hardware Identification

By default, Aegis uses a `DefaultHardwareIdentifier` that combines Machine Name, User Name, OS Version, and MAC Address.
To implement a custom hardware identifier:

1. **Create a class that implements `IHardwareIdentifier`:**

   ```csharp
   using Aegis.Interfaces;

   public class MyCustomHardwareIdentifier : IHardwareIdentifier
   {
       public string GetHardwareIdentifier()
       {
           // Your custom logic to retrieve the hardware identifier.
           // Example: CPU ID, Motherboard serial number, etc.
           return "..."; 
       }

       public bool ValidateHardwareIdentifier(string hardwareIdentifier)
       {
           // Your custom logic to validate the hardware identifier.
           return GetHardwareIdentifier() == hardwareIdentifier;
       }
   }
   ```

2. **Set the custom identifier:**

   ```csharp
   LicenseManager.SetHardwareIdentifier(new MyCustomHardwareIdentifier());
   ```

### Custom Serialization

Aegis uses a `JsonLicenseSerializer` by default. To implement a custom serializer:

1. **Create a class that implements `ILicenseSerializer`:**

   ```csharp
   using Aegis.Interfaces;
   using Aegis.Models.License;

   public class MyCustomSerializer : ILicenseSerializer
   {
       public string Serialize(BaseLicense license)
       {
           // Your custom serialization logic.
           // Example: XML serialization, binary serialization, etc.
           return "...";
       }

       public BaseLicense? Deserialize(string data)
       {
           // Your custom deserialization logic.
           return ...;
       }
   }
   ```

2. **Set the custom serializer:**

   ```csharp
   LicenseManager.SetSerializer(new MyCustomSerializer());
   ```

### Disabling Built-in Validation

You can disable Aegis's built-in validation logic if you want to rely solely on your custom validation rules or have a
different validation process.

```csharp
LicenseManager.SetBuiltInValidation(false); 
```

This will prevent `LicenseManager` from performing its default validation checks for license type, expiry date, and
other built-in criteria. You will be responsible for implementing all necessary validation logic in your custom rules or
through other means.

### Architecture

**Aegis Licensing System Architecture:**

The Aegis licensing system comprises two main components: the Aegis client library (integrated into your .NET
application) and the Aegis.Server backend service.

**Client-Side:**

- **License Generation:** Licenses are generated using the Aegis library, typically during the development or deployment
  process.
- **License Storage:** The generated license file is stored securely on the client machine (consider best practices for
  protecting this file).
- **License Loading:** The client application loads the license file using `LicenseManager.LoadLicenseAsync()`.
- **Validation:**
    - **Offline Validation:** The license is validated locally using cryptographic signatures, checksums, and any registered custom validation rules.
    - **Online Validation:** For floating and concurrent licenses, the client library connects to the
      Aegis.Server.AspNetCore to validate the license and manage activations.
- **Feature Access:** The application checks for enabled features using `FeatureManager.IsFeatureEnabled()`.
- **Heartbeat (Concurrent Licenses):** For concurrent licenses, the client library periodically sends heartbeat requests
  to the Aegis.Server.AspNetCore to maintain an active connection.

**Server-Side:**

- **License Management:** The Aegis.Server backend service manages licenses and activations.
- **Heartbeat Monitor:** A background service monitors heartbeat requests from clients and automatically disconnects
  inactive concurrent users.

- **ASP.NET Core Sample Implementation:** A sample ASP.NET Core application is available in
  the `Aegis.Server.AspNetCore` project and it offers.
    - **API Endpoints:** Aegis.Server.AspNetCore exposes RESTful API endpoints for license validation, activation,
      revocation, user authentication, and heartbeat monitoring.
    - **Database:** A SQL database is used to store license information, user data, activations, and other relevant
      data.
    - **Authentication:** Aegis.Server.AspNetCore uses JWT (JSON Web Tokens) for user authentication and authorization.
    - **ApplicationDbContext**: Aegis.Server.AspNetCore uses the `ApplicationDbContext` that inherits
      from `AegisDbContext` to store license information, user data, activations, and other relevant data.

**Interaction:**

- Client applications communicate with the Aegis.Server.AspNetCore using its API endpoints.
- For floating and concurrent licenses, the server tracks the number of active users and enforces license limits.
- The heartbeat mechanism ensures that the server has an up-to-date view of active concurrent users.

### Troubleshooting

- **License Validation Errors:**
    - Ensure the license file is not corrupted or tampered with.
    - Check the system clock on the client machine to ensure it is accurate.
    - Verify the license key and any validation parameters (e.g., hardware ID, username) are correct.
    - If using online validation, ensure the server is reachable and the API key is configured correctly.

- **Server Errors:**
    - Check the server logs for any error messages.
    - Ensure the database connection string and JWT settings are configured correctly in the `appsettings.json` file.

- **Concurrent License Issues:**
    - If users are being disconnected unexpectedly, check the heartbeat interval and timeout settings on both the client
      and server.
    - Ensure that the server can handle the expected number of concurrent connections.

### Contributing

Contributions to Aegis are welcome!

**Guidelines:**

1. **Fork the repository** on GitHub.
2. **Create a new branch** for your feature or bug fix.
3. **Make your changes** and ensure they follow the project's coding style.
4. **Write tests** to cover your changes.
5. **Submit a pull request** to the main repository.

### License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.