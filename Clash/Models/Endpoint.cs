using System;

namespace Dwight;

public class Endpoint<T> where T : class
{
    public string PathTemplate { get; }
    public string? Parameter { get; }

    public Endpoint(string pathTemplate)
    {
        PathTemplate = pathTemplate;
        Parameter = null;
    }

    private Endpoint(string pathTemplate, string parameter)
    {
        PathTemplate = pathTemplate;
        Parameter = parameter;
    }

    public string Format()
    {
        if (Parameter == null)
            throw new NullReferenceException();
        
        return string.Format(PathTemplate, Uri.EscapeDataString(Parameter));
    }

    public Endpoint<T> With(string parameter) => new(PathTemplate, parameter);
}
