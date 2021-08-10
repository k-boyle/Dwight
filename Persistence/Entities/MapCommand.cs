using System;

namespace Dwight
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MapCommandAttribute : Attribute
    {
        public Type ParameterType { get; }

        public MapCommandAttribute(Type parameterType)
        {
            ParameterType = parameterType;
        }
    }
}