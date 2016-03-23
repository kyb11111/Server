using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SuperControl.ServiceModel;

namespace SuperControl.TraceServerModel
{
  [Serializable, DataContract]
  [DbTable(HasAlternateKey = false,CacheMode = CacheMode.Both,TableName = "TraceInfo",Realtime = false)]
  public class TraceInfo : TraceModel
  {
       private String m_Name;
       [DataMember, DbField(FieldName = "Name")]
       public String Name
       {
           get { return m_Name; }
           set
           {
              if (m_Name != value)
              {
                  m_Name = value;
                  OnPropertyChanged("Name");
              }
           }
       }

       private Int32 m_Product;
       [DataMember, DbField(FieldName = "Product")]
       public Int32 Product
       {
           get { return m_Product; }
           set
           {
              if (m_Product != value)
              {
                  m_Product = value;
                  OnPropertyChanged("Product");
              }
           }
       }

       private DateTime m_DateTime;
       [DataMember, DbField(FieldName = "DateTime")]
       public DateTime DateTime
       {
           get { return m_DateTime; }
           set
           {
              if (m_DateTime != value)
              {
                  m_DateTime = value;
                  OnPropertyChanged("DateTime");
              }
           }
       }

       private String m_TextInfo;
       [DataMember, DbField(FieldName = "TextInfo")]
       public String TextInfo
       {
           get { return m_TextInfo; }
           set
           {
              if (m_TextInfo != value)
              {
                  m_TextInfo = value;
                  OnPropertyChanged("TextInfo");
              }
           }
       }

       private String m_PicInfo;
       [DataMember, DbField(FieldName = "PicInfo")]
       public String PicInfo
       {
           get { return m_PicInfo; }
           set
           {
              if (m_PicInfo != value)
              {
                  m_PicInfo = value;
                  OnPropertyChanged("PicInfo");
              }
           }
       }

      public override void SetValueWithTableName(string fieldName, object value, out bool sendImmediately)
      {
          base.SetValueWithTableName(fieldName, value, out sendImmediately);
          switch (fieldName)
          {
              case "Name":
                  if (!m_Name.Equals(value))
                  {
                      m_Name = (System.String)ModelDatabaseEnumerator.ChangeType(value, typeof(System.String));
                      sendImmediately = true;
                      OnPropertyChanged("Name");
                  }
                  break;
              case "Product":
                  if (!m_Product.Equals(value))
                  {
                      m_Product = (System.Int32)ModelDatabaseEnumerator.ChangeType(value, typeof(System.Int32));
                      sendImmediately = true;
                      OnPropertyChanged("Product");
                  }
                  break;
              case "DateTime":
                  if (!m_DateTime.Equals(value))
                  {
                      m_DateTime = (System.DateTime)ModelDatabaseEnumerator.ChangeType(value, typeof(System.DateTime));
                      sendImmediately = true;
                      OnPropertyChanged("DateTime");
                  }
                  break;
              case "TextInfo":
                  if (!m_TextInfo.Equals(value))
                  {
                      m_TextInfo = (System.String)ModelDatabaseEnumerator.ChangeType(value, typeof(System.String));
                      sendImmediately = true;
                      OnPropertyChanged("TextInfo");
                  }
                  break;
              case "PicInfo":
                  if (!m_PicInfo.Equals(value))
                  {
                      m_PicInfo = (System.String)ModelDatabaseEnumerator.ChangeType(value, typeof(System.String));
                      sendImmediately = true;
                      OnPropertyChanged("PicInfo");
                  }
                  break;
              default:
                  sendImmediately = false;
                  break;
          }
      }
  }
}

