//MIT, 2018, Phycolos
using System;
using System.Collections.Generic;

using System.Text;
using System.Windows.Forms;
using SharpConnect.MySql;
using SharpConnect.MySql.SyncPatt;
using SharpConnect.MySql.Parser;

namespace TestMySqlParser
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string db = textBox1.Text;
            string tb = textBox2.Text;
            string createSql = "";

            string h = "127.0.0.1";
            string u = "root";
            string p = "root";
            int port = 3306;

            MySqlConnectionString connStr = new MySqlConnectionString(h, u, p, db, port);
            MySqlConnection mySqlConn = new MySqlConnection(connStr);
            mySqlConn.Open();

            string sql = "SHOW CREATE TABLE " + tb;
            var cmd = new MySqlCommand(sql, mySqlConn);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                createSql = reader.GetString(1);
            }

            //----------------------
            //1. tokenization => tokenizer
            //2. parse => parser
            //3. semantic checking => semantic checker

            //1.1 
            if (createSql == "")
            {
                return;
            }

            MySqlParser parser = new MySqlParser();
            parser.ParseSql(createSql);

            MySqlInfoToCsCodeGenerator tableInfoToCsCodeGen = new MySqlInfoToCsCodeGenerator();

            List<TablePart> tables = parser.ResultTables;
            foreach(TablePart t in tables)
            {
                tableInfoToCsCodeGen.ConvertSQL(t, db);
            } 
        }




    }
}
