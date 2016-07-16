

#if NET20
namespace SharpConnect
{
    public delegate void Action();
    public delegate R Func<R>();
    public delegate R Func<T, R>(T t);
}
namespace System.Runtime.CompilerServices
{
    public class ExtensionAttribute : Attribute { }
}
#endif