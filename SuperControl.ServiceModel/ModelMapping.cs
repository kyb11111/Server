using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace SuperControl.ServiceModel
{
    public class PropertyFieldPair
    {
        public string FieldName
        {
            get;
            set;
        }

        public PropertyInfo Property
        {
            get;
            set;
        }
    }
    //
    public class ModelMapping
    {
        private bool m_init;
        private ModelMapping m_parent;
        private ModelMapping m_root;
        private Type m_type;
        private string m_table;
        private PropertyFieldPair[] m_field;

        public ModelMapping(Type modelType)
        {
            m_init = false;
            m_type = modelType;
            m_field = GetFields(modelType);
            m_table = GetTableName(modelType);
        }

        public Type ModelType
        {
            get { return m_type; }
        }

        public string TableName
        {
            get { return m_table; }
        }

        public PropertyFieldPair[] PropertyFields
        {
            get { return m_field; }
        }

        public ModelMapping Parent
        {
            get
            {
                if (!m_init)
                {
                    if (GetTableName(m_type.BaseType) != string.Empty)
                        m_parent = new ModelMapping(m_type.BaseType);
                    m_init = true;
                }
                return m_parent;
            }
        }

        public ModelMapping Root
        {
            get
            {
                if (m_root == null)
                {
                    m_root = this;
                    while (m_root.Parent != null)
                    {
                        m_root = m_root.Parent;
                    }
                }
                return m_root;
            }
        }

        public static PropertyFieldPair[] GetFields(Type modelType)
        {
            List<PropertyFieldPair> list = new List<PropertyFieldPair>();
            PropertyInfo[] properties = modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (PropertyInfo property in properties)
            {
                DbFieldAttribute attribute = Attribute.GetCustomAttribute(property, typeof(DbFieldAttribute)) as DbFieldAttribute;
                if (attribute != null)
                {
                    MethodInfo ms = property.GetSetMethod();
                    MethodInfo mg = property.GetGetMethod();
                    if (ms != null && !ms.IsAbstract &&
                        mg != null && !mg.IsAbstract)
                    {
                        PropertyFieldPair mf = new PropertyFieldPair();
                        if (string.IsNullOrWhiteSpace(attribute.FieldName))
                            mf.FieldName = property.Name;
                        else
                            mf.FieldName = attribute.FieldName;
                        mf.Property = property;
                        list.Add(mf);
                    }
                }
            }
            return list.ToArray();
        }

        public static string GetTableName(Type modelType)
        {
            DbTableAttribute attribute = Attribute.GetCustomAttribute(modelType, typeof(DbTableAttribute)) as DbTableAttribute;
            if (attribute == null)
                return string.Empty;
            else if (!string.IsNullOrWhiteSpace(attribute.TableName))
                return attribute.TableName;
            else
                return modelType.Name;
        }
    }
}
