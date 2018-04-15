//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
namespace SharpConnect.MySql.Internal
{
    class WaitingTask
    {
        Func<bool> action;
        public WaitingTask(Func<bool> action)
        {
            this.action = action;
        }
        public bool DoTask()
        {
            //return true if finish
            return action();
        }
    }

    static class CentralWaitingTasks
    {

        static Queue<WaitingTask> waitingQueue = new Queue<WaitingTask>();
        static bool working = false;
        static bool timerIsRunning = false;
        static System.Threading.Timer centralTimer;
        static object queueLock = new object();
        static CentralWaitingTasks()
        {
            //timer is stop
            centralTimer = new System.Threading.Timer(Timer_Tick, null,
                System.Threading.Timeout.Infinite,
                System.Threading.Timeout.Infinite);

        }
        static void Timer_Tick(object state)
        {
            if (working) { return; }
            working = true;
            //---------------------- 
            for (;;)
            {
                //clear jobs
                WaitingTask waitingTask = null;
                lock (queueLock)
                {
                    if (waitingQueue.Count > 0)
                    {
                        waitingTask = waitingQueue.Dequeue();
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

            working = false;
        }
        public static void AddWaitingTask(WaitingTask waitingTask)
        {
            lock (queueLock)
            {
                waitingQueue.Enqueue(waitingTask);
                if (!timerIsRunning)
                {
                    StartTimer();
                }
            }
        }
        static void StartTimer()
        {
            timerIsRunning = true;
            centralTimer.Change(0, 1);

        }
        static void StopTimer()
        {
            timerIsRunning = false;
            centralTimer.Change(
                   System.Threading.Timeout.Infinite,
                   System.Threading.Timeout.Infinite);

        }
    }

}