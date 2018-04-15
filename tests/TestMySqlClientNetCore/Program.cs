//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using MySqlTest;
using System.Reflection;
namespace TestOnNetCore
{
    public class Program
    {
        static List<TestCase> testList = new List<TestCase>();
        public static void Main(string[] args)
        {
            Console.WriteLine("SharpConnect.MySql Test");
            Console.WriteLine("hello .NetCore 1.0");
            Console.WriteLine("");
            Console.WriteLine("---");
            TestCaseExtracter.ExtractTestCase(typeof(Program).GetTypeInfo().Assembly, testList);

            AGAIN_HERE:
            //---------
            int j = testList.Count;
            for (int i = 0; i < j; ++i)
            {
                TestCase t = testList[i];
                Console.WriteLine("[" + i + "] " + t.Name);
            }
            Console.WriteLine("");
            Console.WriteLine("type test case number and enter, or type 'x' to exit");
            string userInput = Console.ReadLine();
            if (userInput == "x")
            {
                //exit
                return;
            }
            else
            {
                int selectedNum;
                if (int.TryParse(userInput, out selectedNum))
                {
                    Console.WriteLine("running ...");
                    Console.WriteLine("");
                    if (selectedNum < j)
                    {
                        //select that test
                        testList[selectedNum].Run();
                    }
                    Console.WriteLine("finished");
                    Console.WriteLine("---");
                    Console.WriteLine("");
                    Console.WriteLine("");
                    goto AGAIN_HERE;
                }
            }
            //-------------
        }
    }
}
