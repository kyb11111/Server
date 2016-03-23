using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.Odbc;
using System.Data;

namespace SuperControl.ServiceModel
{
    public sealed class SCModelFactory : ModelFactory
    {
        protected override string CreateSelectSqlString(ModelMapping mapping, string where, bool hasRid)
        {
            string sql = "select ";
            if (hasRid)
                sql += "Rid,";
            foreach (PropertyFieldPair pair in mapping.PropertyFields)
            {
                sql += pair.FieldName;
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

        protected override string CreateSelectModelSqlString(ModelMapping mapping, int rid)
        {
            return CreateSelectSqlString(mapping, string.Format("Rid={0}", rid), true);
        }

        public override DbCommand GetInsertCommand(ModelBase value, ModelMapping mapping)
        {
            lock (DbCommand)
            {
                DataTable schema = m_connection.GetSchema("Columns", new string[] { null, null, mapping.TableName });
                string sql = "insert into ";
                sql += mapping.TableName;
                sql += " values (";
                sql += value.Rid;
                List<PropertyFieldPair> listPair = new List<PropertyFieldPair>();
                foreach (DataRow row in schema.Rows)
                {
                    string columnName = row["COLUMN_NAME"].ToString();
                    if (columnName.ToLower() == "rid")
                        continue;
                    foreach (PropertyFieldPair pair in mapping.PropertyFields)
                    {
                        if (pair.FieldName.ToLower() == columnName.ToLower())
                        {
                            sql += ",";
                            //sql += "?";
                            object v = pair.Property.GetValue(value, null);
                            if (v == null)
                                sql += "''";
                            else if (row["DATA_TYPE"].ToString() == "12")
                                sql += string.Format("'{0}'", v.ToString());
                            else if (row["DATA_TYPE"].ToString() == "-5" && v.GetType() == typeof(DateTime))
                            {
                                DateTime dt = (DateTime)v;
                                TimeSpan ts = dt.Subtract(new DateTime(1970, 1, 1));
                                long t = Convert.ToInt64(ts.TotalMilliseconds);
                                sql += t.ToString();
                            }
                            else
                                sql += v.ToString();
                            //listPair.Add(pair);
                            goto Next;
                        }
                    }
                    sql += ",";
                    sql += GetDefaultValue(row["DATA_TYPE"].ToString());
                Next:
                    continue;
                }
                sql += ");";
                DbCommand.CommandText = sql;
                DbCommand.Parameters.Clear();
                //foreach (PropertyFieldPair pair in listPair)
                //{
                //    DbCommand.Parameters.Add(GetParameter(value, pair));
                //}
            }
            return DbCommand;
        }

        public override DbCommand GetUpdateCommand(ModelBase value, ModelMapping mapping)
        {
            lock (DbCommand)
            {
                string sql = "update ";
                sql += mapping.TableName;
                sql += " set ";
                foreach (PropertyFieldPair pair in mapping.PropertyFields)
                {
                    sql += pair.FieldName;
                    sql += "=";
                    object v = pair.Property.GetValue(value, null);
                    if (v == null)
                        sql += "''";
                    else if (pair.Property.PropertyType == typeof(string))
                        sql += string.Format("'{0}'", v.ToString());
                    else if (pair.Property.PropertyType == typeof(DateTime))
                    {
                        DateTime dt = (DateTime)v;
                        TimeSpan ts = dt.Subtract(new DateTime(1970, 1, 1));
                        long t = Convert.ToInt64(ts.TotalMilliseconds);
                        sql += t.ToString();
                    }
                    else
                        sql += v.ToString();
                    sql += ",";
                }
                sql = sql.TrimEnd(',');
                sql += " where Rid=";
                sql += value.Rid.ToString();
                sql += ";";
                DbCommand.CommandText = sql;
                DbCommand.Parameters.Clear();
            }
            return DbCommand;
        }

        public override DbCommand GetAppendCommand(ModelBase value, ModelMapping mapping)
        {
            return GetInsertCommand(value, mapping);
        }

        private string GetDefaultValue(string typeName)
        {
            switch (int.Parse(typeName))
            {
                case 4:
                    return default(int).ToString();
                case 12:
                    return "''";
                case 6:
                    return default(float).ToString();
                case 8:
                    return default(double).ToString();
                case 5:
                    return default(short).ToString();
                case -7:
                    return default(bool).ToString();
                case -5:
                    return default(long).ToString();
                case -6:
                    return default(byte).ToString();
                default:
                    return "''";
            }
        }

        protected override string CreateUpdateSqlString(ModelBase value, ModelMapping mapping)
        {
            string sql = "update ";
            sql += mapping.TableName;
            sql += " set ";
            foreach (PropertyFieldPair pair in mapping.PropertyFields)
            {
                sql += pair.FieldName;
                sql += "=";
                sql += "?";
                sql += ",";
            }
            sql = sql.TrimEnd(',');
            sql += " where Rid=";
            sql += value.Rid.ToString();
            sql += ";";
            return sql;
        }

        protected override string CreateDeleteSqlString(ModelBase value, ModelMapping mapping)
        {
            string sql = "delete from ";
            sql += mapping.TableName;
            sql += " where Rid = ";
            sql += value.Rid;
            return sql;
        }

        protected override DbParameter GetParameter(object value, PropertyFieldPair pair)
        {
            DbParameter parameter = DbCommand.CreateParameter();
            parameter.Value = pair.Property.GetValue(value, null);
            if (parameter.Value == null)
                parameter.Value = string.Empty;
            else if (pair.Property.PropertyType == typeof(DateTime))
            {
                DateTime dt = (DateTime)parameter.Value;
                TimeSpan ts = dt.Subtract(new DateTime(1970, 1, 1));
                parameter.Value = Convert.ToInt64(ts.TotalMilliseconds);
            }
            parameter.ParameterName = "?";
            return parameter;
        }

        internal override int GetMaxRid(string tableName)
        {
            string sql = " select Rid from " + tableName + "  order by Rid desc limit 1";
            DbCommand.CommandText = sql;
            object obj = DbCommand.ExecuteScalar();
            if (obj == null || obj == DBNull.Value)
                return 1;
            int rid = int.Parse(obj.ToString()) + 1;
            return rid;
        }

        private DbConnection m_connection;
        private DbCommand m_command;

        internal override DbConnection DbConnection
        {
            get { return m_connection; }
        }

        internal override DbCommand DbCommand
        {
            get { return m_command; }
        }

        protected override void Initialize(string providerName, string connectionString)
        {
            m_connection = new OdbcConnection();
            m_connection.ConnectionString = connectionString;
            m_command = new OdbcCommand();
            m_command.Connection = m_connection;

        }
    }
}
