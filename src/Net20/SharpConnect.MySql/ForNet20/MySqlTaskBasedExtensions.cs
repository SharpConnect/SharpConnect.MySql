//MIT, 2016, brezza92, EngineKit and contributors 

using System.Collections.Generic;

namespace SharpConnect.MySql
{
    //------------------------------------
    //this sample is designed for .net2.0 
    //that dose not have Task library
    //------------------------------------
    public static class MySqlTaskBasedExtension
    {
        public static BasicTask OpenAsync(this MySqlConnection conn)
        {
            return new BasicTask(ch =>
            {
                conn.Open(ch.Next);
            });
        }
        public static BasicTask CloseAsync(this MySqlConnection conn)
        {
            return new BasicTask(ch =>
            {
                conn.Close(ch.Next);
            });
        }
        //------------------------------------------------------------
        public static BasicTask PrepareAsync(this MySqlCommand cmd)
        {
            return new BasicTask(ch =>
            {
                cmd.Prepare(ch.Next);
            });
        }
        public static BasicTask ExecuteNonQueryAsync(this MySqlCommand cmd)
        {
            return new BasicTask(ch =>
            {
                cmd.ExecuteNonQuery(ch.Next);
            });
        }
        public static BasicTask ExecuteReaderAsync(this MySqlCommand cmd)
        {
            return new BasicTask(ch =>
            {
                cmd.ExecuteReader(ch.Next);
            });
        }
        //-----------------------------------------------------------------------------
        public static BasicTask CloseAsync(this MySqlDataReader reader)
        {
            return new BasicTask(ch =>
            {
                reader.Close(ch.Next);
            });
        }
    }




    public class BasicTask
    {
        Action<TaskChain> action;
        public BasicTask(Action<TaskChain> action)
        {
            this.action = action;
        }
        public TaskChain OwnerTaskChain
        {
            get;
            set;
        }
        public void Start()
        {
            action(this.OwnerTaskChain);
        }
        public void Wait()
        {
            //wait until this task complete
        }
        public void StartAndWait()
        {

        }
    }

    public class TaskChain
    {
        int currentIndex = 0;
        Action onFinish;
        bool pleaseStop;

        List<BasicTask> taskList = new List<BasicTask>();
        public BasicTask AddTask(BasicTask t)
        {
            t.OwnerTaskChain = this;
            taskList.Add(t);
            return t;
        }
        public BasicTask AddTask(Action<TaskChain> a)
        {
            BasicTask basicTask = new BasicTask(a);
            basicTask.OwnerTaskChain = this;
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
        public static TaskChain operator +(TaskChain taskChain, BasicTask task)
        {
            taskChain.AddTask(task);
            return taskChain;
        }
    }

}