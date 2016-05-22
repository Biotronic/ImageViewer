using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ImageViewer
{
    internal class TaskList
    {
        private static readonly HashSet<Tuple<Task,string>> _tasks = new HashSet<Tuple<Task, string>>();

        public static Task StartTask(Action action, [CallerMemberName]string name = "")
        {
            Tuple<Task, string>[] tsk = { null };
            tsk[0] = Tuple.Create(Task.Factory.StartNew(() =>
            {
                try
                {
                    action();
                }
                finally
                {
                    _tasks.Remove(tsk[0]);
                }
            }), name);
            _tasks.Add(tsk[0]);
            return tsk[0].Item1;
        }

        public static bool Empty => !_tasks.Any();
        public static bool Closing { get; private set; }

        public static void Close()
        {
            Closing = true;
            while (_tasks.Any())
            {
                _tasks.First().Item1.Wait();
            }
        }
    }
}
