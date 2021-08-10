namespace Dwight
{
    public static class LoggingTemplate
    {
        public const string CONSOLE = "{Timestamp: HH:mm:ss} | {Level,-15} | {ClassName} | {Message}{NewLine}{Exception}";
        public const string FILE = "{Timestamp:dd-MM-yyyy HH:mm:ss} | {Level} | {SourceContext} | {Message}{NewLine}{Exception}";
    }
}