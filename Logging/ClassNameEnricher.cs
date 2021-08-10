using System.Collections.Concurrent;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace Dwight
{
    public class ClassNameEnricher : ILogEventEnricher
    {
        private const int PADDING = 30;

        private readonly ConcurrentDictionary<string, LogEventProperty> _propertyByClassName;

        public ClassNameEnricher()
        {
            _propertyByClassName = new();
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var sourceContext = (string) ((ScalarValue) logEvent.Properties["SourceContext"]).Value;
            var property = this._propertyByClassName.GetOrAdd(sourceContext, CreateFormattedLogEvent);

            logEvent.AddOrUpdateProperty(property);
        }

        private static LogEventProperty CreateFormattedLogEvent(string source)
        {
            var split = source.Split('.');

            if (split.Length == 1)
                return new("ClassName", new ScalarValue(PadClass(source)));
            
            var sourceClass = split[^1];

            var sourceNamespace = split[..^1];
            var firstNamespace = sourceNamespace[0];

            if (firstNamespace != typeof(ClassNameEnricher).Namespace)
            {
                var shortenedNamespace = string.Join('.', sourceNamespace.Select(name => name[0]));
                sourceClass = $"{shortenedNamespace}.{sourceClass}";
            }

            sourceClass = PadClass(sourceClass);

            return new("ClassName", new ScalarValue(sourceClass));
        }

        private static string PadClass(string name)
        {
            return name.Length > PADDING
                ? $"{name[..(PADDING - 3)]}..."
                : name.PadRight(PADDING, ' ');
        }
    }
}