using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Collections;

namespace SuperControl.ServiceModel
{
    public class ObjectDatabaseReader : IEnumerable<object>
    {
        private Type m_type;
        private ModelMapping m_field;
        private DbCommand m_command;
        private ObjectDatabaseEnumerator m_enumerator;

        public ObjectDatabaseReader(Type type, DbCommand command)
        {
            m_type = type;
            m_field = new ModelMapping(type);
            DbCommand = command;
        }

        public DbCommand DbCommand
        {
            get
            {
                return m_command;
            }
            set
            {
                m_command = value;
                m_enumerator = new ObjectDatabaseEnumerator(m_type, m_field, m_command);
            }
        }

        #region IEnumerable<object>
        public virtual IEnumerator GetEnumerator()
        {
            return m_enumerator;
        }

        IEnumerator<object> IEnumerable<object>.GetEnumerator()
        {
            return m_enumerator;
        }
        #endregion
    }

    public class ObjectDatabaseEnumerator : IEnumerator<object>
    {
        private Type m_type;
        private ModelMapping m_field;
        private DbCommand m_command;
        private DbDataReader m_reader;

        internal ObjectDatabaseEnumerator(Type type, ModelMapping field, DbCommand command)
        {
            m_type = type;
            m_field = field;
            m_command = command;
            Reset();
        }

        #region IEnumerator<object>
        public virtual object Current
        {
            get { return GetCurrent(); }
        }

        public virtual bool MoveNext()
        {
            return m_reader.Read();
        }

        public virtual void Reset()
        {
            m_reader = m_command.ExecuteReader();
        }

        object IEnumerator<object>.Current
        {
            get { return GetCurrent(); }
        }

        public void Dispose()
        {
            m_reader.Close();
        }
        #endregion

        private object GetCurrent()
        {
            object obj = m_type.Assembly.CreateInstance(m_type.FullName);
            for (int i = 0; i < m_reader.FieldCount; i++)
            {
                object value = m_reader.GetValue(i);
                object cv = Convert.ChangeType(value, m_field.PropertyFields[i - 1].Property.PropertyType);
                m_field.PropertyFields[i].Property.SetValue(obj, cv, null);
            }
            return obj as ModelBase;
        }
    }
}
