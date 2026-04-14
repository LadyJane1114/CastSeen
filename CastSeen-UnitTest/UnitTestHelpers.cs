namespace CastSeen_UnitTest;

using CastSeen.ViewModels;
using System.Reflection;
using System.Runtime.Serialization;

internal static class UnitTestHelpers
{
    public static ActorsViewModel CreateUninitializedActorsViewModel()
        => (ActorsViewModel)FormatterServices.GetUninitializedObject(typeof(ActorsViewModel));

    public static void SetPrivateField<T>(object instance, string fieldName, T value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(instance, value);
    }

    public static bool InvokePrivateBoolMethod(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        return (bool)method.Invoke(instance, null)!;
    }

    public static void InvokePrivateVoidMethod(object instance, string methodName, object? parameter)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        method.Invoke(instance, new[] { parameter });
    }
}

