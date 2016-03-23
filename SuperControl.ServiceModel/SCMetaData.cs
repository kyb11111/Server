using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SuperControl.ServiceModel
{
    [DataContract]
    public class SCMetaData : ICloneable
    {
        #region prop

        //字段列表
        private List<SCField> m_fieldsList = new List<SCField>();
        //名称-字段缓存
        private Dictionary<string, SCField> m_fieldCache = null;
        private object m_lockObject = new object();

        /// <summary>
        /// 字段列表
        /// </summary>
        [DataMember]
        public List<SCField> Fields
        {
            get { return m_fieldsList; }
            set { m_fieldsList = value; }
        }

        #endregion

        #region public

        /// <summary>
        /// 根据名称获得字段
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public SCField GetField(string fieldName)
        {
            if (String.IsNullOrEmpty(fieldName))
                return null;
            return GetNameCache()[fieldName];
        }

        /// <summary>
        /// 添加字段
        /// </summary>
        /// <param name="fields"></param>
        public void AddField(SCField[] fields)
        {
            if (fields != null && fields.Length > 0)
            {
                foreach (SCField f in fields)
                {
                    if (f == null || String.IsNullOrEmpty(f.FiledName) || m_fieldsList.Contains(f))
                        continue;
                    m_fieldsList.Add(f);
                }
                ClearCache();
            }
        }

        /// <summary>
        /// 添加字段
        /// </summary>
        /// <param name="field"></param>
        public void AddField(SCField field)
        {
            if (field == null || m_fieldsList.Contains(field))
                return;
            m_fieldsList.Add(field);
            ClearCache();
        }

        /// <summary>
        /// 获得字段在列表中的索引位置
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public int GetIndex(SCField field)
        {
            if (field == null)
                return -1;
            return m_fieldsList.IndexOf(field);
        }

        /// <summary>
        /// 根据字段名称获得索引
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public int GetIndex(string fieldName)
        {
            if (String.IsNullOrEmpty(fieldName))
                return -1;
            SCField f = GetField(fieldName);
            return m_fieldsList.IndexOf(f);
        }

        /// <summary>
        /// 从字段列表中删除字段
        /// </summary>
        /// <param name="fieldName"></param>
        public void DropField(string fieldName)
        {
            SCField field = GetField(fieldName);
            if (field != null)
            {
                m_fieldsList.Remove(field);
                ClearCache();
            }
        }

        /// <summary>
        /// 获得字段名称列表
        /// </summary>
        /// <returns></returns>
        public string[] GetFieldNames()
        {
            string[] names = new string[m_fieldsList.Count];
            for (int i = 0; i < m_fieldsList.Count; i++)
            {
                names[i] = m_fieldsList.ElementAt<SCField>(i).FiledName;
            }
            return names;
        }

        /// <summary>
        /// 获得字段数组
        /// </summary>
        /// <returns></returns>
        public SCField[] GetFields()
        {
            return GetNameCache().Values.ToArray();
        }



        /// <summary>
        /// 清除字段列表
        /// </summary>
        public void Clear()
        {
            m_fieldsList.Clear();
            ClearCache();
        }

        /// <summary>
        /// 更新字段
        /// </summary>
        /// <param name="oldField"></param>
        /// <param name="newField"></param>
        public void UpdateField(SCField oldField, SCField newField)
        {
            lock (m_lockObject)
            {
                if (oldField == null || newField == null)
                    return;
                int index = GetIndex(oldField);
                if (index >= 0)
                {
                    m_fieldsList.Remove(oldField);
                    m_fieldsList.Insert(index, newField);
                    ClearCache();
                }
            }
        }

        #endregion

        #region private

        /// <summary>
        /// 获得字段缓存
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, SCField> GetNameCache()
        {
            if (m_fieldCache == null)
            {
                m_fieldCache = new Dictionary<string, SCField>();
                foreach (SCField f in m_fieldsList)
                {
                    if (!m_fieldCache.ContainsKey(f.FiledName.Trim()))
                    {
                        m_fieldCache.Add(f.FiledName.Trim(), f);
                    }
                }
            }
            return m_fieldCache;
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        private void ClearCache()
        {
            if (m_fieldCache != null)
            {
                m_fieldCache.Clear();
            }
        }

        #endregion

        #region ICloneable 成员

        public object Clone()
        {
            SCMetaData md = new SCMetaData();
            if (m_fieldsList.Count > 0)
            {
                md.Fields.AddRange(m_fieldsList);
            }
            return md;
        }

        #endregion
    }
}
