//MIT, 2016-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;

namespace SharpConnect.MySql.AsyncPatt
{
    //------------------------------------
    //this sample is designed for .net2.0 
    //that dose not have Task library
    //but we intend to design the api different from 
    //original TAP model
    //here:
    //we pass TaskChain as an arg to async method
    //and make sure that the task is add into the taskchain
    //------------------------------------
    public static class MySqlTaskBasedExtension
    {
        public static ActionTask AsyncOpen(this MySqlConnection conn, TaskChain ch)
        {
            return ch.AddTask(() =>
            {
                ch.AutoCallNext = false;
                //open connection async
                //after finish then call next task in task chain
                conn.InternalOpen(ch.Next);
            });
            //not use autocall next task, let the connection call it when ready ***
        }
        public static ActionTask AsyncClose(this MySqlConnection conn, TaskChain ch)
        {
            return ch.AddTask(() =>
            {
                ch.AutoCallNext = false;
                conn.Close(ch.Next);

            });
            //not use autocall next task, let the connection call it when ready ***
        }
        //------------------------------------------------------------
        public static ActionTask AsyncPrepare(this MySqlCommand cmd, TaskChain ch)
        {
            return ch.AddTask(() =>
            {
                ch.AutoCallNext = false;
                cmd.Prepare(ch.Next);

            });
            //not use autocall next task, let the cmd call it when ready ***
        }
        public static ActionTask AsyncExecuteNonQuery(this MySqlCommand cmd, TaskChain ch)
        {
            return ch.AddTask(() =>
            {
                ch.AutoCallNext = false;
                cmd.ExecuteNonQuery(ch.Next);

            });
            //not use autocall next task, let the cmd call it when ready ***
        }
        public static ActionTask AsyncExecuteReader(this MySqlCommand cmd, TaskChain ch, Action<MySqlDataReader> readerReady)
        {
            return ch.AddTask(() =>
            {
                ch.AutoCallNext = false;
                cmd.InternalExecuteReader(subtable =>
                {
                    //this method is respond for call next ***
                    ch.AutoCallNext = true;
                    //**
                    //fetch data
                    while (subtable.InternalRead())
                    {
                        readerReady(subtable);
                        if (subtable.StopReadingNextRow)
                        {
                            break;
                        }
                    }
                    //
                    subtable.Close(() => { });
                    //
                    if (ch.AutoCallNext)
                    {
                        ch.Next();
                    }
                });
            });
        }
        public static ActionTask AsyncExecuteSubTableReader(this MySqlCommand cmd, TaskChain ch, Action<MySqlDataReader> readerReady)
        {
            return ch.AddTask(() =>
            {
                ch.AutoCallNext = false;
                cmd.InternalExecuteSubTableReader(subtable =>
                {
                    //this method is respond for call next ***
                    ch.AutoCallNext = true;
                    readerReady(subtable);
                    if (ch.AutoCallNext)
                    {
                        ch.Next();
                    }
                });
            });
            //not use autocall next task, let the cmd call it when ready ***
        }
        public static ActionTask AsyncExecuteScalar<T>(this MySqlCommand cmd, TaskChain ch, Action<T> resultReady)
        {
            return ch.AddTask(() =>
            {
                ch.AutoCallNext = false;
                cmd.ExecuteScalar<T>(result =>
                {
                    ch.AutoCallNext = true;
                    resultReady(result);
                    if (ch.AutoCallNext)
                    {
                        ch.Next();
                    }
                });
            });
            //not use autocall next task, let the cmd call it when ready ***
        }

        public static ActionTask AsyncClose(this MySqlDataReader reader, TaskChain ch)
        {
            return ch.AddTask(() =>
            {
                ch.AutoCallNext = false;
                reader.Close(ch.Next);
            });
            //not use autocall next task, let the reader call it when ready ***
        }

    }

    public abstract class BasicTaskBase
    {
        public BasicTaskBase(TaskChain tc)
        {
            this.OwnerTaskChain = tc;
        }
        public TaskChain OwnerTaskChain
        {
            get;
            private set;
        }
        public abstract void Start();



