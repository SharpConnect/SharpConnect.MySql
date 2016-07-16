//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect;
namespace MySqlTest
{
    //------------------------------------
    //this sample is designed for .net2.0 
    //that dose not have Task library
    //------------------------------------
    class BasicTask
    {
        Action action;
        public BasicTask(Action action)
        {
            this.action = action;
        }
        public void Start()
        {
            action();
        }
    }

    class TaskChain
    {
        int currentIndex = 0;
        Action onFinish;
        bool pleaseStop;

        List<BasicTask> taskList = new List<BasicTask>();
        public BasicTask AddTask(BasicTask t)
        {
            taskList.Add(t);
            return t;
        }
        public BasicTask AddTask(Action a)
        {
            BasicTask basicTask = new BasicTask(a);
            taskList.Add(basicTask);
            return basicTask;
        }
        public void Start()
        {
            pleaseStop = false;
            if (taskList.Count > 0)
            {
                currentIndex = 0;
                taskList[0].Start();
            }
        }
        public void Finish(Action onFinish)
        {
            this.onFinish = onFinish;
        }
        public void Stop()
        {
            pleaseStop = true;
        }
        public void Next()
        {
            if (pleaseStop)
            {
                //just stop
            }
            else
            {
                if (currentIndex + 1 < taskList.Count)
                {
                    currentIndex++;
                    taskList[currentIndex].Start();
                }
                else
                {
                    //finish
                    if (onFinish != null)
                    {
                        onFinish();
                    }
                }
            }
        }
    }

    public class TestSet2Async : MySqlTestSet
    {
        [Test]
        public static void T_InsertAndSelect_Async()
        {




            System.Diagnostics.Stopwatch stopW = new System.Diagnostics.Stopwatch();
            stopW.Start();
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.UseConnectionPool = true;

            var tasks = new TaskChain();
            tasks.AddTask(() =>
            {
                conn.Open(tasks.Next);
            });
            tasks.AddTask(() =>
            {
                //drop table if exist
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery(tasks.Next);
            });

            tasks.AddTask(() =>
            {
                string sql = "create table test001(col_id  int(10) unsigned not null auto_increment, col1 int(10)," +
                   "col2 char(2),col3 varchar(255),col4 datetime, primary key(col_id) )";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery(tasks.Next);
            });

            for (int i = 0; i < 2000; ++i)
            {
                tasks.AddTask(() =>
                {
                    string sql = "insert into test001(col1,col2,col3,col4) values(10,'AA','123456789','0001-01-01')";
                    var cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery(tasks.Next);
                });
            }

            tasks.AddTask(() =>
            {
                conn.Close(); tasks.Next();
            });
            //----------------------------------------
            tasks.Start();
            tasks.Finish(() =>
            {
                stopW.Stop();
                Report.WriteLine("avg:" + stopW.ElapsedTicks);
            });


        }
    }
}