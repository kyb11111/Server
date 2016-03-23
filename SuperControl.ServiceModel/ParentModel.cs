using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace SuperControl.ServiceModel
{
    /// <summary>
    /// 获取多对一关联中的关联父对象
    /// </summary>
    public class ParentModel
    {
        private ModelBase model;
        /// <summary>
        /// 创建ParentModel对象
        /// </summary>
        /// <param name="value">模型对象</param>
        internal ParentModel(ModelBase value)
        {
            model = value;
        }

        /// <summary>
        /// 获取多对一关联中的关联父对象
        /// </summary>
        /// <param name="parentTypeName">关联父对象类型名称</param>
        /// <returns>关联父对象</returns>
        public ModelBase this[string parentTypeName]
        {
            get
            {
                return this[parentTypeName, parentTypeName];
            }
        }

        /// <summary>
        /// 获取多对一关联中的关联父对象
        /// </summary>
        /// <param name="parentTypeName">关联父对象类型名称</param>
        /// <param name="parentPropertyName">关联父对象对应的成员属性名称</param>
        /// <returns>关联父对象</returns>
        public ModelBase this[string parentTypeName, string parentPropertyName]
        {
            get
            {
                PropertyInfo property = model.GetType().GetProperty(parentPropertyName);
                if (property == null || property.PropertyType != typeof(int))
                    return null;
                int rid = (int)property.GetValue(model, null);
                return ModelCacheManager.Instance[parentTypeName, rid];
            }
        }

        /// <summary>
        /// 获取多对一关联中的关联父对象
        /// </summary>
        /// <param name="parentTypeName">关联父对象类型</param>
        /// <returns>关联父对象</returns>
        public ModelBase this[Type parentType]
        {
            get
            {
                return this[parentType, parentType.Name];
            }
        }

        /// <summary>
        /// 获取多对一关联中的关联父对象
        /// </summary>
        /// <param name="parentTypeName">关联父对象类型</param>
        /// <param name="parentPropertyName">关联父对象对应的成员属性名称</param>
        /// <returns>关联父对象</returns>
        public ModelBase this[Type parentType, string parentPropertyName]
        {
            get
            {
                return this[parentType.Name, parentPropertyName];
            }
        }

        /// <summary>
        /// 获取多对一关联中的关联父对象
        /// </summary>
        /// <typeparam name="T">关联父对象类型</typeparam>
        /// <returns>关联父对象</returns>
        public T GetParent<T>() where T : ModelBase
        {
            return GetParent<T>(typeof(T).Name);
        }

        /// <summary>
        /// 获取多对一关联中的关联父对象
        /// </summary>
        /// <typeparam name="T">关联父对象类型</typeparam>
        /// <param name="parentPropertyName">关联父对象对应的成员属性名称</param>
        /// <returns>关联父对象</returns>
        public T GetParent<T>(string parentPropertyName) where T : ModelBase
        {
            return this[typeof(T), parentPropertyName] as T;
        }
    }
}
