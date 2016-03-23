using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

namespace SuperControl.ServiceModel
{
    internal class ModelAltKeyCollection<T> : KeyedCollection<string, T> where T : ModelBase
    {
        protected override string GetKeyForItem(T item)
        {
            return item.AlternateKey;
        }
    }

    public class ModelCollection<T> : KeyedCollection<int, T>, INotifyCollectionChanged, INotifyPropertyChanged where T : ModelBase
    {
        private int m_maxRid = 0;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public int MaxRid
        {
            get { return m_maxRid; }
        }

        protected override void ClearItems()
        {
            base.ClearItems();

            if (CollectionChanged != null)
            {
                NotifyCollectionChangedEventArgs arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                CollectionChanged(this, arg);
            }

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
        }

        protected override void InsertItem(int index, T item)
        {
            int i = BinarySearch(item.Rid);
            //防止带有两级节点的树节点删除异常
            try
            {
                base.InsertItem(i, item);
            }
            catch
            {
                Console.WriteLine("insert item error");
            }
            if (item.Rid > m_maxRid)
                m_maxRid = item.Rid;
            if (CollectionChanged != null)
            {
                NotifyCollectionChangedEventArgs arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, i);
                CollectionChanged(this, arg);
            }

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));     
        }

        protected override void RemoveItem(int index)
        {
            if (CollectionChanged != null)
            {
                T item = this.Items[index];

                base.RemoveItem(index);

                NotifyCollectionChangedEventArgs arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
                CollectionChanged(this, arg);
            }
            else base.RemoveItem(index);

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
        }

        protected override void SetItem(int index, T item)
        {
            if (CollectionChanged != null)
            {
                T oldItem = this.Items[index];

                int i = BinarySearch(item.Rid);

                base.RemoveItem(index);
                base.InsertItem(i, item);
                if (item.Rid > m_maxRid)
                    m_maxRid = item.Rid;

                NotifyCollectionChangedEventArgs arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, i);
                CollectionChanged(this, arg);
            }
            else base.SetItem(index, item);
        }

        protected override int GetKeyForItem(T item)
        {
            return item.Rid;
        }

        private int BinarySearch(int key)
        {
            int low = 0;
            int high = this.Count - 1;
            if (this.Count == 0)
                return 0;
            if (key < this.Items[low].Rid)
                return low;
            if (key > this.Items[high].Rid)
                return high + 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                if (key < this.Items[mid].Rid)
                {
                    high = mid - 1;
                    if (high < 0)
                        return 0;
                    else if (key > this.Items[high].Rid)
                        return mid;
                }
                else
                {
                    low = mid + 1;
                    if (low >= this.Count)
                        return this.Count;
                    else if (key < this.Items[low].Rid)
                        return low;
                }
            }
            return 0;
        }

        public void FindDifference(ModelCollection<T> target, out T[] notInTarget, out T[] notInThis, out T[] diffInTarget)
        {
            if (target.Count == 0 || this.Count == 0)
            {
                notInTarget = this.ToArray();
                notInThis = target.ToArray();
                diffInTarget = new T[0];
                return;
            }
            List<T> thisList = new List<T>();
            List<T> targetList = new List<T>();
            List<T> diffList = new List<T>();
            int indexThis = 0;
            int indexTarget = 0;
            while (indexThis < this.Count && indexTarget < target.Count)
            {
                T itemThis = this.Items[indexThis];
                T itemTarget =  target.Items[indexTarget];
                if (itemThis.Rid < itemTarget.Rid)
                {
                    thisList.Add(itemThis);
                    indexThis++;
                }
                else if (itemThis.Rid > itemTarget.Rid)
                {
                    targetList.Add(itemTarget);
                    indexTarget++;
                }
                else
                {
                    indexThis++;
                    indexTarget++;
                    foreach (PropertyInfo property in typeof(T).GetProperties())
                    {
                        if (property.IsDefined(typeof(DataMemberAttribute), false))
                        {
                            object obj1 = property.GetValue(itemThis, null);
                            object obj2 = property.GetValue(itemTarget, null);
                            if (obj1 == null && obj2 == null)
                                continue;
                            else if (obj1 != null && !obj1.Equals(obj2))
                                diffList.Add(itemTarget);
                        }
                    }
                }
            }
            for (int i = indexThis; i < this.Count; i++)
            {
                thisList.Add(this.Items[i]);
            }
            for (int i = indexTarget; i < target.Count; i++)
            {
                targetList.Add(target.Items[i]);
            }
            notInTarget = thisList.ToArray();
            notInThis = targetList.ToArray();
            diffInTarget = diffList.ToArray();
        }
    }

    public class ModelCollection : ModelCollection<ModelBase>
    {
        private ModelAltKeyCollection<ModelBase> m_altCollection;

        internal void AddAltKey(ModelBase item)
        {
            if (m_altCollection == null)
                return;
            if (string.IsNullOrWhiteSpace(item.AlternateKey))
                return;
            if (m_altCollection.Contains(item.AlternateKey))
                return;
            m_altCollection.Add(item);
        }

        internal void RemoveAltKey(ModelBase item)
        {
            if (m_altCollection == null)
                return;
            if (string.IsNullOrWhiteSpace(item.AlternateKey))
                return;
            if (m_altCollection.Contains(item.AlternateKey))
                m_altCollection.Remove(item.AlternateKey);
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            if (m_altCollection == null)
                return;
            m_altCollection.Clear();
        }

        private Type m_type = null;
        internal Type ModelType
        {
            get
            {
                return m_type == null ? typeof(ModelBase) : m_type;
            }
            set
            {
                if (value.IsSubclassOf(typeof(ModelBase)))
                {
                    m_type = value;
                    m_field = new ModelMapping(m_type);
                    DbTableAttribute attribute = Attribute.GetCustomAttribute(m_type, typeof(DbTableAttribute)) as DbTableAttribute;
                    if (attribute != null && attribute.HasAlternateKey)
                    {
                        m_altCollection = new ModelAltKeyCollection<ModelBase>();
                    }
                }
            }
        }

        public ModelBase this[string altKey]
        {
            get
            {
                if (m_altCollection != null)
                {
                    if (m_altCollection.Contains(altKey))
                        return m_altCollection[altKey];
                }
                else if (BaseCollection != null)
                {
                    return BaseCollection[altKey];
                }
                return null;
            }
        }

        public bool Contains(string altKey)
        {
            return m_altCollection.Contains(altKey);
        }

        private ModelMapping m_field;
        internal ModelMapping ModelField
        {
            get
            {
                return m_field;
            }
        }

        internal ModelCollection BaseCollection
        {
            get;
            set;
        }

        public T[] ToArray<T>() where T : ModelBase
        {
            List<T> list = new List<T>();
            lock (ModelFactoryCollection.s_locker)
            {
                foreach (ModelBase model in this)
                {
                    T item = model as T;
                    if (item != null)
                        list.Add(item);
                }
            }
            return list.ToArray();
        }

        public T[] ToArray<T>(int startRid, int count) where T : ModelBase
        {
            ModelBase model = null;
            List<T> list = new List<T>();
            lock (ModelFactoryCollection.s_locker)
            {
                while (startRid <= MaxRid)
                {
                    if (Contains(startRid))
                    {
                        model = this[startRid];
                        break;
                    }
                    startRid++;
                }
                if (model != null)
                {
                    int index = this.IndexOf(model);
                    while (count > 0)
                    {
                        T item = Items[index] as T;
                        if (item != null)
                        {
                            list.Add(item);
                            count--;
                        }
                        index++;
                        if (index >= Count)
                            break;
                    }
                }
            }
            return list.ToArray();
        }
    }
}
