using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SuperControl.ServiceModel
{
    [DataContract]
    public class SCField : ICloneable
    {

        public SCField()
        {
        }

        #region props

        private string m_dataType;
        private string m_caption;
        private string m_fieldName;
        private string m_expression;
        private int m_maxLength;
        private bool m_isRequire;
        private bool m_isKey;
        private bool m_isReadOnly;

        /// <summary>
        /// 数据类型
        /// </summary>
        [DataMember]
        public string DataType
        {
            get { return m_dataType; }
            set { m_dataType = value; }
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        [DataMember]
        public string Caption
        {
            get { return m_caption; }
            set { m_caption = value; }
        }

        /// <summary>
        /// 字段名称
        /// </summary>
        [DataMember]
        public string FiledName
        {
            get { return m_fieldName; }
            set { m_fieldName = value; }
        }

        /// <summary>
        /// 表达式
        /// </summary>
        [DataMember]
        public string Expression
        {
            get { return m_expression; }
            set { m_expression = value; }
        }

        /// <summary>
        /// 最大长度
        /// </summary>
        [DataMember]
        public int MaxLength
        {
            get { return m_maxLength; }
            set { m_maxLength = value; }
        }

        /// <summary>
        /// 是否可空
        /// </summary>
        [DataMember]
        public bool IsRequire
        {
            get { return m_isRequire; }
            set { m_isRequire = value; }
        }

        /// <summary>
        /// 是否为主键
        /// </summary>
        [DataMember]
        public bool IsKey
        {
            get { return m_isKey; }
            set { m_isKey = value; }
        }

        /// <summary>
        /// 是否只读
        /// </summary>
        [DataMember]
        public bool IsReadOnly
        {
            get { return m_isReadOnly; }
            set { m_isReadOnly = value; }
        }

        #endregion

        public override string ToString()
        {
            return String.IsNullOrEmpty(m_caption) ? m_fieldName : m_caption;
        }

        public override bool Equals(object obj)
        {
            if (m_fieldName == null || obj == null || !(obj is SCField))
                return false;
            SCField f = (SCField)obj;
            return f.FiledName.Equals(m_fieldName);
        }

        public override int GetHashCode()
        {
            return m_fieldName.GetHashCode();
        }

        #region ICloneable 成员

        public object Clone()
        {
            SCField f = new SCField();
            f.Caption = this.m_caption;
            f.DataType = this.DataType;
            f.Expression = this.m_expression;
            f.FiledName = this.m_fieldName;
            f.IsKey = this.m_isKey;
            f.IsReadOnly = this.m_isReadOnly;
            f.IsRequire = this.m_isRequire;
            f.MaxLength = this.m_maxLength;
            //f.Note = this.note;
            //f.Precision = this.precision;
            //f.Scale = this.scale;
            return f;
        }

        #endregion
    }
}
