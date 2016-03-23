using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SuperControl.ServiceModel;

namespace SuperControl.TraceServerModel
{
  [Serializable, DataContract]
  [DbTable(HasAlternateKey = false,CacheMode = CacheMode.Both,TableName = "ProductType",Realtime = false)]
  public class ProductType : TraceModel
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
              case "Remark":
                  if (!m_Remark.Equals(value))
                  {
                      m_Remark = (System.String)ModelDatabaseEnumerator.ChangeType(value, typeof(System.String));
                      sendImmediately = true;
                      OnPropertyChanged("Remark");
                  }
                  break;
              default:
                  sendImmediately = false;
                  break;
          }
      }
  }
}

