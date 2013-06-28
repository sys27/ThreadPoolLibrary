using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using TPL = System.Threading.Tasks;

namespace ThreadPoolLibrary
{

    /// <summary>
    /// Пул потоков. В нем задачи запускаются по приоритетам. Если количество потоков больше или равно 4, то на каждые 3 задачи с высоким приоритетом - будет запущена задача с нормальным приоритетом. Если количество потоков меньше 4, тогда задачи выполняются прямо следуя приоритетам. Задачи с низким приоритетом выполняются в последнюю очередь.
    /// </summary>
    public class FixedThreadPool : IDisposable
    {

        private int numberThreads;

        private ManualResetEvent stopEvent;
        private bool isStoping;
        private object stopLock;

        private Dictionary<int, ManualResetEvent> threadsEvent;
        private Thread[] threads;
        private List<Task> tasks;

        private ManualResetEvent scheduleEvent;
        private Thread scheduleThread;

        private bool isDisposed;

        /// <summary>
        /// Создает пул потоков с количеством потоков равным количеству ядер процессора.
        /// </summary>
        public FixedThreadPool() : this(Environment.ProcessorCount) { }

        /// <summary>
        /// Создает пул потоков с указанным количеством потоков.
        /// </summary>
        /// <param name="numberThreads">Количество поток.</param>
        public FixedThreadPool(int numberThreads)
        {
            if (numberThreads <= 0)
                throw new ArgumentException("numberThreads", "Количество потоков должно быть больше нуля.");

            this.numberThreads = numberThreads;

            this.stopLock = new object();
            this.stopEvent = new ManualResetEvent(false);

            this.scheduleEvent = new ManualResetEvent(false);
            this.scheduleThread = new Thread(SelectAndStartFreeThread) { Name = "Schedule Thread", IsBackground = true };
            scheduleThread.Start();

            this.threads = new Thread[numberThreads];
            this.threadsEvent = new Dictionary<int, ManualResetEvent>(numberThreads);

            for (int i = 0; i < numberThreads; i++)
            {
                threads[i] = new Thread(ThreadWork) { Name = "Pool Thread", IsBackground = true };
                threadsEvent.Add(threads[i].ManagedThreadId, new ManualResetEvent(false));

                threads[i].Start();
            }

            this.tasks = new List<Task>();
        }

        /// <summary>
        /// Прерывает выполнение всех потоков, не дожидаясь их завершения и уничтожает за собой все ресурсы.
        /// </summary>
        ~FixedThreadPool()
        {
            Dispose(false);
        }

        /// <summary>
        /// Высвобождает ресурсы, которые используются пулом потоков.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Высвобождает ресурсы, которые используются пулом потоков.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    scheduleThread.Abort();
                    scheduleEvent.Dispose();

                    for (int i = 0; i < numberThreads; i++)
                    {
                        threads[i].Abort();
                        threadsEvent[threads[i].ManagedThreadId].Dispose();
                    }
                }

                isDisposed = true;
            }
        }

        private Task SelectTask()
        {
            lock (tasks)
            {
                if (tasks.Count == 0)
                    throw new ArgumentException();

                var waitingTasks = tasks.Where(t => !t.IsRunned);
                var highTasks = waitingTasks.Where(t => t.Priority == TaskPriority.High);
                var normalTasks = waitingTasks.Where(t => t.Priority == TaskPriority.Normal);

                if (highTasks.Count() > 0)
                {
                    if (numberThreads >= 4)
                    {
                        var runnedHighTasks = tasks.Where(t => t.IsRunned && t.Priority == TaskPriority.High);
                        var runnedNormalTasks = tasks.Where(t => t.IsRunned && t.Priority == TaskPriority.Normal);

                        if (runnedHighTasks.Count() / (runnedNormalTasks.Count() + 1) < 3)
                        {
                            return highTasks.First();
                        }
                        else
                        {
                            return normalTasks.First();
                        }
                    }
                    else
                    {
                        return highTasks.First();
                    }
                }
                else
                {
                    if (normalTasks.Count() > 0)
                    {
                        return normalTasks.First();
                    }
                    else
                    {
                        var lowTasks = tasks.Where(t => t.Priority == TaskPriority.Low).ToArray();
                        return lowTasks.FirstOrDefault();
                    }
                }
            }
        }

        private void ThreadWork()
        {
            while (true)
            {
                threadsEvent[Thread.CurrentThread.ManagedThreadId].WaitOne();

                Task task = SelectTask();
                if (task != null)
                {
                    try
                    {
                        task.Execute();
                    }
                    finally
                    {
                        RemoveTask(task);
                        if (isStoping)
                            stopEvent.Set();
                        threadsEvent[Thread.CurrentThread.ManagedThreadId].Reset();
                    }
                }
            }
        }

        private void SelectAndStartFreeThread()
        {
            while (true)
            {
                scheduleEvent.WaitOne();
                lock (threads)
                {
                    foreach (var thread in threads)
                    {
                        if (threadsEvent[thread.ManagedThreadId].WaitOne(0) == false)
                        {
                            threadsEvent[thread.ManagedThreadId].Set();
                            break;
                        }
                    }
                }

                scheduleEvent.Reset();
            }
        }

        private void AddTask(Task task)
        {
            lock (tasks)
            {
                tasks.Add(task);
            }

            scheduleEvent.Set();
        }

        private void RemoveTask(Task task)
        {
            lock (tasks)
            {
                tasks.Remove(task);
            }

            if (tasks.Count > 0 && tasks.Where(t => !t.IsRunned).Count() > 0)
            {
                scheduleEvent.Set();
            }
        }

        /// <summary>
        /// Ставит задачу в очередь.
        /// </summary>
        /// <param name="task">Задача.</param>
        /// <returns>Возвращает значание удалось ли поставить задачу в очередь.</returns>
        public bool Execute(Task task)
        {
            if (task == null)
                throw new ArgumentNullException("task", "The Task can't be null.");

            lock (stopLock)
            {
                if (isStoping)
                {
                    return false;
                }

                AddTask(task);
                return true;
            }
        }

        /// <summary>
        /// Ставить несколько задачь в очередь.
        /// </summary>
        /// <param name="tasks">Массив задач.</param>
        /// <returns>Возвращает False, если хотя бы одну задачу не удалось установить.</returns>
        public bool ExecuteRange(IEnumerable<Task> tasks)
        {
            bool result = true;
            foreach (var task in tasks)
            {
                if (!Execute(task))
                    result = false;
            }

            return result;
        }

        /// <summary>
        /// Останавливает работу пула потоков. Ожидает завершения всех задач (запущенных и стоящих в очереди) и уничтожает все ресурсы.
        /// </summary>
        public void Stop()
        {
            lock (stopLock)
            {
                isStoping = true;
            }

            while (tasks.Count > 0)
            {
                stopEvent.WaitOne();
                stopEvent.Reset();
            }

            Dispose(true);
        }

    }

}
