using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Data.Common;
using System.Reflection;
using System.Data.SqlTypes;
using System.Data;
namespace SuperControl.ServiceModel
{
    public class ModelDatabaseReader : IEnumerable<ModelBase>
    {
        private Type m_type;
        private ModelMapping m_field;
        private DbCommand m_command;
        private ModelDatabaseEnumerator m_enumerator;

        public ModelDatabaseReader(Type type, ModelFactory factory)
        {
            m_type = type;
            m_field = new ModelMapping(type);
            DbCommand = factory.GetSelectCommand(m_field);
        }

        /// <summary>
        /// 我自己加的带条件查询
        /// </summary>
        public ModelDatabaseReader(Type type, ModelFactory factory, string where)
        {
            m_type = type;
            m_field = new ModelMapping(type);
            DbCommand = factory.GetSelectCommand(m_field, where, true);
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
                m_enumerator = new ModelDatabaseEnumerator(m_type, m_field, m_command);
            }
        }

        #region IEnumerable<ModelBase>
        public virtual IEnumerator GetEnumerator()
        {
            return m_enumerator;
        }

        IEnumerator<ModelBase> IEnumerable<ModelBase>.GetEnumerator()
        {
            return m_enumerator;
        }
        #endregion
    }

    public class ModelDatabaseEnumerator : IEnumerator<ModelBase>
    {
        private static DateTime s_zeroTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        private Type m_type;
        private ModelMapping m_field;
        private DbCommand m_command;
        private DbDataReader m_reader;

        internal ModelDatabaseEnumerator(Type type, ModelMapping field, DbCommand command)
        {
            m_type = type;
            m_field = field;
            m_command = command;
            Reset();
        }

        #region IEnumerator<ModelBase>
        public virtual object Current
        {
            get { return GetCurrent(); }
        }

        public virtual bool MoveNext()
        {
            //
            if (m_reader != null)
                return m_reader.Read();
            else
                return false;
            //吴杰改

            //
        }

        public virtual void Reset()
        {
            if (m_reader != null)
                m_reader.Close();
            lock (ModelFactoryCollection.s_locker)
            {
                //if (m_command.Connection.State != ConnectionState.Closed)
                //    m_command.Connection.Close();
                if (m_command.Connection.State == ConnectionState.Closed)
                    m_command.Connection.Open();
                lock (m_command)
                {

                    try
                    {
                        m_reader = m_command.ExecuteReader();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("aaa:");
                        Console.WriteLine(ex.Message);
                    }
                }

            }
        }

        ModelBase IEnumerator<ModelBase>.Current
        {
            get { return GetCurrent(); }
        }

        public void Dispose()
        {
            if (m_reader != null)
                m_reader.Close();
        }
        #endregion

        private ModelBase GetCurrent()
        {
            ModelBase model = m_type.Assembly.CreateInstance(m_type.FullName) as ModelBase;
            if (model != null)
            {
                model.Rid = (int)Convert.ChangeType(m_reader.GetValue(0), typeof(int));
                for (int i = 1; i < m_reader.FieldCount; i++)
                {
                    object value = m_reader.GetValue(i);
                    PropertyInfo property = m_field.PropertyFields[i - 1].Property;
                    property.SetValue(model, ChangeType(value, property.PropertyType), null);
                }
            }
            return model;
        }

        public static object ChangeType(object value, Type conversionType)
        {
            if (conversionType == typeof(string))
            {
                return value.ToString().Trim();
            }
            else if (conversionType == typeof(DateTime))
            {
                if (value is DateTime)
                    return value;
                else if (value is long)
                {
                    long misc = (long)value;
                    return s_zeroTime.Add(TimeSpan.FromMilliseconds((double)misc));
                }
                else if (value is string)
                {
                    DateTime ret;
                    if (DateTime.TryParse(value.ToString(), out ret))
                    {
                        return ret;
                    }
                    return DateTime.Now;
                }
                else
                    return new DateTime();
            }
            else if (conversionType == typeof(Boolean))
            {
                if (value is bool)
                    return value;
                else
                {
                    return int.Parse(value.ToString()) == 0 ? false : true;
                }
            }
            else
            {//////////此处转换有问题//////////转换bool类型出错
                if (value is DBNull)
                    return null;
                else
                    return Convert.ChangeType(value, conversionType);

            }
        }
    }


    public class ModelEnumerator : IEnumerable<ModelBase>, IEnumerator<ModelBase>
    {
        private Type m_type;
        private List<IEnumerator<ModelBase>> m_readerList;
        private IEnumerator<IEnumerator<ModelBase>> m_listEnumerator;
        public ModelEnumerator(Type type)
        {
            m_type = type;
            m_readerList = new List<IEnumerator<ModelBase>>();
            foreach (ModelFactory factory in ModelFactoryCollection.GetFactories(type))
            {
                IEnumerable<ModelBase> reader = new ModelDatabaseReader(type, factory);
                m_readerList.Add(reader.GetEnumerator());
            }
            m_listEnumerator = m_readerList.GetEnumerator();
            m_listEnumerator.MoveNext();
        }
        /// <summary>
        /// 
        /// </summary>
        public ModelEnumerator(Type type, string where)
        {
            m_type = type;
            m_readerList = new List<IEnumerator<ModelBase>>();
            foreach (ModelFactory factory in ModelFactoryCollection.GetFactories(type))
            {
                IEnumerable<ModelBase> reader = new ModelDatabaseReader(type, factory, where);
                m_readerList.Add(reader.GetEnumerator());
            }
            m_listEnumerator = m_readerList.GetEnumerator();
            m_listEnumerator.MoveNext();
        }
        public Type ModelType
        {
            get { return m_type; }
        }


        #region 胡珊添加 2012-5-15
        public T[] ToArray<T>() where T : ModelBase
        {
            List<T> list = new List<T>();
            foreach (ModelBase model in this)
            {
                T item = model as T;
                if (item != null)
                    list.Add(item);
            }
            return list.ToArray();
        }
        public T GetItem<T>(int rid) where T : ModelBase
        {
            foreach (ModelBase model in this)
            {
                T item = model as T;
                if (item.Rid == rid)
                    return item;
            }
            return null;
        }
        #endregion

        #region IEnumerable<ModelBase>
        public IEnumerator<ModelBase> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }
        #endregion

        #region IEnumerator<ModelBase>
        public ModelBase Current
        {
            get { return m_listEnumerator.Current.Current; }
        }

        public void Dispose()
        {
            foreach (IEnumerator<ModelBase> enumerator in m_readerList)
                enumerator.Dispose();
        }

        object IEnumerator.Current
        {
            get { return m_listEnumerator.Current.Current; }
        }

        public bool MoveNext()
        {
            bool ret = m_listEnumerator.Current.MoveNext();
            if (!ret)
            {
                ret = m_listEnumerator.MoveNext();
                if (ret)
                    m_listEnumerator.Current.MoveNext();
            }
            return ret;
        }

        public void Reset()
        {
            foreach (IEnumerator<ModelBase> enumerator in m_readerList)
                enumerator.Reset();
            m_listEnumerator.Reset();
        }
        #endregion
    }
}
