using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data.Common;
using System.Reflection;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;

namespace SuperControl.ServiceModel
{
    public class GeneralModelFactory : ModelFactory
    {
        private DbConnection m_connection;
        private DbCommand m_command;
        private DbProviderFactory m_provider;

        protected override void Initialize(string providerName, string connectionString)
        {
            int startIndex = providerName.LastIndexOf('.');
            string assemblyName = providerName.Remove(startIndex);
            string typeName = string.Format("{0}{1}Factory", providerName, providerName.Substring(startIndex));
            Assembly assembly = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assemblyName + ".dll";
                if (name == asm.ManifestModule.Name)
                {
                    assembly = asm;
                    break;
                }
            }
            if (assembly == null)
                assembly = Assembly.Load(assemblyName);
            Type type = assembly.GetType(typeName);
            FieldInfo field = type.GetField("Instance", BindingFlags.Static | BindingFlags.Public);
            m_provider = field.GetValue(null) as DbProviderFactory;
            if (m_provider is MySqlClientFactory)
            {
                this.EscapeExpr = "`{0}`";
            }
            else if (m_provider is SqlClientFactory)
            {
                this.EscapeExpr = "[{0}]";
            }
            m_connection = m_provider.CreateConnection();
            m_connection.ConnectionString = connectionString;
            m_command = m_provider.CreateCommand();
            m_command.Connection = m_connection;
        }

        public DbProviderFactory Provider
        {
            get { return m_provider; }
        }

        internal override DbConnection DbConnection
        {
            get { return m_connection; }
        }

        internal override DbCommand DbCommand
        {
            get { return m_command; }
        }
    }
}
