//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
namespace MySqlTest
{
    public enum TimeUnit
    {
        Ticks,
        Millisec
    }

    public class TestAttribute : Attribute
    {
        public TestAttribute() { }
        public TestAttribute(string desc) { Description = desc; }
        public string Description { get; set; }
    }


    public static class Report
    {
        static StringBuilder s_stbuilder = new StringBuilder();
        public static void Clear()
        {
            s_stbuilder.Length = 0;
        }
        public static void WriteLine(string info)
        {
#if DEBUG
            Console.WriteLine(info);
#endif

            s_stbuilder.AppendLine(info);
        }
        public static string GetReportText()
        {
            return s_stbuilder.ToString();
        }
    }

    public class TestCase
    {
        MethodInfo _testMethod;
        string _testName;
        public TestCase(string name, MethodInfo testMethod)
        {
            _testMethod = testMethod;
            _testName = name;
        }
        public override string ToString()
        {
            return _testName;
        }
        public void Run()
        {
            _testMethod.Invoke(null, null);
        }
    }

    public static class TestCaseExtracter
    {
        public static void ExtractTestCase(Type fromType, List<TestCase> output)
        {
            //extract only public method with test attribute
            MethodInfo[] methodInfos = fromType.GetMethods();
            int j = methodInfos.Length;
            Type testAttrType = typeof(TestAttribute);
            for (int i = 0; i < j; ++i)
            {
                MethodInfo m = methodInfos[i];
                //and static method only
                if (m.IsStatic)
                {
                    object[] founds = m.GetCustomAttributes(testAttrType, false);
                    if (founds.Length > 0)
                    {
                        output.Add(new TestCase(m.Name, m));
                    }
                }
            }
        }

        public static void ExtractTestCase(Assembly fromAsm, List<TestCase> output)
        {
            foreach (var type in fromAsm.GetTypes())
            {
                ExtractTestCase(type, output);
            }
        }
    }

    public delegate void TestAction();
    public abstract class MySqlTesterBase
    {
        public static void Test(int n, TimeUnit timeUnit, out long total, out long avg, TestAction ac)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = n; i > 0; --i)
            {
                ac();
            }
            sw.Stop();
            total = sw.ElapsedTicks;
            avg = total / n;
        }
    }
}