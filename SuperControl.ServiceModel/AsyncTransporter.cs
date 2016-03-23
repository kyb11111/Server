using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace SuperControl.ServiceModel
{
    public abstract class AsyncTransporter
    {
        #region 静态变量和方法
        private static Timer s_timer = new Timer();
        private static List<AsyncTransporter> s_transporterList = new List<AsyncTransporter>();

        protected List<AsyncTransporter> AsyncTransporters
        {
            get { return s_transporterList; }
        }

        public static void Start()
        {
            s_timer.Interval = SystemConfig.ExcuteActionTransportInterval;
            s_timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
            s_timer.Start();
        }

        static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (AsyncTransporter transporter in s_transporterList)
            {
                transporter.SendAction();
            }
        }
        #endregion

        public abstract void SendAction();
    }

    public class AsyncTransporter<T> : AsyncTransporter, IDisposable
    {
        public delegate void SendCallback(IEnumerable<T> actions);

        private Queue<T> m_queue = new Queue<T>();
        private SendCallback m_callback;

        public AsyncTransporter(SendCallback callback)
        {
            m_callback = callback;
            AsyncTransporters.Add(this);
        }

        public void Enqueue(T value)
        {
            m_queue.Enqueue(value);
        }

        public override void SendAction()
        {
            int count = Math.Min(m_queue.Count, SystemConfig.MaxArrayLength);
            if (count == 0)
                return;
            T[] sendArray = new T[count];
            for (int i = 0; i < count; i++)
            {
                sendArray[i] = m_queue.Dequeue();
            }
            if (m_callback != null)
                m_callback(sendArray);
        }

        public void Dispose()
        {
            AsyncTransporters.Remove(this);
        }

        ~AsyncTransporter()
        {
            Dispose();
        }
    }
}
