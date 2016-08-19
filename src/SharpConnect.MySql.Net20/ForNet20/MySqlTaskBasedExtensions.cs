//MIT, 2016, brezza92, EngineKit and contributors 

using System;
using System.Collections.Generic;

namespace SharpConnect.MySql.BasicAsyncTasks
{
    //------------------------------------
    //this sample is designed for .net2.0 
    //that dose not have Task library
    //------------------------------------
    public static class MySqlTaskBasedExtension
    {
        public static ActionTask OpenAsync(this MySqlConnection conn)
        {
            return new ActionTask(ch =>
            {
                conn.Open(ch.Next);
            });
        }
        public static ActionTask CloseAsync(this MySqlConnection conn)
        {
            return new ActionTask(ch =>
            {
                conn.Close(ch.Next);
            });
        }
        //------------------------------------------------------------
        public static ActionTask PrepareAsync(this MySqlCommand cmd)
        {
            return new ActionTask(ch =>
            {
                cmd.Prepare(ch.Next);
            });
        }
        public static ActionTask ExecuteNonQueryAsync(this MySqlCommand cmd)
        {
            return new ActionTask(ch =>
            {
                cmd.ExecuteNonQuery(ch.Next);
            });
        }
        public static ActionTask ExecuteReaderAsync(this MySqlCommand cmd, Action<MySqlDataReader> readerReady)
        {
            return new ActionTask(ch =>
            {
                cmd.ExecuteReader(reader =>
                {
                    //reader is ready for read
                    readerReady(reader);
                    ch.Next();
                });
            });
        }
        //-----------------------------------------------------------------------------
        public static ActionTask CloseAsync(this MySqlDataReader reader)
        {
            return new ActionTask(ch =>
            {
                reader.Close(ch.Next);
            });
        }
    }

    public abstract class BasicTaskBase
    {
        public BasicTaskBase()
        {
        }
        public TaskChain OwnerTaskChain
        {
            get;
            set;
        }
        public abstract void Start();
        public void Wait()
        {
            //wait until this task complete
        }
        public void StartAndWait()
        {

        }
    }

    public class ActionTask : BasicTaskBase
    {
        Action<TaskChain> action;
        public ActionTask(Action<TaskChain> action)
        {
            this.action = action;
        }
        public override void Start()
        {
            action(this.OwnerTaskChain);
        }
    }

    

    public class TaskChain
    {
        int currentIndex = 0;
        Action onFinish;
        Action onBeginTask;
        bool pleaseStop;
        List<BasicTaskBase> taskList = new List<BasicTaskBase>();
        public BasicTaskBase AddTask(BasicTaskBase t)
        {
            t.OwnerTaskChain = this;
            taskList.Add(t);
            return t;
        }
        public BasicTaskBase AddTask(Action<TaskChain> a)
        {
            BasicTaskBase basicTask = new ActionTask(a);
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

                if (onBeginTask != null)
                {
                    onBeginTask();
                }
                taskList[0].Start();
            }
        }
        public void WhenFinish(Action onFinish)
        {
            this.onFinish = onFinish;
        }
        public void WhenTaskBegin(Action onBeginTask)
        {
            this.onBeginTask = onBeginTask;
        }
        public void Stop()
        {
            //stop the chain
            //but not cancel task execution
            pleaseStop = true;
        }
        public void Next()
        {
            if (pleaseStop)
            {
                //just stop
                //not exec further
            }
            else
            {
                if (currentIndex + 1 < taskList.Count)
                {
                    currentIndex++;
                    if (onBeginTask != null)
                    {
                        onBeginTask();
                    }
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
        public int CurrentTaskIndex
        {
            get
            {
                return currentIndex;
            }
        }
        public int TaskCount
        {
            get
            {
                return taskList.Count;

            }
        }

        public static TaskChain operator +(TaskChain taskChain, BasicTaskBase task)
        {
            taskChain.AddTask(task);
            return taskChain;
        }
    }

}