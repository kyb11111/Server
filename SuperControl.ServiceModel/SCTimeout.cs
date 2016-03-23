using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace SuperControl.ServiceModel
{
    public enum SCTimeoutAction
    {
        KillDispatch,
        Exit,
        ThrowException,
    }

    public class SCTimeout
    {
        private bool m_stop = false;
        private SCTimeoutAction m_action = SCTimeoutAction.ThrowException;
        private int m_timeout = 1000;

        public SCTimeout()
        {
        }

        public SCTimeout(SCTimeoutAction action)
        {
            m_action = action;
        }

        public void Wait(int timeout)
        {
            Thread thread = new Thread(new ThreadStart(Proc));
            thread.Start();
            m_timeout = timeout;
        }

        public void Stop()
        {
            m_stop = true;
        }

        private void Proc()
        {
            int timeout = m_timeout;
            while (true)
            {
                if (m_stop || timeout < 0)
                {
                    break;
                }
                timeout--;
                Thread.Sleep(1);
            }
            if (!m_stop)
            {
                switch (m_action)
                {
                    case SCTimeoutAction.KillDispatch:
                        StreamWriter sw = File.AppendText("clear.log");
                        sw.AutoFlush = true;
                        sw.WriteLine(DateTime.Now);
                        foreach (Process p in Process.GetProcessesByName("SCDBServer"))
                        {
                            try
                            {
                                p.Kill();
                                Console.WriteLine("kill SCDBServer {0}", p.Id);
                                sw.WriteLine(string.Format("因为超时所以停止SCDBServer {0}", p.Id));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                        break;
                    case SCTimeoutAction.Exit:
                        Environment.Exit(-1);
                        break;
                    default:
                        throw new TimeoutException(string.Format("{0}超时",this.GetType()));
                        break;
                }
            }
        }
    }
}
