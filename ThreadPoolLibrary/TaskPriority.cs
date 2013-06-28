using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreadPoolLibrary
{

    /// <summary>
    /// Приоритет выполнения задачи.
    /// </summary>
    public enum TaskPriority
    {

        /// <summary>
        /// Низкий приоритет.
        /// </summary>
        Low,
        /// <summary>
        /// Средний приоритет.
        /// </summary>
        Normal,
        /// <summary>
        /// Высокий приоритет.
        /// </summary>
        High

    }

}
