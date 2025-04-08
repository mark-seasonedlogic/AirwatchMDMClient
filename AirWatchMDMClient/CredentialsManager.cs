using System;
using System.Net;
using System.Security;
using System.Text;
using System.Text.Unicode;
using System.Xml.Serialization;
using System.Management.Automation;
using System.Xml.Linq;

namespace BBIHardwareSupport
{
public class CredentialsManager
{
    private  PSCredential _psCredential {get;set;}
    private string _username { get; set; }
    private string _password { get; set; }
    public string TenantCode { get; } = "TMq2c1xaCR6mG5Z0XjJmSFpp+Nqk0JFPzmVmPxWtWTU=";

    public CredentialsManager(string credentialsFilePath)
    {
        (this._username, this._password) = ImportCredentialFromXml(credentialsFilePath);

        
    }
    /// <summary>
    /// Get credentials via windows prompts
    /// </summary>
    /// <returns></returns>
    public CredentialsManager(string username, string password)
    {
        this._username = username;
        this._password = password;
    }

    
public string GetAuthorizationHeader()
{
   var credentials = $"{_username}:{_password}";

            // Convert the credentials to bytes
            var credentialBytes = Encoding.UTF8.GetBytes(credentials);

            // Encode the byte array to a Base64 string
            var encodedCredentials = Convert.ToBase64String(credentialBytes);

            // Format the Authorization header value as "Basic {Base64EncodedCredentials}"
            return $"Basic {encodedCredentials}";
}
private static string SecureStringToString(SecureString secureString)
        {
            if (secureString == null)
                return string.Empty;

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
private (string userName, string password) ImportCredentialFromXml(string filePath)
{
 using (PowerShell ps = PowerShell.Create())
            {
                // Run PowerShell command to import the credential from the XML file
                ps.AddCommand("Import-Clixml").AddArgument(filePath);

                var result = ps.Invoke();

                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Console.WriteLine($"Error: {error.ToString()}");
                    }
                    throw new InvalidOperationException("Failed to import credentials.");
                }

                // Extract PSCredential
                var psCredential = result[0].BaseObject as PSCredential;
                if (psCredential == null)
                    throw new InvalidOperationException("Failed to convert imported data to PSCredential.");

                string userName = psCredential.UserName;
                string password = SecureStringToString(psCredential.Password);

                return (userName, password);
            }
    }

 
   }
}