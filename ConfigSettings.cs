using System;
using System.IO;
using Newtonsoft.Json;

namespace rajapet.isuserinapple
{
    /// <summary>
    /// C# Wrapper for the configuration settings file
    /// </summary>
    public class ConfigSettings
    {
        /// <summary>
        /// Path the private key file in PEMS format
        /// </summary>
        public string PrivateKeyFile {get; set;}

        /// <summary>
        /// Your private key ID from App Store Connect
        /// </summary>
        public string KeyID {get; set;}
        
        /// <summary>
        /// Your issuer ID from the API Keys page in App Store Connect
        /// </summary>
        public string IssuerID {get; set;}

        public ConfigSettings() {}
        public ConfigSettings(string fileName) : base()
        {
            _ = LoadFromFile(fileName);
        }
        
        public ConfigSettings LoadFromFile(string fileName)
        {
            var c = JsonConvert.DeserializeObject<ConfigSettings>(File.ReadAllText(fileName));
            
            this.PrivateKeyFile = c.PrivateKeyFile;
            this.KeyID = c.KeyID;
            this.IssuerID = c.IssuerID;
            
            return this;
        }
    }
}
