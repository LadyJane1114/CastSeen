using CastSeen.ViewModels;
using System.Reflection;
using System.Runtime.Serialization;

namespace CastSeen_UnitTest;

internal static class UnitTestHelpers
{
    public static T CreateUninitialized<T>() where T : class
        => (T)FormatterServices.GetUninitializedObject(typeof(T));

    public static void SetPrivateField<T>(object instance, string fieldName, T value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(instance, value);
    }
}

