using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GammaCopy
{
    public sealed class Global
    {
        private static readonly Lazy<Global> lazy =
            new Lazy<Global>(() => new Global());
        public static Global Instance => lazy.Value;
        private Global() { }
        internal Dictionary<long, Result> _ResultsCurrentlyOpen = new Dictionary<long, Result>();
        internal static Dictionary<long, Result> ResultsCurrentlyOpen { get => Instance._ResultsCurrentlyOpen; set => Instance._ResultsCurrentlyOpen = value; }

        private long counter = 0;
        private readonly object consoleLock = new object();
        public static long Counter { get { return Instance.counter; } set { Instance.counter = value; } }

        public static void IncrementCounterAndDisplay()
        {
            //return;
            var r = Interlocked.Increment(ref Instance.counter);
            Task.Run(() =>
            {
                lock (Instance.consoleLock)
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"{r}");
                }
            });
           
        }
    }
}
