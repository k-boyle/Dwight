using System;

namespace Dwight;

public class DevApiFailure : Exception
{
    public string Error { get; }
    public string Description { get; }

    public DevApiFailure(string error, string description)
    {
        this.Error = error;
        this.Description = description;
    }
}
