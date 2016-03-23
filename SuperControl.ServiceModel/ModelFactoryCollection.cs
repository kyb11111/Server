using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.Common;
using System.Data;
using System.IO;

namespace SuperControl.ServiceModel
{
    public class ModelFactoryCollection : ObservableCollection<ModelFactory>
    {
        #region ModelFactoryMapping
        internal class ModelFactoryMapping
        {
            internal ModelFactory m_factory;
            internal ModelMapping m_mapping;
        }
        #endregion

        //用于数据库加锁
        internal static object s_locker = new object();

        #region 静态变量和方法
        private static ModelFactory s_defaultFactory;
        private static Dictionary<Type, ModelFactoryCollection> s_dictionary;
        static ModelFactoryCollection()
        {
            s_defaultFactory = ModelFactory.CreateFactory("default");
            s_defaultFactory.Initialize("default");
            //s_defaultFactory.Initialize("mysql_new");

            s_defaultFactory.MaxRid = int.MaxValue;
            s_defaultFactory.MinRid = 0;
            s_dictionary = new Dictionary<Type, ModelFactoryCollection>();
        }

        public static void BuildFactories(Type modelType)
        {
            string value = ConfigurationManager.AppSettings[modelType.FullName];
            if (value == null)
                return;
            ModelFactoryCollection collection = new ModelFactoryCollection(modelType);
            string[] ss1 = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            int minRid = 0;
            int maxRid = 0;
            // Console.Write(value);
            foreach (string s1 in ss1)
            {
                string[] ss2 = s1.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                ModelFactory factory = ModelFactory.CreateFactory(value);
                if (ss2.Length == 1)
                {
                    factory.MinRid = minRid;
                    factory.MaxRid = int.MaxValue;
                    factory.Initialize(ss2[0]);
                    collection.Add(factory);
                    break;
                }
                else if (ss2.Length == 2)
                {
                    if (!int.TryParse(ss2[0], out maxRid))
                        throw new ModelServiceException("配置文件格式错误，key={0}", modelType.Name);
                    if (maxRid < minRid)
                        throw new ModelServiceException("配置文件格式错误，key={0}", modelType.Name);
                    factory.MinRid = minRid;
                    factory.MaxRid = maxRid;
                    minRid = maxRid;
                    factory.Initialize(ss2[1]);
                    collection.Add(factory);
                }
            }
            s_dictionary.Add(modelType, collection);
        }

        public static void CloseAllConnection()
        {
            foreach (ModelFactory factory in ModelFactory.s_factoryDic.Values)
            {
                lock (factory.DbConnection)
                {
                    if (factory.DbConnection.State != ConnectionState.Closed)
                    {
                        factory.DbConnection.Close();
                    }
                }
            }
            //s_defaultFactory.DbConnection.Close();
            //foreach (ModelFactoryCollection collection in s_dictionary.Values)
            //{
            //    foreach (ModelFactory factory in collection)
            //    {
            //        if (factory != s_defaultFactory)
            //            factory.DbConnection.Close();
            //    }
            //}
        }

        public static ModelFactory GetFactory(Type modelType, int rid)
        {
            ModelFactoryCollection collection;
            if (s_dictionary.TryGetValue(modelType, out collection))
                return collection.GetFactory(rid);
            return s_defaultFactory;
        }

        public static ModelFactory[] GetFactories(Type modelType)
        {
            ModelFactoryCollection collection;
            if (s_dictionary.TryGetValue(modelType, out collection))
                return collection.ToArray();
            return new ModelFactory[] { s_defaultFactory };
        }

        public static int GetMaxRid(ModelMapping mapping)
        {
            int ret = 0;
            lock (s_locker)
            {
                foreach (ModelFactory factory in GetFactories(mapping.Root.ModelType))
                {
                    int rid = factory.GetMaxRid(mapping);
                    if (ret < rid)
                        ret = rid;
                }
            }
            return ret;
        }

