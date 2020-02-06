using System.Reflection;

namespace Reactive.Testing.Marbles
{
    static class ReflectionExtensions
    {
        public static T GetReflectedProperty<T>(this object instance, string propertyName)
        {
            var type = instance.GetType();
            var property = type.GetRuntimeProperty(propertyName);
            
            var value = property.GetValue(instance);
            return (T) value;
        }
    }
}