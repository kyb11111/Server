using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SuperControl.ServiceModel
{
    [Serializable, DataContract]
    public class RealtimeData
    {
        private List<object> m_data = new List<object>();

        [DataMember]
        public int Rid
        {
            get;
            set;
        }

        [DataMember]
        public object[] Data
        {
            get { return m_data.ToArray(); }
        }

        public void AddData(params object[] data)
        {
            m_data.AddRange(data);
        }

        public override bool Equals(object obj)
        {
            RealtimeData data = obj as RealtimeData;
            if (data != null)
            {
                if (this.Rid != data.Rid)
                    return false;

                if (this.Data.Length == data.Data.Length)
                {
                    for (int i = 0; i < this.m_data.Count; i++)
                    {
                        if (!this.m_data[i].Equals(data.m_data[i]))
                            return false;
                    }
                    return true;
                }
                return false;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class RealtimeDataCache
    {
        public delegate void SendCallback(string modelType, RealtimeData[] data);

        private class TypeSetPair
        {
            public Type m_type;
            public HashSet<ModelBase> m_set;
        }

        private List<TypeSetPair> m_list = new List<TypeSetPair>();
        private SendCallback m_callback;

        public RealtimeDataCache()
        {
        }

        public RealtimeDataCache(SendCallback callback)
        {
            SetNotifyCallback(callback);
        }

        public void SetNotifyCallback(SendCallback callback)
        {
            m_callback = callback;
        }

        public void SendNotify()
        {
            if (m_callback == null)
                return;

            lock (this)
            {
                foreach (TypeSetPair p in m_list)
                {
                    List<RealtimeData> sendList = new List<RealtimeData>();
                    foreach (ModelBase model in p.m_set)
                    {
                        RealtimeData rtd = model.PrepareRealtimeData();
                        if (rtd != null)
                            sendList.Add(rtd);
                    }
                    if (sendList.Count > 0)
                    {
                        int index = 0;
                        while (index < sendList.Count)
                        {
                            int length = Math.Min(SystemConfig.MaxArrayLength, sendList.Count - index);
                            RealtimeData[] array = new RealtimeData[length];
                            sendList.CopyTo(index, array, 0, array.Length);
                            m_callback(p.m_type.Name, array);
                            index += SystemConfig.MaxArrayLength;
                        }
                    }
                }
            }
        }

        public bool Add(ModelBase model)
        {
            if (model == null)
                return false;
            lock (this)
            {
                TypeSetPair pair = null;
                Type type = model.GetType();
                foreach (TypeSetPair p in m_list)
                {
                    if (p.m_type == type)
                    {
                        pair = p;
                        break;
                    }
                }
                if (pair == null)
                {
                    pair = new TypeSetPair();
                    pair.m_type = type;
                    pair.m_set = new HashSet<ModelBase>();
                    m_list.Add(pair);
                }
                return pair.m_set.Add(model);
            }
        }

        public bool Remove(ModelBase model)
        {
            if (model == null)
                return false;
            lock (this)
            {
                Type type = model.GetType();
                foreach (TypeSetPair p in m_list)
                {
                    if (p.m_type == type)
                    {
                        return p.m_set.Remove(model);
                    }
                }
                return true;
            }
        }
    }
}