        public static int UpdateModel(ModelBase value, ModelMapping mapping, out Exception exception)
        {
            int count = 0;
            lock (s_locker)
            {
                exception = null;
                if (mapping.Parent == null)
                {
                    try
                    {
                        ModelFactory factory = GetFactory(mapping.ModelType, value.Rid);
                        DbCommand cmd = factory.GetUpdateCommand(value, mapping);
                        if (factory.DbConnection.State == ConnectionState.Closed)
                        {
                            factory.DbConnection.Open();
                        }
                        count = cmd.ExecuteNonQuery();
                        if (factory.DbConnection.State != ConnectionState.Closed)
                        {
                            factory.DbConnection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
                else
                {
                    List<ModelFactoryMapping> factoryList = GetFactories(mapping, value.Rid);
                    //foreach (ModelFactoryMapping mfm in factoryList)
                    //{
                    //    mfm.m_factory.DbCommand.Transaction = mfm.m_factory.DbConnection.BeginTransaction();
                    //}
                    HashSet<ModelFactory> factories = new HashSet<ModelFactory>();
                    foreach (ModelFactoryMapping mfm in factoryList)
                    {
                        factories.Add(mfm.m_factory);
                    }
                    //foreach (ModelFactory factory in factories)
                    //{    //田濛 12.11.11
                    //    if (factory.DbConnection.State == ConnectionState.Closed)
                    //    {
                    //        factory.DbConnection.Open();
                    //    }
                    //    lock (factory.DbCommand)
                    //    {
                    //        factory.DbCommand.Transaction = factory.DbConnection.BeginTransaction();
                    //    }

                    //    if (factory.DbConnection.State != ConnectionState.Closed)
                    //    {
                    //        factory.DbConnection.Close();
                    //    }
                    //}
                    try
                    {
                        foreach (ModelFactoryMapping mfm in factoryList)
                        {
                            if (mfm.m_factory.DbConnection.State == ConnectionState.Closed)
                            {
                                mfm.m_factory.DbConnection.Open();

                            }
                            DbCommand cmd = mfm.m_factory.GetUpdateCommand(value, mfm.m_mapping);
                            count += cmd.ExecuteNonQuery();
                            if (mfm.m_factory.DbConnection.State != ConnectionState.Closed)
                            {
                                mfm.m_factory.DbConnection.Close();
                            }
                        }
                        //foreach (ModelFactoryMapping mfm in factoryList)
                        //{
                        //    mfm.m_factory.DbCommand.Transaction.Commit();
                        //}
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    //田濛 12.11.11
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //  factory.DbCommand.Transaction.Commit();
                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                       // }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        //foreach (ModelFactoryMapping mfm in factoryList)
                        //{
                        //    mfm.m_factory.DbCommand.Transaction.Rollback();
                        //}
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    //田濛 12.11.11
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //    factory.DbCommand.Transaction.Rollback();
                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                    finally
                    {
                        //foreach (ModelFactoryMapping mfm in factoryList)
                        //{
                        //    mfm.m_factory.DbCommand.Transaction = null;
                        //}
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();

                        //    }
                        //    factory.DbCommand.Transaction = null;

                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                }
            }
            return count;
        }

        public static int InsertModel(ModelBase value, ModelMapping mapping, out Exception exception)
        {
            int count = 0;
            lock (s_locker)
            {
                exception = null;
                if (mapping.Parent == null)
                {
                    try
                    {
                        ModelFactory factory = GetFactory(mapping.ModelType, value.Rid);
                        //田濛 12.11.11
                        if (factory.DbConnection.State == ConnectionState.Closed)
                        {
                            factory.DbConnection.Open();
                        }
                        if (value.Rid == 0)
                            value.Rid = factory.GetMaxRid(mapping.ModelType);
                        if (factory.DbConnection.State == ConnectionState.Closed)
                        {
                            factory.DbConnection.Open();
                        }
                      
                        DbCommand cmd = factory.GetInsertCommand(value, mapping);
                        //string sql = cmd.CommandText;
                        //StringBuilder str = new StringBuilder();
                        //string[] ss = sql.Split('@');
                        //str.Append(ss[0]);
                        //foreach (DbParameter dd in cmd.Parameters)
                        //{
                        //    str.Append("'");
                        //    str.Append(dd.Value);
                        //    str.Append("'");
                        //    str.Append(",");
                        //}
                        //sql = string.Empty;
                        //sql = str.ToString();
                        //sql = sql.Substring(0, sql.Length - 1);
                        //sql = sql + ");";

                        //string sqlname = sql.Substring(12, 16);
                        //if (sqlname == "EnergyRegistrationData")
                        //{
                        //    string filename = string.Format("d:\\sql\\EnergyRegistrationData_{0}.txt", DateTime.Now.Ticks);
                        //    FileStream fs = new FileStream(filename, FileMode.Append);
                        //    StreamWriter sw = new StreamWriter(fs);
                        //    sw.BaseStream.Seek(0, SeekOrigin.End);
                        //    sw.WriteLine(sql);
                        //    sw.Close();
                        //    fs.Close();
                        //}
                        count = cmd.ExecuteNonQuery();
                        if (factory.DbConnection.State != ConnectionState.Closed)
                        {
                            factory.DbConnection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
                else
                {
                    List<ModelFactoryMapping> factoryList = GetFactories(mapping, value.Rid);
                    //foreach (ModelFactoryMapping mfm in factoryList)
                    //{
                    //    mfm.m_factory.DbCommand.Transaction = mfm.m_factory.DbConnection.BeginTransaction();
                    //}
                    HashSet<ModelFactory> factories = new HashSet<ModelFactory>();
                    foreach (ModelFactoryMapping mfm in factoryList)
                    {
                        factories.Add(mfm.m_factory);
                    }
                    //foreach (ModelFactory factory in factories)
                    //{
                    //    if (factory.DbConnection.State == ConnectionState.Closed)
                    //    {
                    //        factory.DbConnection.Open();
                    //    }
                    //    //factory.DbCommand.Transaction = factory.DbConnection.BeginTransaction();
                    //    if (factory.DbConnection.State != ConnectionState.Closed)
                    //    {
                    //        factory.DbConnection.Close();
                    //    }
                    //}
                    try
                    {
                        foreach (ModelFactoryMapping mfm in factoryList)
                        {
                            if (mfm.m_factory.DbConnection.State == ConnectionState.Closed)
                            {
                                mfm.m_factory.DbConnection.Open();

                            }
                            if(value.Rid == 0)
                                value.Rid = mfm.m_factory.GetMaxRid(mapping.ModelType);
                            if (mfm.m_factory.DbConnection.State == ConnectionState.Closed)
                            {
                                mfm.m_factory.DbConnection.Open();
                            }
                            DbCommand cmd = mfm.m_factory.GetInsertCommand(value, mfm.m_mapping);
                            count += cmd.ExecuteNonQuery();

                            if (mfm.m_factory.DbConnection.State != ConnectionState.Closed)
                            {
                                mfm.m_factory.DbConnection.Close();
                            }
                        }
                        //foreach (ModelFactoryMapping mfm in factoryList)
                        //{
                        //    mfm.m_factory.DbCommand.Transaction.Commit();
                        //}
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //      factory.DbCommand.Transaction.Commit();
                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        //foreach (ModelFactoryMapping mfm in factoryList)
                        //{
                        //    mfm.m_factory.DbCommand.Transaction.Rollback();
                        //}
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //    factory.DbCommand.Transaction.Rollback();
                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                    finally
                    {
                        //foreach (ModelFactoryMapping mfm in factoryList)
                        //{
                        //    mfm.m_factory.DbCommand.Transaction = null;
                        //}
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //    factory.DbCommand.Transaction = null;
                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                }
            }
            return count;
        }

        public static int InsertModelReturnRid(ModelBase value, ModelMapping mapping, out Exception exception)
        {
            int count = 0;
            lock (s_locker)
            {
                exception = null;
                if (mapping.Parent == null)
                {
                    try
                    {
                        ModelFactory factory = GetFactory(mapping.ModelType, value.Rid);
                        //田濛 12.11.11
                        if (factory.DbConnection.State == ConnectionState.Closed)
                        {
                            factory.DbConnection.Open();
                        }
                        if(value.Rid == 0)
                             value.Rid = factory.GetMaxRid(mapping.ModelType);
                        DbCommand cmd = factory.GetInsertCommand(value, mapping);
                        count = cmd.ExecuteNonQuery();
                        if (factory.DbConnection.State != ConnectionState.Closed)
                        {
                            factory.DbConnection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
                else
                {
                    List<ModelFactoryMapping> factoryList = GetFactories(mapping, value.Rid);
                    //foreach (ModelFactoryMapping mfm in factoryList)
                    //{
                    //    mfm.m_factory.DbCommand.Transaction = mfm.m_factory.DbConnection.BeginTransaction();
                    //}
                    HashSet<ModelFactory> factories = new HashSet<ModelFactory>();
                    foreach (ModelFactoryMapping mfm in factoryList)
                    {
                        factories.Add(mfm.m_factory);
                    }
                    //foreach (ModelFactory factory in factories)
                    //{
                    //    if (factory.DbConnection.State == ConnectionState.Closed)
                    //    {
                    //        factory.DbConnection.Open();
                    //    }
                    //    factory.DbCommand.Transaction = factory.DbConnection.BeginTransaction();
                    //    if (factory.DbConnection.State != ConnectionState.Closed)
                    //    {
                    //        factory.DbConnection.Close();
                    //    }
                    //}
                    try
                    {
                        foreach (ModelFactoryMapping mfm in factoryList)
                        {
                            if (mfm.m_factory.DbConnection.State == ConnectionState.Closed)
                            {
                                mfm.m_factory.DbConnection.Open();

                            }
                            if(value.Rid == 0)
                                value.Rid = mfm.m_factory.GetMaxRid(mapping.ModelType);
                            if (mfm.m_factory.DbConnection.State == ConnectionState.Closed)
                            {
                                mfm.m_factory.DbConnection.Open();
                            }
                            DbCommand cmd = mfm.m_factory.GetInsertCommand(value, mfm.m_mapping);
                            count += cmd.ExecuteNonQuery();

                            if (mfm.m_factory.DbConnection.State != ConnectionState.Closed)
                            {
                                mfm.m_factory.DbConnection.Close();
                            }
                        }
                        //foreach (ModelFactoryMapping mfm in factoryList)
                        //{
                        //    mfm.m_factory.DbCommand.Transaction.Commit();
                        //}
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //    factory.DbCommand.Transaction.Commit();
                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        //foreach (ModelFactoryMapping mfm in factoryList)
                        //{
                        //    mfm.m_factory.DbCommand.Transaction.Rollback();
                        //}
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //    factory.DbCommand.Transaction.Rollback();
                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                    finally
                    {
                        //foreach (ModelFactoryMapping mfm in factoryList)
                        //{
                        //    mfm.m_factory.DbCommand.Transaction = null;
                        //}
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //    factory.DbCommand.Transaction = null;
                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                }
            }
            if (count > 0)
                return value.Rid;
            return count;
        }

        public static int DeleteModel(ModelBase value, ModelMapping mapping, out Exception exception)
        {
            int count = 0;
            lock (s_locker)
            {
                exception = null;
                if (mapping.Parent == null)
                {
                    try
                    {
                        ModelFactory factory = GetFactory(mapping.ModelType, value.Rid);
                        DbCommand cmd = factory.GetDeleteCommand(value, mapping);
                        // Console.WriteLine(cmd.CommandText);
                        if (factory.DbConnection.State == ConnectionState.Closed)
                        {
                            factory.DbConnection.Open();
                        }
                        count = cmd.ExecuteNonQuery();
                        if (factory.DbConnection.State != ConnectionState.Closed)
                        {
                            factory.DbConnection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
                else
                {

                    List<ModelFactoryMapping> factoryList = GetFactories(mapping, value.Rid);
                    HashSet<ModelFactory> factories = new HashSet<ModelFactory>();
                    foreach (ModelFactoryMapping mfm in factoryList)
                    {
                        factories.Add(mfm.m_factory);
                    }
                    //foreach (ModelFactory factory in factories)
                    //{
                    //    if (factory.DbConnection.State == ConnectionState.Closed)
                    //    {
                    //        factory.DbConnection.Open();
                    //    }
                    //    factory.DbCommand.Transaction = factory.DbConnection.BeginTransaction();
                    //    if (factory.DbConnection.State != ConnectionState.Closed)
                    //    {
                    //        factory.DbConnection.Close();
                    //    }
                    //}
                    try
                    {
                        foreach (ModelFactoryMapping mfm in factoryList)
                        {
                            if (mfm.m_factory.DbConnection.State == ConnectionState.Closed)
                            {
                                mfm.m_factory.DbConnection.Open();
                            }
                            DbCommand cmd = mfm.m_factory.GetDeleteCommand(value, mfm.m_mapping);
                            count += cmd.ExecuteNonQuery();
                            if (mfm.m_factory.DbConnection.State != ConnectionState.Closed)
                            {
                                mfm.m_factory.DbConnection.Close();
                            }
                        }
                        //foreach (ModelFactoryMapping mfm in factoryList)
                        //{
                        //    mfm.m_factory.DbCommand.Transaction.Commit();
                        //}
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //   factory.DbCommand.Transaction.Commit();
                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        //foreach (ModelFactoryMapping mfm in factoryList)
                        //{
                        //    mfm.m_factory.DbCommand.Transaction.Rollback();
                        //}
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //    factory.DbCommand.Transaction.Rollback();
                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                    finally
                    {
                        //foreach (ModelFactoryMapping mfm in factoryList)
                        //{
                        //    mfm.m_factory.DbCommand.Transaction = null;
                        //}
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //    factory.DbCommand.Transaction = null;
                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                }
            }
            return count;
        }

        public static int AppendModel(ModelBase value, ModelMapping mapping, out Exception exception)
        {
            int count = 0;
            lock (s_locker)
            {
                exception = null;
                ModelFactory[] factoryList = GetFactories(mapping.ModelType);
                if (factoryList.Length <= 1)
                {
                    try
                    {
                        foreach (ModelFactory factory in factoryList)
                        {
                            DbCommand cmd = factory.GetAppendCommand(value, mapping);
                            if (factory.DbConnection.State == ConnectionState.Closed)
                            {
                                factory.DbConnection.Open();
                            }
                            count += cmd.ExecuteNonQuery();
                            if (factory.DbConnection.State != ConnectionState.Closed)
                            {
                                factory.DbConnection.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
                else
                {

                    //foreach (ModelFactory factory in factories)
                    //{
                    //    factory.DbCommand.Transaction = factory.DbConnection.BeginTransaction();
                    //}
                    HashSet<ModelFactory> factories = new HashSet<ModelFactory>();
                    foreach (ModelFactory f in factoryList)
                    {
                        factories.Add(f);
                    }
                    //foreach (ModelFactory factory in factories)
                    //{
                    //    if (factory.DbConnection.State == ConnectionState.Closed)
                    //    {
                    //        factory.DbConnection.Open();
                    //    }
                    //    factory.DbCommand.Transaction = factory.DbConnection.BeginTransaction();

                    //    if (factory.DbConnection.State != ConnectionState.Closed)
                    //    {
                    //        factory.DbConnection.Close();
                    //    }
                    //}
                    try
                    {
                        foreach (ModelFactory factory in factories)
                        {
                            if (factory.DbConnection.State == ConnectionState.Closed)
                            {
                                factory.DbConnection.Open();
                            }
                            DbCommand cmd = factory.GetAppendCommand(value, mapping);
                            count += cmd.ExecuteNonQuery();
                            if (factory.DbConnection.State != ConnectionState.Closed)
                            {
                                factory.DbConnection.Close();
                            }
                        }
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //    factory.DbCommand.Transaction.Commit();

                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //    factory.DbCommand.Transaction.Rollback();

                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                    finally
                    {
                        //foreach (ModelFactory factory in factories)
                        //{
                        //    if (factory.DbConnection.State == ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Open();
                        //    }
                        //    factory.DbCommand.Transaction = null;
                        //    if (factory.DbConnection.State != ConnectionState.Closed)
                        //    {
                        //        factory.DbConnection.Close();
                        //    }
                        //}
                    }
                }
            }
            return count;
        }

        private static List<ModelFactoryMapping> GetFactories(ModelMapping mapping, int rid)
        {
            List<ModelFactoryMapping> list = new List<ModelFactoryMapping>();
            while (mapping != null)
            {
                ModelFactoryMapping mfm = new ModelFactoryMapping();
                mfm.m_factory = GetFactory(mapping.ModelType, rid);
                mfm.m_mapping = mapping;
                list.Add(mfm);
                mapping = mapping.Parent;
            }
            return list;
        }
        #endregion

        #region 成员变量和方法
        private readonly ModelMapping m_mapping;

        public ModelFactoryCollection(Type modelType)
        {
            m_mapping = new ModelMapping(modelType);
        }

        private ModelFactory GetFactory(int rid)
        {
            foreach (ModelFactory factory in this)
            {
                if (rid <= factory.MaxRid && rid > factory.MinRid)
                    return factory;
            }
            return s_defaultFactory;
        }
        #endregion
    }
}