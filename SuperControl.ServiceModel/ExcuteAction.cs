using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SuperControl.ServiceModel
{
    [Serializable, DataContract]
    public enum ExcuteType : byte
    {
        [EnumMember]
        Insert = 0x01,
        [EnumMember]
        Delete = 0x02,
        [EnumMember]
        Update = 0x03,
        [EnumMember]
        CacheSave = 0x04,
        [EnumMember]
        CacheRemove = 0x05,
        [EnumMember]
        Registe = 0x06,
        [EnumMember]
        Unregiste = 0x07,
        [EnumMember]
        GetLog = 0x08,
        [EnumMember]
        Append = 0x09,
        [EnumMember]
        Select = 0x10
    }

    [Serializable, DataContract]
    public class ExcuteAction
    {
        [DataMember]
        public ExcuteType ExcuteType
        {
            get;
            set;
        }

        [DataMember]
        public object ExcuteObject
        {
            get;
            set;
        }

        public void Serialize(Stream stream)
        {
            stream.WriteByte((byte)ExcuteType);
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, ExcuteObject);
            stream.Flush();
        }

        public void Deserialize(Stream stream)
        {
            ExcuteType = (ExcuteType)stream.ReadByte();
            IFormatter formatter = new BinaryFormatter();
            ExcuteObject = formatter.Deserialize(stream) as ModelBase;
        }
    }
}
