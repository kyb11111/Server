using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Reflection;
using System.Configuration;
using System.Data;
using System.IO;

namespace SuperControl.ServiceModel
{
    public abstract class ModelFactory
    {
        internal string EscapeExpr = "{0}";
        protected virtual string CreateSelectSqlString(ModelMapping mapping, string where, bool hasRid)
        {
            string sql = "select ";
            if (hasRid)
                sql += "Rid,";
            foreach (PropertyFieldPair pair in mapping.PropertyFields)
            {
                sql += string.Format(EscapeExpr, pair.FieldName);
                sql += ",";
            }
            sql = sql.TrimEnd(',');
            sql += " from ";
            sql += mapping.TableName;
            if (!string.IsNullOrWhiteSpace(where))
            {
                sql += " where ";
                sql += where;
            }
            sql += ";";
            return sql;
        }

        protected virtual string CreateSelectModelSqlString(ModelMapping mapping, int rid)
        {
            return CreateSelectSqlString(mapping, string.Format("Rid={0}", rid), true);
        }

        protected virtual string CreateInsertSqlString(ModelBase value, ModelMapping mapping)
        {
            string sql = "insert into ";
            sql += mapping.TableName;
            sql += " (";
            foreach (PropertyFieldPair pair in mapping.PropertyFields)
            {
                sql += string.Format(EscapeExpr, pair.FieldName);
                sql += ",";
            }
            sql = sql.TrimEnd(',');
            sql += ") values (";
            bool IsFirst = true;
            foreach (PropertyFieldPair pair in mapping.PropertyFields)
            {
                if (IsFirst)
                {
                    sql += "@";
                    sql += pair.FieldName;
                    IsFirst = false;
                }
                else
                {
                    sql += ",@";
                    sql += pair.FieldName;
                }
            }
            sql += ");";
            return sql;
        }

        protected virtual string CreateAppendSqlString(ModelBase value, ModelMapping mapping)
        {
            string sql = "insert into ";
            sql += mapping.TableName;
            sql += " (";
            foreach (PropertyFieldPair pair in mapping.PropertyFields)
            {
                sql += string.Format(EscapeExpr, pair.FieldName);
                sql += ",";
            }
            sql = sql.TrimEnd(',');
            sql += ") values (";
            foreach (PropertyFieldPair pair in mapping.PropertyFields)
            {
                sql += "@";
                sql += pair.FieldName;
                sql += ",";
            }
            sql = sql.TrimEnd(',');
            sql += ");";
            return sql;
        }

        protected virtual string CreateUpdateSqlString(ModelBase value, ModelMapping mapping)
        {
            string sql = "update ";
            sql += mapping.TableName;
            sql += " set ";
            foreach (PropertyFieldPair pair in mapping.PropertyFields)
            {
                sql += string.Format(EscapeExpr, pair.FieldName);
                sql += "=@";
                sql += pair.FieldName;
                sql += ",";
            }
            sql = sql.TrimEnd(',');
            sql += " where Rid=";
            sql += value.Rid.ToString();
            sql += ";";
            return sql;
        }

        protected virtual string CreateDeleteSqlString(ModelBase value, ModelMapping mapping)
        {
            string sql = "delete from ";
            sql += mapping.TableName;
            sql += " where Rid = ";
            sql += value.Rid;
            return sql;
        }

        internal abstract DbConnection DbConnection
        {
            get;
        }

        internal abstract DbCommand DbCommand
        {
            get;
        }

        protected abstract void Initialize(string providerName, string connectionString);

        private bool m_isInitialized = false;
        internal void Initialize(string configName)
        {
            if (!m_isInitialized)
            {
                ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings[configName];
                if (setting == null)
                    throw new ModelServiceException("找不到链接,配置名:{0}", configName);
                string providerName = setting.ProviderName.Split(':')[0];
                Initialize(providerName, setting.ConnectionString);
                //DbConnection.Open();
                m_isInitialized = true;
            }
        }

        internal int MaxRid
        {
            set;
            get;
        }

        internal int MinRid
        {
            set;
            get;
        }

        public virtual DbCommand GetSelectCommand(ModelMapping mapping, string where, bool hasRid)
        {//孙良旭 2012-08-15,参考GetUpdateCommand也补充lock
            lock (DbCommand)
            {
                DbCommand.CommandText = CreateSelectSqlString(mapping, where, hasRid);
                return DbCommand;
            }
        }

        public virtual DbCommand GetSelectCommand(ModelMapping mapping)
        {
            string where = string.Format("Rid <= {0} and Rid > {1}", MaxRid, MinRid);
            return GetSelectCommand(mapping, where, true);
        }

        public virtual DbCommand GetSelectModelCommand(ModelMapping mapping, int rid)
        {//孙良旭 2012-08-15,参考GetUpdateCommand也补充lock
            lock (DbCommand)
            {
                DbCommand.CommandText = CreateSelectModelSqlString(mapping, rid);
                return DbCommand;
            }
        }

        public virtual DbCommand GetInsertCommand(ModelBase value, ModelMapping mapping)
        {//孙良旭 2012-08-15,参考GetUpdateCommand也补充lock
            lock (DbCommand)
            {
                DbCommand.CommandText = CreateInsertSqlString(value, mapping);
                DbCommand.Parameters.Clear();
                foreach (PropertyFieldPair pair in mapping.PropertyFields)
                {
                    DbCommand.Parameters.Add(GetParameter(value, pair));
                }
                return DbCommand;
            }
        }

