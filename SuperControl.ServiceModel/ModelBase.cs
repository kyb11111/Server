using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Reflection;
using System.ComponentModel;

namespace SuperControl.ServiceModel
{
    [Serializable, DataContract]
    public class ModelBase : INotifyPropertyChanged
    {
        [DataMember(IsRequired = true), DbField]
        public int Rid
        {
            get;
            set;
        }

        private RealtimeData m_oldCopy = new RealtimeData();
        public RealtimeData PrepareRealtimeData()
        {
            RealtimeData data = new RealtimeData();
            data.Rid = this.Rid;
            GetRealtimeData(data);
            if (m_oldCopy == data)
                return null;
            m_oldCopy = data;
            return data;
        }

        protected virtual void GetRealtimeData(RealtimeData data)
        {
        }

        public virtual string AlternateKey
        {
            get
            {
                return string.Empty;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 通过实时数据库的时间写入当前存根管理中
        /// </summary>
        /// <param name="fieldName">数据库变更的字段</param>
        /// <param name="value">变更的值</param>
        /// <param name="sendImmediately">是否立即发送事件到客户端</param>
        public virtual void SetValueWithTableName(string fieldName, object value, out bool sendImmediately)
        {
            sendImmediately = false;
        }

        #region 关联属性
        private ParentModel parent;
        private ChildrenModel children;

        /// <summary>
        /// 获取多对一关联中的关联父对象
        /// </summary>
        public ParentModel ParentModel
        {
            get
            {
                if (parent == null)
                    parent = new ParentModel(this);
                return parent;
            }
        }

        /// <summary>
        /// 获取一对多关联中的子对象集合
        /// </summary>
        public ChildrenModel ChildrenModel
        {
            get
            {
                if (children == null)
                    children = new ChildrenModel(this);
                return children;
            }
        }

        /// <summary>
        /// 获取一对多关联中的子对象集合
        /// </summary>
        /// <typeparam name="T">关联子对象类型</typeparam>
        /// <returns>关联子对象集合</returns>
        public IEnumerable<T> GetChildren<T>() where T : ModelBase
        {
            return ChildrenModel.GetChildren<T>();
        }

        /// <summary>
        /// 获取一对多关联中的子对象集合
        /// </summary>
        /// <typeparam name="T">关联子对象类型</typeparam>
        /// <param name="childPropertyName">关联子对象对应的成员属性名称</param>
        /// <returns>关联子对象集合</returns>
        public IEnumerable<T> GetChildren<T>(string childPropertyName) where T : ModelBase
        {
            return ChildrenModel.GetChildren<T>(childPropertyName);
        }

        /// <summary>
        /// 获取多对一关联中的关联父对象
        /// </summary>
        /// <typeparam name="T">关联父对象类型</typeparam>
        /// <returns>关联父对象</returns>
        public T GetParent<T>() where T : ModelBase
        {
            return ParentModel.GetParent<T>();
        }

        /// <summary>
        /// 获取多对一关联中的关联父对象
        /// </summary>
        /// <typeparam name="T">关联父对象类型</typeparam>
        /// <param name="parentPropertyName">关联父对象对应的成员属性名称</param>
        /// <returns>关联父对象</returns>
        public T GetParent<T>(string parentPropertyName) where T : ModelBase
        {
            return ParentModel.GetParent<T>(parentPropertyName);
        }


        /// <summary>
        /// 根据类标签，获得所有子对象
        /// </summary>
        /// <returns>所有子对象</returns>
        public ModelBase[] GetAllChildren()
        {
            List<ModelBase> list = new List<ModelBase>();
            AddChildrenToList(this, list);
            return list.ToArray();
        }

        internal static void AddChildrenToList(ModelBase parentModel,List<ModelBase> targetList)
        {
            if (parentModel == null)
                return;
            Type type = parentModel.GetType();
            ChildrenModelAttribute[] childrenAttributes = Attribute.GetCustomAttributes(type, typeof(ChildrenModelAttribute)) as ChildrenModelAttribute[];
            foreach (ChildrenModelAttribute childrenAttribute in childrenAttributes)
            {
                ModelCollection rootCollection = ModelCacheManager.Instance[childrenAttribute.m_childType];
                if (rootCollection == null)
                    return;

                string protertyName = string.IsNullOrWhiteSpace(childrenAttribute.m_protertyName) ?
                    parentModel.GetType().Name : childrenAttribute.m_protertyName;

                PropertyInfo property = childrenAttribute.m_childType.GetProperty(protertyName);
                if (property == null || property.PropertyType != typeof(int))
                    return;

                foreach (ModelBase child in rootCollection)
                {
                    int rid = (int)property.GetValue(child, null);
                    if (rid == parentModel.Rid)
                    {
                        targetList.Add(child);
                        AddChildrenToList(child, targetList);
                    }
                }
            }
        }
        #endregion
    }
}
