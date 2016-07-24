using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using MySqlTest;
namespace ReferenceMySqlClient
{
    public partial class FormTestSuite : Form
    {
        List<TestCase> testList = new List<TestCase>();
        public FormTestSuite()
        {
            InitializeComponent();
        }
        private void FormTestSuite_Load(object sender, EventArgs e)
        {
            this.Text = "Test";
            listboxTestCases.DoubleClick += ListboxTestCases_DoubleClick;
            LoadTestCases();
        }
        void LoadTestCases()
        {
            testList.Clear();
            listboxTestCases.Items.Clear();
            TestCaseExtracter.ExtractTestCase(this.GetType().Assembly, testList);
            //load into listbox
            int j = testList.Count;
            for (int i = 0; i < j; ++i)
            {
                listboxTestCases.Items.Add(testList[i]);
            }
        }
        private void ListboxTestCases_DoubleClick(object sender, EventArgs e)
        {
            TestCase testCase = listboxTestCases.SelectedItem as TestCase;
            if (testCase != null)
            {
                GC.Collect();
                Report.Clear();
                testCase.Run();
                this.textBox1.Text = Report.GetReportText();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //TestSet2.T_InsertAndSelect();
            //#if DEBUG
            //            //TestSet1.T_NumRange();
            //            TestSet1.T_FloatingRange();
            //           // TestSet_Blob.T_InsertBlobData();
            //            //dbugInternal.Test1();
            //#endif

#if DEBUG
            //DateTime d = new DateTime(0, 0, 0, 0, 0, 0);

            //TestSet1.T_NumRange();
            //TestSet1.T_FloatingRange();
            for (int i = 0; i < 100; ++i)
            {
                Console.WriteLine("ROUND: " + i);
                Test_StoreProc_MultiResultSet.T_StoreProcMultiResultSet();
            }
            // TestSet_Blob.T_InsertBlobData();
            //dbugInternal.Test1();
#endif
        }
    }
}
