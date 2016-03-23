using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Data;

namespace SuperControl.ServiceModel
{
    [DataContract]
    public class SCDataTable : ICloneable
    {
        #region prop

        private SCMetaData m_md;
        private List<List<object>> m_datas = new List<List<object>>();
        private string m_tableName = string.Empty;

        /// <summary>
        /// 元数据信息
        /// </summary>
        [DataMember]
        public SCMetaData MetaData
        {
            get { return m_md; }
            set { m_md = value; }
        }

        /// <summary>
        /// DataTable数据,目前只能放基本类型数据
        /// </summary>
        [DataMember]
        public List<List<object>> Datas
        {
            get { return m_datas; }
            set { m_datas = value; }
        }

        /// <summary>
        /// 字段列表
        /// </summary>
        [DataMember]
        public string TableName
        {
            get { return m_tableName; }
            set { m_tableName = value; }
        }
        #endregion

        public virtual SCField GetField(string fieldName)
        {
            if (m_md == null)
                return null;
            return m_md.GetField(fieldName);
        }

        public virtual string[] GetFieldNames()
        {
            if (m_md == null)
                return new string[] { };
            return m_md.GetFieldNames();
        }

        #region ICloneable 成员

        public object Clone()
        {
            SCDataTable dsd = new SCDataTable();
            dsd.MetaData = (SCMetaData)this.m_md.Clone();
            return dsd;
        }

        #endregion
    }
}
