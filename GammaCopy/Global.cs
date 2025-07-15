using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

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

        private readonly object consoleLock = new object();
        private int barOrigin = 0;
        //private long barIdCursor = 0;




        private long _barIdCursor = 0;
        public static long ProgressBarCount { get { return Instance._barIdCursor; } }
        public static long IncrementProgressBarId()
        {
            var r = Interlocked.Increment(ref Instance._barIdCursor);
            //if (Console.BufferHeight <= r)
            //{
            //    Console.BufferHeight = (int)(r + 1);
            //}
            return r;
        }





        internal class Bar
        {
            public bool Active { get; set; }
            public int offset { get; set; }
        }
        private ConcurrentDictionary<long, Bar> Bars = new ConcurrentDictionary<long, Bar>();
        private List<Bar> ActiveBars { get { return Bars.Values.Where(k => k.Active).ToList(); } }
        private int NextBarOffset
        {
            get
            {
                var p = 0;
                var activ = ActiveBars;
                for (int i = 0; i < int.MaxValue; i++)
                {
                    if (activ.FirstOrDefault(k => k.offset == i) == null)
                        return i;
                }
                return 0;
                //return ActiveBars.DefaultIfEmpty(new Bar() { offset = 0 }).Max(k => k.offset) + 1;
            }
        }
        public static void BarWrite(long barId, string what)
        {
            StringBuilder sb = new StringBuilder(what);
            BarWrite(barId, sb);
        }
        public static void BarWrite(long barId, StringBuilder what)
        {
            lock (Instance.consoleLock)
            {
                if (!Instance.Bars.ContainsKey(barId))
                {
                    throw new Exception("no such bar");
                }
                var bar = Instance.Bars[barId];
                if (!bar.Active)
                {
                    throw new Exception("bar already destroyed");
                }
                if (Console.BufferHeight < Instance.barOrigin + bar.offset + 1)
                {
                    Console.BufferHeight = Instance.barOrigin + bar.offset + 1;
                }
                Console.SetCursorPosition(0, Instance.barOrigin + bar.offset);
                Console.Write(what);
            }
        }
        public static long CreateBar()
        {
            lock (Instance.consoleLock)
            {
                long id = IncrementProgressBarId();
                var activeBars = Instance.ActiveBars;
                if (activeBars.Count == 0)
                {
                    Instance.barOrigin = Console.CursorTop;
                }
                Instance.Bars[id] = new Bar() { Active = true, offset = Instance.NextBarOffset };
                return id;
            }
        }
        public static void DestroyBar(long barId)
        {
            lock (Instance.consoleLock)
            {
                if (!Instance.Bars.ContainsKey(barId))
                {
                    throw new Exception("no such bar");
                }
                if (!Instance.Bars[barId].Active)
                {
                    throw new Exception("bar already destroyed");
                }
                Instance.Bars[barId].Active = false;
                if (Instance.ActiveBars.Count == 0)
                {
                    Console.SetCursorPosition(0, Instance.barOrigin);
                }
            }
        }
    }
}
