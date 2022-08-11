using System;

namespace Dwight;

public class ClientError<T> : Exception
{
    private readonly T _error;
    
    public ClientError(T error)
    {
        _error = error;
    }
}