        //user can assign name for this task
        //mainly purpose for debuging
        public string Name { get; set; }

    }

    public enum TaskStatus
    {
        Init,
        Running,
        Finish
    }

    public class ActionTask : BasicTaskBase
    {
        Action action;
        TaskStatus taskStatus;

        public ActionTask(TaskChain tc, Action action)
            : base(tc)
        {

            this.action = action;
        }
        public TaskStatus Status
        {
            get { return taskStatus; }
        }
        public override void Start()
        {
            //each task must run once ***
            switch (taskStatus)
            {
                case TaskStatus.Init:

                    this.OwnerTaskChain.AutoCallNext = true; //auto set to true
                    taskStatus = TaskStatus.Running;
                    action();
                    taskStatus = TaskStatus.Finish;

                    if (this.OwnerTaskChain.AutoCallNext)
                    {
                        this.OwnerTaskChain.Next();
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }



    /// <summary>
    /// task step result
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TR<T>
    {
        public T result;
        public TR()
        {
        }
    }
    public class TaskChain
    {
        /// <summary>
        /// latest run task index
        /// </summary>
        int _currentIndex = -1;
        int _insertIndex = -1;
        Action _onFinish;
        Action _onBeginTask;
        bool _pleaseStop;
        bool _isStarted = false;
        bool _finish = false;
        List<BasicTaskBase> _taskList = new List<BasicTaskBase>();


        void AddTask(BasicTaskBase actionTask)
        {
            if (_insertIndex < 0)
            {
                //un-start so
                _taskList.Add(actionTask);
            }
            else
            {
                if (_insertIndex == _taskList.Count - 1)
                {
                    //append to last task
                    _taskList.Add(actionTask);
                }
                else
                {
                    _taskList.Insert(_insertIndex + 1, actionTask);
                }

                _insertIndex++;
            }
        }

        public ActionTask AddTask(Action a)
        {
            var actionTask = new ActionTask(this, a);
            AddTask(actionTask);
            return actionTask;
        }

        public void Start(Action whenFinish = null)
        {
            //start once ***
            if (_isStarted)
            {
                throw new Exception("task chain has started!");
            }
            if (whenFinish != null)
            {
                if (_onFinish != null)
                {
                    throw new Exception("task chain has on finish handler!");
                }
                else
                {
                    _onFinish = whenFinish;
                }
            }
            //------------------------------
            _isStarted = true;
            _pleaseStop = false;
            this.AutoCallNext = true;
            if (_taskList.Count > 0)
            {
                //finish task
                this.AddTask(() =>
                {
                });
                //---------------------------------
                //update insert index= current index
                _insertIndex = _currentIndex = 0;
                if (_onBeginTask != null)
                {
                    _onBeginTask();
                }
                _taskList[0].Start();
            }
        }
        public void WhenFinish(Action onFinish)
        {
            _onFinish = onFinish;
        }
        public void BeforeEachTaskBegin(Action onBeginTask)
        {
            _onBeginTask = onBeginTask;
        }
        public void Stop()
        {
            //stop the chain
            //but not cancel task execution
            _pleaseStop = true;
        }

        internal void Next()
        {
            if (_pleaseStop)
            {
                //just stop
                //not exec further
            }
            else
            {
                if (_currentIndex + 1 < _taskList.Count)
                {
                    _insertIndex = ++_currentIndex;
                    //update insert index= current index***

                    if (_onBeginTask != null)
                    {
                        _onBeginTask();
                    }

                    _taskList[_currentIndex].Start();
                }
                else
                {
                    if (!_finish)
                    {
                        //run once
                        _finish = true;

                        //finish
                        if (_onFinish != null)
                        {
                            _onFinish();
                        }
                    }
                }
            }
        }

        public int CurrentTaskIndex => _currentIndex;

        public int TaskCount => _taskList.Count;

        public bool AutoCallNext { get; set; }
    }
}