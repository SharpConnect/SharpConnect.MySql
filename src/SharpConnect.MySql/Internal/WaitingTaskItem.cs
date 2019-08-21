//MIT, 2015-2019, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
namespace SharpConnect.MySql.Internal
{
    class WaitingTask
    {
        Func<bool> _action;
        public WaitingTask(Func<bool> action)
        {
            _action = action;
        }
        public bool DoTask() => _action();
        //return true if finish 
    }

    static class CentralWaitingTasks
    {

        static Queue<WaitingTask> s_waitingQueue = new Queue<WaitingTask>();
        static bool s_working = false;
        static bool s_timerIsRunning = false;
        static System.Threading.Timer s_centralTimer;
        static object s_queueLock = new object();
        static CentralWaitingTasks()
        {
            //timer is stop
            s_centralTimer = new System.Threading.Timer(Timer_Tick, null,
                System.Threading.Timeout.Infinite,
                System.Threading.Timeout.Infinite);

        }
        static void Timer_Tick(object state)
        {
            if (s_working) { return; }
            s_working = true;
            //---------------------- 
            for (; ; )
            {
                //clear jobs
                WaitingTask waitingTask = null;
                lock (s_queueLock)
                {
                    if (s_waitingQueue.Count > 0)
                    {
                        waitingTask = s_waitingQueue.Dequeue();
                    }
                    else
                    {
                        //no item in queue
                        StopTimer();
                    }
                }
                //-------------
                if (waitingTask != null)
                {
                    //check state
                    if (!waitingTask.DoTask())
                    {
                        //if not finish
                        AddWaitingTask(waitingTask);
                    }
                }
                else
                {
                    break;
                }
            }

            s_working = false;
        }
        public static void AddWaitingTask(WaitingTask waitingTask)
        {
            lock (s_queueLock)
            {
                s_waitingQueue.Enqueue(waitingTask);
                if (!s_timerIsRunning)
                {
                    StartTimer();
                }
            }
        }
        static void StartTimer()
        {
            s_timerIsRunning = true;
            s_centralTimer.Change(0, 1);

        }
        static void StopTimer()
        {
            s_timerIsRunning = false;
            s_centralTimer.Change(
                   System.Threading.Timeout.Infinite,
                   System.Threading.Timeout.Infinite);

        }
    }

}