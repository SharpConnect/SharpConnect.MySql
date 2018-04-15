//MIT, 2018, Phycolos
using System;
using System.Collections.Generic;

using System.Text;
using System.Windows.Forms;
using SharpConnect.MySql;
using SharpConnect.MySql.SyncPatt;
using SharpConnect.MySql.SqlLang;
using SharpConnect.MySql.Information;

namespace TestMySqlParser
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        MySqlConnectionString GetLocalConnString()
        {
            string db = "test";
            string h = "127.0.0.1";
            string u = "root";
            string p = "root";
            int port = 3306;
            return new MySqlConnectionString(h, u, p, db, port);
        }

        private void tlstrpRefreshDBs_Click(object sender, EventArgs e)
        {
            //generate list of databases
            //and its table

            var connStr = GetLocalConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();

            //connect to server name
            MySqlDbServerInfo serverInfo = new MySqlDbServerInfo("test");
            serverInfo.ReloadDatabaseList(conn);

            this.treeView1.Nodes.Clear();

            foreach (MySqlDatabaseInfo dbInfo in serverInfo.Databases.Values)
            {
                dbInfo.ReloadTableList(conn, true);
                dbInfo.ReloadStoreFuncList(conn, true);
                dbInfo.ReloadStoreProcList(conn, true);

                //show database ...
                TreeNode dbNode = new TreeNode("database:" + dbInfo.Name);
                dbNode.Tag = dbInfo;
                this.treeView1.Nodes.Add(dbNode);

                foreach (MySqlTableInfo tbl in dbInfo.Tables)
                {
                    TreeNode tblNode = new TreeNode("table:" + tbl.Name);
                    tblNode.Tag = tbl;
                    foreach (MySqlColumnInfo colInfo in tbl.Columns)
                    {
                        TreeNode colNode = new TreeNode("col:" + colInfo.Name + "," + colInfo.FieldTypeName);
                        colNode.Tag = colInfo;
                        tblNode.Nodes.Add(colNode);
                    }
                    dbNode.Nodes.Add(tblNode);
                    //show table information
                }

            }
            conn.Close();
        }

        private void tlstrpGenCsCode_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;
            //
            MySqlTableInfo selectedTableInfo = treeView1.SelectedNode.Tag as MySqlTableInfo;
            if (selectedTableInfo == null) return;
            //---------------

            //create data from selected tree node
            //show create table
            string db = selectedTableInfo.OwnerDatabase.Name;
            string tb = selectedTableInfo.Name;

            
            MySqlConnection mySqlConn = new MySqlConnection(GetLocalConnString());
            mySqlConn.Open();


            string createSql = "";
            string sql = "SHOW CREATE TABLE " + db + "." + tb;
            var cmd = new MySqlCommand(sql, mySqlConn);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                createSql = reader.GetString(1);
            }
            reader.Close();
            //-----------------------
            if (createSql == "")
            {
                return;
            }
            MySqlParser parser = new MySqlParser();
            parser.ParseSql(createSql);

            MySqlInfoToCsCodeGenerator tableInfoToCsCodeGen = new MySqlInfoToCsCodeGenerator();

            StringBuilder result = new StringBuilder();
            List<TablePart> tables = parser.ResultTables;
            foreach (TablePart table in tables)
            {
                if (table.DatabaseName == null)
                {
                    table.DatabaseName = db;
                }
                //
                tableInfoToCsCodeGen.GenerateCsCode(table, result);
                //tableInfoToCsCodeGen.GenerateSqlAndSave(table, "code." + db + "." + table + ".cs");
            }
            mySqlConn.Close();
            //
            this.textBox1.Text = result.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
