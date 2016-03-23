using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Threading;

namespace SuperControl.ServiceModel
{
    public class ModelNotificationEvantArgs : EventArgs
    {
        public readonly ExcuteAction Action;

        public ModelNotificationEvantArgs(ExcuteAction action)
        {
            Action = action;
        }
    }

    public abstract class ModelNotification
    {
        public const int Port = 1083;
        private ObservableCollection<IPAddress> m_hosts;

        public Collection<IPAddress> RemoteHosts
        {
            get
            {
                if (m_hosts == null)
                    m_hosts = new ObservableCollection<IPAddress>();
                return m_hosts;
            }
        }

        public IPAddress LocalHost
        {
            get;
            set;
        }

        public abstract void NotifyChanged(params ExcuteAction[] actions);

        protected void OnNotify(ExcuteAction action)
        {
            if (NotifyReceived != null)
                NotifyReceived(this, new ModelNotificationEvantArgs(action));
        }

        public event EventHandler<ModelNotificationEvantArgs> NotifyReceived;
    }

    public class ModelUdpNotification : ModelNotification
    {
        private UdpClient m_udp;
        private IPEndPoint m_receivePoint;
        private AsyncCallback m_receiveCallback;
        private AsyncCallback m_sendCallback;

        public ModelUdpNotification()
        {
            m_receivePoint = new IPEndPoint(IPAddress.Any, Port);
            m_udp = new UdpClient(m_receivePoint);
            m_receiveCallback = new AsyncCallback(ReceiveCallback);
            m_sendCallback = new AsyncCallback(SendCallback);
            m_udp.BeginReceive(m_receiveCallback, null);
        }

        public override void NotifyChanged(params ExcuteAction[] actions)
        {
            try
            {
                lock (this)
                {
                    foreach (ExcuteAction action in actions)
                    {
                        MemoryStream ms = new MemoryStream();
                        action.Serialize(ms);
                        byte[] buffer = ms.ToArray();
                        foreach (IPAddress address in RemoteHosts)
                        {
                            m_udp.Connect(address, Port);
                            m_udp.BeginSend(buffer, buffer.Length, m_sendCallback, null);
                        }
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                Byte[] buffer = m_udp.EndReceive(ar, ref m_receivePoint);
                MemoryStream ms = new MemoryStream(buffer);
                ExcuteAction action = new ExcuteAction();
                action.Deserialize(ms);
                OnNotify(action);
                m_udp.BeginReceive(m_receiveCallback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            m_udp.EndSend(ar);
        }
    }

    public class ModelTcpNotification : ModelNotification, IDisposable
    {
        private TcpListener m_listener;
        private AsyncCallback m_acceptCallback;
        private AsyncCallback m_connectCallback;

        private struct SendContext
        {
            public TcpClient m_client;
            public ExcuteAction[] m_actions;
        }

        public ModelTcpNotification()
        {
            m_acceptCallback = new AsyncCallback(AcceptCallback);
            m_connectCallback = new AsyncCallback(ConnectCallback);
            if (LocalHost == null)
                m_listener = new TcpListener(Port);
            else
                m_listener = new TcpListener(LocalHost, Port);
            m_listener.Start();
            m_listener.BeginAcceptTcpClient(m_acceptCallback, null);
        }

        public override void NotifyChanged(params ExcuteAction[] actions)
        {
            try
            {
                foreach (IPAddress address in RemoteHosts)
                {
                    TcpClient client = new TcpClient();
                    SendContext context = new SendContext();
                    context.m_actions = actions;
                    context.m_client = client;
                    client.BeginConnect(address, Port, m_connectCallback, context);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            TcpClient client = m_listener.EndAcceptTcpClient(ar);
            m_listener.BeginAcceptTcpClient(m_acceptCallback, null);
            #region 用线程池接收
            //ThreadPool.QueueUserWorkItem(ReceiveCallback, client);
            #endregion

            #region 直接接收
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[sizeof(int)];
                stream.Read(buffer, 0, buffer.Length);
                int count = BitConverter.ToInt32(buffer, 0);
                while (count > 0)
                {
                    ExcuteAction action = new ExcuteAction();
                    action.Deserialize(stream);
                    OnNotify(action);
                    count--;
                }
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            #endregion
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                SendContext context = (SendContext)ar.AsyncState;
                context.m_client.EndConnect(ar);
                NetworkStream stream = context.m_client.GetStream();
                stream.WriteTimeout = 100;
                
                //写入对象数量
                byte[] buffer = BitConverter.GetBytes(context.m_actions.Length);
                stream.Write(buffer, 0, buffer.Length);
                //写入对象
                foreach (ExcuteAction action in context.m_actions)
                    action.Serialize(stream);
                stream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ReceiveCallback(Object threadContext)
        {
            try
            {
                TcpClient client = threadContext as TcpClient;
                NetworkStream stream = client.GetStream();
                while (stream.DataAvailable)
                {
                    ExcuteAction action = new ExcuteAction();
                    action.Deserialize(stream);
                    OnNotify(action);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        ~ModelTcpNotification()
        {
            Dispose();
        }

        public void Dispose()
        {        
            m_listener.Stop();
        }
    }
}
