
using System;
using System.Reflection;

#if NET20
namespace System
{
    public delegate void Action();
    //public delegate void Action<T>(T t);
    public delegate void Action<T1, T2>(T1 t1, T2 t2);
    public delegate void Action<T1, T2, T3>(T1 t1, T2 t2, T3 t3);
    public delegate void Action<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4);
    //
    public delegate R Func<R>();
    //public delegate R Func<T, R>(T t);
    //

    public static class DelegateExtensionMethods
    {

        public static MethodInfo GetMethodInfo(this Delegate del)
        {
            return del.Method;
        }
    }
}
namespace System.Runtime.CompilerServices
{
    public partial class ExtensionAttribute : Attribute { }
}
#endif