using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SuperControl.ServiceModel;

namespace SuperControl.TraceServerModel
{
  [Serializable, DataContract]
  [DbTable(HasAlternateKey = false,CacheMode = CacheMode.Both,TableName = "Product",Realtime = false)]
  public class Product : TraceModel
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

       private Int32 m_ProductType;
       [DataMember, DbField(FieldName = "ProductType")]
       public Int32 ProductType
       {
           get { return m_ProductType; }
           set
           {
              if (m_ProductType != value)
              {
                  m_ProductType = value;
                  OnPropertyChanged("ProductType");
              }
           }
       }

       private Int32 m_UserInfo;
       [DataMember, DbField(FieldName = "UserInfo")]
       public Int32 UserInfo
       {
           get { return m_UserInfo; }
           set
           {
              if (m_UserInfo != value)
              {
                  m_UserInfo = value;
                  OnPropertyChanged("UserInfo");
              }
           }
       }

       private String m_Remark;
       [DataMember, DbField(FieldName = "Remark")]
       public String Remark
       {
           get { return m_Remark; }
           set
           {
              if (m_Remark != value)
              {
                  m_Remark = value;
                  OnPropertyChanged("Remark");
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

       private String m_QrCordeInfo;
       [DataMember, DbField(FieldName = "QrCordeInfo")]
       public String QrCordeInfo
       {
           get { return m_QrCordeInfo; }
           set
           {
              if (m_QrCordeInfo != value)
              {
                  m_QrCordeInfo = value;
                  OnPropertyChanged("QrCordeInfo");
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
              case "ProductType":
                  if (!m_ProductType.Equals(value))
                  {
                      m_ProductType = (System.Int32)ModelDatabaseEnumerator.ChangeType(value, typeof(System.Int32));
                      sendImmediately = true;
                      OnPropertyChanged("ProductType");
                  }
                  break;
              case "UserInfo":
                  if (!m_UserInfo.Equals(value))
                  {
                      m_UserInfo = (System.Int32)ModelDatabaseEnumerator.ChangeType(value, typeof(System.Int32));
                      sendImmediately = true;
                      OnPropertyChanged("UserInfo");
                  }
                  break;
              case "Remark":
                  if (!m_Remark.Equals(value))
                  {
                      m_Remark = (System.String)ModelDatabaseEnumerator.ChangeType(value, typeof(System.String));
                      sendImmediately = true;
                      OnPropertyChanged("Remark");
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
              case "QrCordeInfo":
                  if (!m_QrCordeInfo.Equals(value))
                  {
                      m_QrCordeInfo = (System.String)ModelDatabaseEnumerator.ChangeType(value, typeof(System.String));
                      sendImmediately = true;
                      OnPropertyChanged("QrCordeInfo");
                  }
                  break;
              default:
                  sendImmediately = false;
                  break;
          }
      }
  }
}

