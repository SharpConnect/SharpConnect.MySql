//MIT 2015, brezza27, EngineKit and contributors

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
        public TestAttribute(string desc) { this.Description = desc; }
        public string Description { get; set; }

    }


    public static class Report
    {
        static StringBuilder stbuilder = new StringBuilder();
        public static void Clear()
        {
            stbuilder.Length = 0;
        }
        public static void WriteLine(string info)
        {
            stbuilder.AppendLine(info);
        }
        public static string GetReportText()
        {
            return stbuilder.ToString();
        }

    }

    public class TestCase
    {
        MethodInfo testMethod;
        string testName;
        public TestCase(string name, MethodInfo testMethod)
        {
            this.testMethod = testMethod;
            this.testName = name;
        }
        public override string ToString()
        {
            return this.testName;
        }
        public void Run()
        {
            testMethod.Invoke(null, null);
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
                    Attribute found = m.GetCustomAttribute(testAttrType);
                    if (found != null)
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

    public class MySqlTester
    {
        public static void Test(int n, TimeUnit timeUnit, out long total, out long avg, Action ac)
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