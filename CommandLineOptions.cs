using CommandLine;

namespace rajapet
{
    public class CommandLineOptions
    {
        [Value(index: 0, Required = true, HelpText = "Email address to match.")]
        public string EmailAddress {get; set;}
    }
}