using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SuperControl.ServiceModel
{
    /// <summary>
    /// 掉线监视器
    /// </summary>
    public class ClientMonitor : IDisposable
    {
        private static ClientMonitor s_instance;
        /// <summary>
        /// 获取掉线监视器的实例
        /// </summary>
        public static ClientMonitor Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new ClientMonitor();
                return s_instance;
            }
        }

        private List<IClientSession> m_clientList = new List<IClientSession>();
        private Thread m_thread;
        private bool m_loop = true;
        private int m_interval = 1000;
        private ClientMonitor()
        {
            m_thread = new Thread(new ThreadStart(Loop));
            m_thread.Start();
        }

        private void Loop()
        {
            while (m_loop)
            {
                List<IClientSession> removeList = new List<IClientSession>();
                //检测所有客户端在线状态
                foreach (IClientSession client in m_clientList)
                {
                    try
                    {
                        client.OnPing();
                    }
                    catch (Exception ex)
                    {
                        removeList.Add(client);
                        Console.WriteLine(ex.Message);
                    }
                }
                //删除所有掉线的客户端,并通知掉线客户端
                foreach (IClientSession client in removeList)
                {
                    m_clientList.Remove(client);
                    client.OnDisconnected();
                }
                //通知其他在线客户端有哪些掉线的客户端
                foreach (IClientSession client in m_clientList)
                {
                    foreach (IClientSession rc in removeList)
                    {
                        client.OnDisconnected(rc);
                    }
                }
                Thread.Sleep(m_interval);
            }
        }

        /// <summary>
        /// 检测间隔
        /// </summary>
        public int Interval
        {
            get { return m_interval; }
            set
            {
                if (value <= 1000)
                    m_interval = 1000;
            }
        }

        /// <summary>
        /// 加入客户端
        /// </summary>
        /// <param name="client">客户端</param>
        public void AddClient(IClientSession client)
        {
            lock (m_clientList)
            {
                if (!m_clientList.Contains(client))
                    m_clientList.Add(client);
            }
        }

        /// <summary>
        /// 删除客户端
        /// </summary>
        /// <param name="client">客户端</param>
        public void RemoveClient(IClientSession client)
        {
            lock (m_clientList)
            {
                m_clientList.Remove(client);
            }
        }

        /// <summary>
        /// 当前保存的所有客户端
        /// </summary>
        public IClientSession[] Clients
        {
            get
            {
                return m_clientList.ToArray();
            }
        }

        /// <summary>
        /// 停止检测线程
        /// </summary>
        public void Dispose()
        {
            m_loop = false;
            m_thread.Abort();
        }
    }
}
