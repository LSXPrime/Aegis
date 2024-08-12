## Aegis - Robust and Flexible .NET Licensing Solution

Aegis is a comprehensive and versatile licensing library for .NET applications, empowering developers to implement various licensing models with ease. It offers strong security features, offline validation, and seamless integration with your existing applications.

### Features

- **Diverse Licensing Models:**
    - **Standard License:** A basic license model for single-user applications.
    - **Trial License:** A time-limited license for evaluation purposes, with a defined trial period.
    - **Node-Locked License:** Licenses tied to specific hardware, preventing unauthorized usage on different machines.
    - **Subscription License:** Licenses valid for a specific duration, suitable for subscription-based services.
    - **Floating License:** Licenses managed on a server, allowing concurrent usage by a limited number of users within an organization.
    - **Concurrent License:** Licenses that allow a maximum number of simultaneous users, ideal for applications with shared access.
- **Strong Security:** Utilizes RSA encryption, digital signatures, and checksum verification to protect your licenses from tampering and unauthorized modifications.
- **License Validation:** Offers both online and offline license validation modes, ensuring flexibility even in scenarios without internet connectivity.
- **Easy Integration:** Provides a simple and intuitive API for generating, saving, loading, validating, and managing licenses within your .NET applications.
- **Ease of Use:** Provides a fluent API for building and managing licenses, simplifying integration into existing projects.
- **Built-in Exceptions:** Includes a set of exceptions for common licensing scenarios, simplifying error handling.

### Aegis.Server - Backend for Floating and Concurrent Licenses

Aegis.Server is a lightweight ASP.NET Core backend service designed to handle floating and concurrent license management. It provides RESTful API endpoints for:

- **License Generation and Validation:** Generate, validate, activate, and revoke licenses securely.
- **User Authentication:** Securely register and authenticate users for accessing license-related operations.
- **Heartbeat Monitoring:** Track active concurrent license usage and automatically disconnect idle users.

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
   This will generate a new RSA key pair and an encryption key. The public key, private key, and encryption key are encrypted with AES using your provided secret key and saved to the specified file path (`signature.bin` in this example).

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

   var license = LicenseGenerator.GenerateStandardLicense("John Doe")
       .WithLicenseKey("SD2D-35G9-1502-X3DG-16VI-ELN2")
       .WithIssuer("Aegis Software")
       .WithFeature("Feature1", true)
       .WithFeature("Feature2", false)
       .WithExpiryDate(DateTime.UtcNow.AddDays(30))
       .SaveLicense(@"C:\Path\To\license.bin"); 
   ```

   **Trial License:**
   ```C#
   var license = LicenseGenerator.GenerateTrialLicense(TimeSpan.FromDays(14))
       .WithIssuer("Aegis Software")
       .WithFeature("AllFeatures", true) 
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
       .WithFeature("PremiumFeatures", true) 
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
       var loadedLicense = await LicenseManager.LoadLicenseAsync(@"C:\Path\To\license.bin"); 

       // License loaded successfully, you can access its properties:
       Console.WriteLine("License Type: " + loadedLicense.Type);
       Console.WriteLine("Expiration Date: " + loadedLicense.ExpirationDate);
       // ...
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

6. **Check Feature Status:**
   ```C#
   if (LicenseManager.IsFeatureEnabled("Feature1"))
   {
       // Allow access to Feature1
   }
   else
   {
       // Feature1 is not enabled, handle accordingly (e.g., disable UI elements, show a message)
   }

   // Or throw an exception if the feature is not allowed:
   LicenseManager.ThrowIfNotAllowed("Feature1"); 
   ```


#### Aegis.Server Repository

1. **Clone the Aegis repository:**
   ```
   git clone https://github.com/LSXPrime/Aegis.git
   ```

2. **Configure the database connection:**
    - Open the `appsettings.json` file in the `Aegis.Server` project.
    - Modify the `DefaultConnection` connection string to point to your SQLite database file. For example:
      ```json
      "ConnectionStrings": {
        "DefaultConnection": "Data Source=C:\\Path\\To\\Your\\Database\\Aegis.db"
      }
      ```

3. **Set the JWT settings:**
    - In `appsettings.json`, configure the `JwtSettings` section with your secret key, salt, and token expiration settings:
      ```json
      "JwtSettings": {
        "Secret": "your_jwt_secret_key",
        "Salt": "your_password_salt",
        "AccessTokenExpirationInDays": 1,
        "RefreshTokenExpirationInDays": 7
      }
      ```
    - **Security Considerations:** Use strong and unique values for your JWT secret key and password salt. Keep them confidential and do not expose them in client-side code.

4. **Build and run the server:**
   ```
   dotnet build
   dotnet run --project Aegis.Server
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
   Replace `"https://your-aegis-server-url"` with the actual URL of your Aegis.Server deployment. The `LoadLicenseAsync` function will now perform online validation against the server.

2. **Heartbeat Handling:**
   For concurrent licenses, Aegis.Server automatically handles heartbeat requests sent from client applications. The client library should periodically send heartbeat requests to the server to maintain an active connection. You can configure the heartbeat interval using the `LicenseManager.SetHeartbeatInterval()` method.

3. **Idle User Disconnection:**
   Aegis.Server monitors heartbeats and will automatically disconnect users if a heartbeat is not received within a specified timeout period (configurable in the `HeartbeatMonitor` service in Aegis.Server).

### Architecture

**Aegis Licensing System Architecture:**

The Aegis licensing system comprises two main components: the Aegis client library (integrated into your .NET application) and the Aegis.Server backend service.

**Client-Side:**

- **License Generation:** Licenses are generated using the Aegis library, typically during the development or deployment process.
- **License Storage:** The generated license file is stored securely on the client machine (consider best practices for protecting this file).
- **License Loading:** The client application loads the license file using `LicenseManager.LoadLicenseAsync()`.
- **Validation:**
    - **Offline Validation:** The license is validated locally using cryptographic signatures and checksums.
    - **Online Validation:** For floating and concurrent licenses, the client library connects to the Aegis.Server to validate the license and manage activations.
- **Feature Access:** The application checks for enabled features using `LicenseManager.IsFeatureEnabled()`.
- **Heartbeat (Concurrent Licenses):** For concurrent licenses, the client library periodically sends heartbeat requests to the Aegis.Server to maintain an active connection.

**Server-Side:**

- **API Endpoints:** Aegis.Server exposes RESTful API endpoints for license validation, activation, revocation, user authentication, and heartbeat monitoring.
- **Database:** A SQLite database is used to store license information, user data, activations, and other relevant data.
- **Authentication:** Aegis.Server uses JWT (JSON Web Tokens) for user authentication and authorization.
- **Heartbeat Monitor:** A background service monitors heartbeat requests from clients and automatically disconnects inactive concurrent users.

**Interaction:**

- Client applications communicate with the Aegis.Server using its API endpoints.
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
    - If users are being disconnected unexpectedly, check the heartbeat interval and timeout settings on both the client and server.
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

