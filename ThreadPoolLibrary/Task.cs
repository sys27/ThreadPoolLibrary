using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreadPoolLibrary
{

    /// <summary>
    /// Класс представляет задачу для выполнения в <see cref="FixedThreadPool"/>
    /// </summary>
    public class Task
    {

        private TaskPriority priority;
        private Action work;
        private bool isRunned;

        /// <summary>
        /// Создает задачу с указанным приоритетом.
        /// </summary>
        /// <param name="work">Делегат содержащий метода для задачи.</param>
        public Task(Action work) : this(work, TaskPriority.Normal) { }

        /// <summary>
        /// Создает задачу с указанным приоритетом.
        /// </summary>
        /// <param name="work">Делегат содержащий метода для задачи.</param>
        /// <param name="priority">Приоритет задачи.</param>
        public Task(Action work, TaskPriority priority)
        {
            this.priority = priority;
            this.work = work;
        }

        /// <summary>
        /// Запускает задачу.
        /// </summary>
        public void Execute()
        {
            lock (this)
            {
                isRunned = true;
            }
            work();
        }

        /// <summary>
        /// Приоритет задачи. Устанавливается только при создании задачи.
        /// </summary>
        public TaskPriority Priority
        {
            get
            {
                return priority;
            }
        }

        /// <summary>
        /// Запущена ли задача. (True - запущена, False - стоит в очереди на выполнение)
        /// </summary>
        public bool IsRunned
        {
            get
            {
                return isRunned;
            }
        }

    }

}
