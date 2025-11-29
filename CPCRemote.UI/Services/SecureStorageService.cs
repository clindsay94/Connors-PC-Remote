using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Windows.Security.Credentials;

namespace CPCRemote.UI.Services;

/// <summary>
/// Item 11: Provides secure storage for sensitive data using Windows Credential Locker
/// with DPAPI fallback for unpackaged scenarios.
/// </summary>
[SupportedOSPlatform("windows10.0.22621.0")]
public class SecureStorageService
{
    private const string ResourceName = "CPCRemote";
    private readonly bool _useCredentialLocker;

    public SecureStorageService(bool useCredentialLocker = true)
    {
        _useCredentialLocker = useCredentialLocker && IsCredentialLockerAvailable();
    }

    private static bool IsCredentialLockerAvailable()
    {
        try
        {
            // Try to create a PasswordVault - this will throw if not available
            _ = new PasswordVault();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Stores a secret securely using Windows Credential Locker or DPAPI.
    /// </summary>
    public void StoreSecret(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (string.IsNullOrEmpty(value))
        {
            RemoveSecret(key);
            return;
        }

        if (_useCredentialLocker)
        {
            StoreInCredentialLocker(key, value);
        }
        else
        {
            StoreWithDpapi(key, value);
        }
    }

    /// <summary>
    /// Retrieves a secret from secure storage.
    /// </summary>
    public string? RetrieveSecret(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _useCredentialLocker
            ? RetrieveFromCredentialLocker(key)
            : RetrieveWithDpapi(key);
    }

    /// <summary>
    /// Removes a secret from secure storage.
    /// </summary>
    public void RemoveSecret(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_useCredentialLocker)
        {
            RemoveFromCredentialLocker(key);
        }
        else
        {
            RemoveFromDpapi(key);
        }
    }

    #region Credential Locker Implementation

    private void StoreInCredentialLocker(string key, string value)
    {
        try
        {
            var vault = new PasswordVault();
            
            // Remove existing credential if present
            RemoveFromCredentialLocker(key);
            
            // Store new credential
            var credential = new PasswordCredential(ResourceName, key, value);
            vault.Add(credential);
        }
        catch (Exception ex)
        {
            // Fall back to DPAPI if Credential Locker fails
            System.Diagnostics.Debug.WriteLine($"Credential Locker store failed, using DPAPI: {ex.Message}");
            StoreWithDpapi(key, value);
        }
    }

    private string? RetrieveFromCredentialLocker(string key)
    {
        try
        {
            var vault = new PasswordVault();
            var credential = vault.Retrieve(ResourceName, key);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch
        {
            // Try DPAPI fallback
            return RetrieveWithDpapi(key);
        }
    }

    private void RemoveFromCredentialLocker(string key)
    {
        try
        {
            var vault = new PasswordVault();
            var credentials = vault.FindAllByResource(ResourceName);
            foreach (var cred in credentials)
            {
                if (cred.UserName == key)
                {
                    vault.Remove(cred);
                }
            }
        }
        catch
        {
            // Ignore errors when removing non-existent credentials
        }
    }

    #endregion

    #region DPAPI Implementation (Fallback)

    private static readonly string DpapiStorePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CPCRemote",
        "secure");

    private void StoreWithDpapi(string key, string value)
    {
        try
        {
            System.IO.Directory.CreateDirectory(DpapiStorePath);
            var filePath = GetDpapiFilePath(key);
            
            byte[] plainBytes = Encoding.UTF8.GetBytes(value);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            
            System.IO.File.WriteAllBytes(filePath, encryptedBytes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DPAPI store failed: {ex.Message}");
        }
    }

    private string? RetrieveWithDpapi(string key)
    {
        try
        {
            var filePath = GetDpapiFilePath(key);
            if (!System.IO.File.Exists(filePath))
            {
                return null;
            }

            byte[] encryptedBytes = System.IO.File.ReadAllBytes(filePath);
            byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DPAPI retrieve failed: {ex.Message}");
            return null;
        }
    }

    private void RemoveFromDpapi(string key)
    {
        try
        {
            var filePath = GetDpapiFilePath(key);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    private static string GetDpapiFilePath(string key)
    {
        // Use a hash of the key to avoid file system issues with special characters
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        var fileName = Convert.ToHexString(hash)[..16] + ".sec";
        return System.IO.Path.Combine(DpapiStorePath, fileName);
    }

    #endregion
}
