using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ThreadPoolLibrary;

namespace ConsoleApplication
{

    class Program
    {

        static void Main(string[] args)
        {
            #region Запускает 8 задач.

            FixedThreadPool pool = new FixedThreadPool(4);
            for (int i = 0; i < 8; i++)
            {
                Thread.Sleep(300);
                Guid g = Guid.NewGuid();
                bool added = pool.Execute(new Task(() =>
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("Task " + g.ToString() + "(" + DateTime.Now.Millisecond + ")");
                }));
                Console.WriteLine(i + ": " + added + "(" + g.ToString() + ")");
            }

            pool.Stop();
            Console.WriteLine();

            #endregion

            #region Запускает 8 задач с разными приоритетами.

            pool = new FixedThreadPool(4);

            pool.Execute(new Task(() => Console.WriteLine("Normal 1"), TaskPriority.Normal));
            pool.Execute(new Task(() => Console.WriteLine("High 1"), TaskPriority.High));
            pool.Execute(new Task(() => Console.WriteLine("Low 1"), TaskPriority.Low));
            pool.Execute(new Task(() => Console.WriteLine("High 2"), TaskPriority.High));
            Thread.Sleep(1000); Console.WriteLine("------------------------");
            pool.Execute(new Task(() => Console.WriteLine("Low 2"), TaskPriority.Low));
            pool.Execute(new Task(() => Console.WriteLine("Low 3"), TaskPriority.Low));
            pool.Execute(new Task(() => Console.WriteLine("High 3"), TaskPriority.High));
            pool.Execute(new Task(() => Console.WriteLine("High 4"), TaskPriority.High));

            pool.Stop();
            Console.WriteLine();

            #endregion

            #region Запускае 8 задач используя метод ExecuteRange.

            pool = new FixedThreadPool(4);

            Task[] tasks = new Task[]
            { 
                new Task(() => Console.WriteLine("Normal 1"), TaskPriority.Normal),
                new Task(() => Console.WriteLine("High 1"), TaskPriority.High),
                new Task(() => Console.WriteLine("Low 1"), TaskPriority.Low),
                new Task(() => Console.WriteLine("High 2"), TaskPriority.High)
            };
            pool.ExecuteRange(tasks);

            Thread.Sleep(1000); Console.WriteLine("------------------------");

            tasks = new Task[]
            {
                new Task(() => Console.WriteLine("Low 2"), TaskPriority.Low),
                new Task(() => Console.WriteLine("Low 3"), TaskPriority.Low),
                new Task(() => Console.WriteLine("High 3"), TaskPriority.High),
                new Task(() => Console.WriteLine("High 4"), TaskPriority.High)
            };
            pool.ExecuteRange(tasks);

            pool.Stop();

            #endregion
        }

    }

}
