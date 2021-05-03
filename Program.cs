using CommandLine;
using System;
using System.IO;
using System.Linq;
using rajapet.Apple;
/*
Step one
dotnet new console -n IsUserInApple -f net5.0
dotnet new gitignore

Step 2 add dependences

dotnet add package jose-jwt --version 3.1.1

dotnet add package CommandLineParser --version 2.8.0
https://devblogs.microsoft.com/ifdef-windows/command-line-parser-on-net5/
*/

namespace rajapet.isuserinapple
{
    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CommandLineOptions>(args)
                .MapResult((CommandLineOptions opts) =>
                {
                    try
                    {
                        return MatchEmail(opts.EmailAddress);
                    }
                    catch
                    {
                        Console.WriteLine("Error!");
                        return -3; // Unhandled error
                    }
                },
                errs => -1); // Invalid arguments
            
        }

        private static int MatchEmail(string EmailAddress)
        {
            var service = GetService();

            var user = service.FindUser(EmailAddress);

            if (user != null)
            {
                var roles = string.Join(", ", user.Roles.Select(s => s.ToString()).ToArray());
                Console.WriteLine($"Found user: {EmailAddress}, {user.FirstName} {user.LastName} [{roles}]");
                return 0;
            }
            else
            {
                Console.WriteLine($"User: {EmailAddress} not found");
                return 1;
            }
        }

        private static AppStoreApiService GetService()
        {
            var p = new Program();

            var configFile = p.GetConfigSettings();
            var privateKey = p.GetPrivateKey(configFile);

            var appleJWT = new AppleJWT();

            var token = appleJWT.GetToken(configFile.KeyID, configFile.IssuerID, privateKey);

            return new AppStoreApiService(token);
        }

        /// <summary>
        /// Reads a configuration file from the same folder as the executable
        /// </summary>
        /// <returns></returns>
        private ConfigSettings GetConfigSettings()
        {
            var configFileName = Path.Combine(System.AppContext.BaseDirectory, "IsUserinApple.json");

            return new ConfigSettings(configFileName);
        }

        /// <summary>
        /// Read the PEMS files specfied by configSettings.PrivateKeyFile and return the private key
        /// </summary>
        /// <param name="configSettings"></param>
        /// <returns>Private key in Base64 encoding</returns>
        private string GetPrivateKey(ConfigSettings configSettings)
        {
            var certPEM = File.ReadAllText(configSettings.PrivateKeyFile);

            return certPEM
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Replace("-----END PRIVATE KEY-----", "");
        }
       
    }
}
