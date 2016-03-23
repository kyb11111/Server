using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace SuperControl.ServiceModel
{
    public class ModelCacheManager
    {
        private Dictionary<string, ModelCollection> typeDictionary = new Dictionary<string, ModelCollection>();
        /* 田濛2012/5/15 添加用于根据表明找到集合的字典 */
        private Dictionary<string, ModelCollection> tableDictionary = new Dictionary<string, ModelCollection>();
        private static ModelCacheManager instance;

        /// <summary>
        /// 声明为受保护的构造函数
        /// </summary>
        protected ModelCacheManager()
        {
        }

        /// <summary>
        /// 获取模型对象存根管理器的实例
        /// </summary>
        public static ModelCacheManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ModelCacheManager();
                return instance;
            }
        }

        /// <summary>
        /// 存储一个模型对象，如果存根管理器中没有该对象则增加该对象
        /// </summary>
        /// <param name="value">要存储的模型对象</param>
        public virtual void Save(ModelBase value)
        {
            Type type = value.GetType();
            ModelCollection modelCollection = this[type];
            if (modelCollection == null)
                return;
            if (modelCollection.Contains(value.Rid))
            {
                bool altKeyChanged = false;
                ModelBase model = modelCollection[value.Rid];
                if (model.AlternateKey != value.AlternateKey)
                {
                    modelCollection.RemoveAltKey(model);
                    altKeyChanged = true;
                }
                ModelMapping mf = modelCollection.ModelField;
                foreach (PropertyFieldPair pfp in mf.PropertyFields)
                {
                    object obj = pfp.Property.GetValue(value, null);
                    pfp.Property.SetValue(model, obj, null);
                }
                while (mf.Parent != null)
                {
                    foreach (PropertyFieldPair pfp in mf.Parent.PropertyFields)
                    {
                        object obj = pfp.Property.GetValue(value, null);
                        pfp.Property.SetValue(model, obj, null);
                    }
                    mf = mf.Parent;
                }
                if (altKeyChanged)
                    modelCollection.AddAltKey(value);
            }
            else
            {
                modelCollection.Add(value);
                modelCollection.AddAltKey(value);
                while (modelCollection.ModelField.Parent != null)
                {
                    modelCollection = this[modelCollection.ModelField.Parent.ModelType];
                    if (modelCollection.Contains(value.Rid))
                    {
                        ModelBase model = modelCollection[value.Rid];
                        ModelMapping mf = modelCollection.ModelField;
                        foreach (PropertyFieldPair pfp in mf.PropertyFields)
                        {
                            object obj = pfp.Property.GetValue(model, null);
                            pfp.Property.SetValue(value, obj, null);
                        }
                        modelCollection.Remove(value.Rid);
                        modelCollection.RemoveAltKey(value);
                    }
                    modelCollection.Add(value);
                    modelCollection.AddAltKey(value);
                    type = type.BaseType;
                }
            }
        }

        /// <summary>
        /// 存储多个模型对象
        /// </summary>
        /// <param name="values">要存储的模型对象集合的枚举器</param>
        public void SaveRange(IEnumerable<ModelBase> values)
        {
            foreach (ModelBase value in values)
            {
                Save(value);
            }
        }

        /// <summary>
        /// 移除一个模型对象
        /// </summary>
        /// <param name="value">要移除的模型对象</param>
        public virtual void Remove(ModelBase value)
        {
            Type type = value.GetType();
            while (type != null)
            {
                ModelCollection modelCollection = this[type.Name];
                if (modelCollection != null && modelCollection.Contains(value.Rid))
                {
                    ModelBase model = modelCollection[value.Rid];
                    modelCollection.Remove(model);
                    modelCollection.RemoveAltKey(model);
                }
                if (modelCollection.ModelField.Parent != null)
                    type = modelCollection.ModelField.Parent.ModelType;
                else
                    type = null;
            }
        }

        /// <summary>
        /// 移除多个模型对象
        /// </summary>
        /// <param name="values">要移除的模型对象集合的枚举器</param>
        public void RemoveRange(IEnumerable<ModelBase> values)
        {
            foreach (ModelBase value in values)
            {
                Remove(value);
            }
        }

        /// <summary>
        /// 获取模型对象的字典集合，如果没有该集合则创建该字典集合以及此类对象的父类模型对象的字典集合
        /// </summary>
        /// <param name="typeName">模型对象的类型名称</param>
        /// <returns>模型对象字典集合</returns>
        public ModelCollection this[string typeName]
        {
            get
            {
                ModelCollection modelCollection;
                typeDictionary.TryGetValue(typeName, out modelCollection);
                //if (!typeDictionary.TryGetValue(typeName, out modelCollection))
                //{
                //    foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                //    {
                //        foreach (Type t in asm.GetTypes())
                //        {
                //            if (t.Name == typeName)
                //                return this[t];
                //        }
                //    }
                //}
                return modelCollection;
            }
        }

        /// <summary>
        /// 获取模型对象的字典集合，如果没有该集合则创建该字典集合以及此类对象的父类模型对象的字典集合
        /// </summary>
        /// <param name="type">模型对象的类型</param>
        /// <returns>模型对象字典集合</returns>
        public ModelCollection this[Type type]
        {
            get
            {
                ModelCollection modelCollection;
                if (!typeDictionary.TryGetValue(type.Name, out modelCollection))
                {
                    modelCollection = CreateModelCollection(type);
                }
                return modelCollection;
            }
        }

        /// <summary>
        /// 获取一个模型对象
        /// </summary>
        /// <param name="typeName">模型对象的类型名称</param>
        /// <param name="rid">模型对象的Rid</param>
        /// <returns>模型对象</returns>
        public ModelBase this[string typeName, int rid]
        {
            get
            {
                try
                {
                    ModelCollection modelDictionary = this[typeName];
                    return modelDictionary[rid];
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 获取一个模型对象
        /// </summary>
        /// <param name="type">模型对象的类型</param>
        /// <param name="rid">模型对象的Rid</param>
        /// <returns>模型对象</returns>
        public ModelBase this[Type type, int rid]
        {
          
            get
            { 
                try
                {
                  ModelCollection modelDictionary = this[type];
                  return modelDictionary[rid];
                }
                catch(Exception e)
                {
                    return null;
                }
            }
        
        }

        /// <summary>
        /// 获取一个模型对象
        /// </summary>
        /// <param name="typeName">模型对象的类型名称</param>
        /// <param name="altKey">模型对象的可选索引键</param>
        /// <returns>模型对象</returns>
        public ModelBase this[string typeName, string altKey]
        {
            get
            {
                try
                {
                    ModelCollection modelDictionary = this[typeName];
                    return modelDictionary[altKey];
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 获取一个模型对象
        /// </summary>
        /// <param name="type">模型对象的类型</param>
        /// <param name="altKey">模型对象的可选索引键</param>
        /// <returns>模型对象</returns>
        public ModelBase this[Type type, string altKey]
        {
            get
            {
                try
                {
                    ModelCollection modelDictionary = this[type];
                    return modelDictionary[altKey];
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 通过远程传输的模型对象获取本地对应的模型对象
        /// </summary>
        /// <param name="value">远程传输来的模型对象</param>
        /// <returns>本地模型对象</returns>
        public ModelBase this[ModelBase value]
        {
            get
            {
                ModelCollection modelDictionary = this[value.GetType()];
                if (modelDictionary != null && modelDictionary.Contains(value.Rid))
                    return modelDictionary[value.Rid];
                return null;
            }
        }

        /// <summary>
        /// 返回指定类型的对象列
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        /// <returns>对象集合索引器</returns>
        public static IEnumerable<T> GetList<T>() where T : ModelBase
        {
            return from model in Instance[typeof(T)] select model as T;
        }

        /// <summary>
        /// 返回指定类型的对象列
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        /// <returns>对象集合索引器</returns>
        public static T GetModel<T>(int rid) where T : ModelBase
        {
            return Instance[typeof(T), rid] as T;
        }

        /// <summary>
        /// 创建对象集合
        /// </summary>
        /// <param name="type">指定类型</param>
        /// <returns></returns>
        internal ModelCollection CreateModelCollection(Type type)
        {
            if (!type.IsSubclassOf(typeof(ModelBase)))
                return null;
            ModelCollection modelCollection;
            if (!typeDictionary.TryGetValue(type.Name, out modelCollection))
            {
                modelCollection = new ModelCollection();
                modelCollection.ModelType = type;
                typeDictionary.Add(type.Name, modelCollection);
                /* 田濛2012/5/15 添加插入表明字典 */
                DbTableAttribute dbTableAttribute = Attribute.GetCustomAttribute(type,typeof(DbTableAttribute)) as DbTableAttribute;
                if (dbTableAttribute != null)
                {
                    if (!string.IsNullOrWhiteSpace(dbTableAttribute.TableName))
                        tableDictionary.Add(dbTableAttribute.TableName, modelCollection);
                    else
                        tableDictionary.Add(type.Name, modelCollection);
                }
                /////////////////////////
                if (modelCollection.ModelField.Parent != null)
                    modelCollection.BaseCollection = CreateModelCollection(modelCollection.ModelField.Parent.ModelType);
                return modelCollection;
            }
            return modelCollection;
        }

        /* 田濛2012/5/15 添加 */
        /// <summary>
        /// 清空存根
        /// </summary>
        public void Clear()
        {
            foreach (ModelCollection collection in typeDictionary.Values)
                collection.Clear();
            typeDictionary.Clear();
            tableDictionary.Clear();
        }

        /// <summary>
        /// 通过表名返回模型对象集合
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>模型对象集合</returns>
        public ModelCollection GetListWithTableName(string tableName)
        {
            ModelCollection collection;
            tableDictionary.TryGetValue(tableName, out collection);
            return collection;
        }

        /// <summary>
        /// 通过表名及Rid返回模型对象
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="rid">Rid</param>
        /// <returns>模型对象</returns>
        public ModelBase GetModelWithTableName(string tableName,int rid)
        {
            ModelCollection collection;
            tableDictionary.TryGetValue(tableName, out collection);
            if (collection != null)
            {
                if (collection.Contains(rid))
                    return collection[rid];
            }
            return null;
        }

        /// <summary>
        /// 通过表名创建一个新模型对象
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>创建的模型对象</returns>
        public ModelBase CreateNew(string tableName)
        {
            try
            {
                ModelCollection collection;
                tableDictionary.TryGetValue(tableName, out collection);
                if (collection != null)
                {
                    return Activator.CreateInstance(collection.ModelType) as ModelBase;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            return null;
        }
    }
}