        public virtual DbCommand GetAppendCommand(ModelBase value, ModelMapping mapping)
        {
            //孙良旭 2012-08-15,参考GetUpdateCommand也补充lock
            lock (DbCommand)
            {
                DbCommand.CommandText = CreateAppendSqlString(value, mapping);
                DbCommand.Parameters.Clear();
                foreach (PropertyFieldPair pair in mapping.PropertyFields)
                {
                    DbCommand.Parameters.Add(GetParameter(value, pair));
                }
                return DbCommand;
            }

        }

        public virtual DbCommand GetUpdateCommand(ModelBase value, ModelMapping mapping)
        {
            //孙良旭 2012-07-25  频繁访问DbCommand对象，造成Parameters项为null，clear报异常，所以加锁
            lock (DbCommand)
            {
                DbCommand.CommandText = CreateUpdateSqlString(value, mapping);

                DbCommand.Parameters.Clear();
                foreach (PropertyFieldPair pair in mapping.PropertyFields)
                {
                    DbCommand.Parameters.Add(GetParameter(value, pair));
                }
            }
            return DbCommand;
        }

        public virtual DbCommand GetDeleteCommand(ModelBase value, ModelMapping mapping)
        {
            //孙良旭 2012-08-15,参考GetUpdateCommand也补充lock
            lock (DbCommand)
            {
                DbCommand.CommandText = CreateDeleteSqlString(value, mapping);
                return DbCommand;
            }
        }

        protected virtual DbParameter GetParameter(object value, PropertyFieldPair pair)
        {//孙良旭 2012-08-15,参考GetUpdateCommand也补充lock
            lock (DbCommand)
            {
                DbParameter parameter = DbCommand.CreateParameter();
                parameter.ParameterName = "@" + pair.FieldName;
                //商希超 2012-08-10 参数异常
                if (pair.Property.PropertyType == typeof(UInt16))
                {
                    parameter.DbType = System.Data.DbType.Int16;
                    parameter.Value = Convert.ToInt16(pair.Property.GetValue(value, null));
                }
                else if (pair.Property.PropertyType == typeof(UInt32))
                {
                    parameter.DbType = System.Data.DbType.Int32;
                    parameter.Value = Convert.ToInt32(pair.Property.GetValue(value, null));
                }
                else if (pair.Property.PropertyType == typeof(UInt64))
                {
                    parameter.DbType = System.Data.DbType.Int64;
                    parameter.Value = Convert.ToInt64(pair.Property.GetValue(value, null));
                }
                else if (pair.Property.PropertyType == typeof(DateTime))
                {
                    //孙东升 2012-8-31
                    DateTime time = (DateTime)pair.Property.GetValue(value, null);
                    if (time <= new DateTime(1753, 1, 1, 12, 0, 0))
                    {
                        time = new DateTime(1753, 1, 1, 12, 0, 0);
                    }
                    parameter.Value = time;
                }
                else if (pair.Property.PropertyType == typeof(string))
                {
                    //孙东升 2012-8-31
                    object obj = pair.Property.GetValue(value, null);
                    if (obj == null)
                    {
                        parameter.Value = DBNull.Value;
                    }
                    else
                    {
                        parameter.Value = obj.ToString();
                    }
                }
                else
                {
                    parameter.Value = pair.Property.GetValue(value, null);
                }

                return parameter;
            }
        }

        internal int GetMaxRid(Type modelType)
        {
            return GetMaxRid(new ModelMapping(modelType));
        }

        internal int GetMaxRid(ModelMapping mapping)
        {
            return GetMaxRid(mapping.Root.TableName);
        }

        internal virtual int GetMaxRid(string tableName)
        {
            object obj;
            string sql = "select Max(Rid) from " + tableName + ";";
            lock (DbConnection)
            {
                if (DbConnection.State == ConnectionState.Closed)
                {
                    //孙良旭 9.18
                    DbConnection.Open();

                }

                lock (DbCommand)
                {
                    DbCommand.CommandText = sql;

                    obj = DbCommand.ExecuteScalar();
                }

                if (DbConnection.State != ConnectionState.Closed)
                {
                    DbConnection.Close();
                }
            }
            if (obj == DBNull.Value)
                return 1;
            ////孙良旭 2012-7-25
            if (obj == null)
                return 1;

            int rid = (int)Convert.ChangeType(obj, typeof(int)) + 1;
            return rid;

        }

        #region 创建工厂
        internal static Dictionary<string, ModelFactory> s_factoryDic = new Dictionary<string, ModelFactory>();

        internal static ModelFactory CreateFactory(string configName)
        {
            ModelFactory factory;
            if (s_factoryDic.TryGetValue(configName, out factory))
            {
                return factory;
            }
            if (factory == null)
            {
                ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings[configName];
                string[] ss = setting.ProviderName.Split(':');
                if (ss.Length > 1)
                {
                    string factoryTypeName = ss[1].Trim();
                    if (!string.IsNullOrWhiteSpace(factoryTypeName))
                    {
                        Type factoryType = Type.GetType(factoryTypeName);
                        factory = factoryType.Assembly.CreateInstance(factoryTypeName) as ModelFactory;
                    }
                }
            }
            if (factory == null)
                factory = new GeneralModelFactory();
            s_factoryDic.Add(configName, factory);
            return factory;
        }

        internal static ModelFactory GetFactory(string configName)
        {
            ModelFactory factory;
            if (s_factoryDic.TryGetValue(configName, out factory))
            {
                return factory;
            }
            return null;
        }
        #endregion
    }
}
