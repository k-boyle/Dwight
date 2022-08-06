using System.Collections.Generic;

namespace Dwight;

public record ClientError(string Reason, string Message, string Type, Dictionary<string, string> Detail);