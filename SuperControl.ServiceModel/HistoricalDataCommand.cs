using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SuperControl.ServiceModel
{
    [Serializable, Flags]
    public enum SCHistStatus : ushort
    {
        Normal = 0x0000,       /**数值正常**/
        TopOverFlow = 0x0001,       /**超出上限**/
        BottomOverFlow = 0x0002,       /**超出下限**/
        TransportFailed = 0x0004,       /**通讯失败**/
        ManualInput = 0x0008,       /**手动输入**/
        Updated = 0x0010,       /**修改**/
        NoData = 0x0020,       /**无数据**/
        Calculate = 0x0040,       /**推测,估算**/
        NoStandard = 0x4000,       /**非标况**/
        OutOfService = 0x8000,        /**停役**/
    }
    
    [Serializable]
    public enum HistoryReaderFunc
    {
        Raw = 0x00,
        Sum = 0x01,
        Average = 0x02,
        Min = 0x03,
        Max = 0x04,
        Snapshot = 0x05
    }

    [Serializable]
    public enum SampleDataType
    {
        Ana = 0x01,
        Dig = 0x02,
        AnaNoQuantum = 0x03,
    }

    [Serializable, DataContract]
    [KnownType(typeof(HistoryReaderFunc))]
    [KnownType(typeof(SampleDataType))]
    public class HistoricalDataCommand
    {
        [DataMember(IsRequired = true)]
        public HistoryReaderFunc Func
        {
            get;
            set;
        }
        [DataMember(IsRequired = true)]
        public DateTime StartTime
        {
            get;
            set;
        }
        [DataMember(IsRequired = true)]
        public DateTime EndTime
        {
            get;
            set;
        }
        [DataMember(IsRequired = true)]
        public uint Rid
        {
            get;
            set;
        }
        [DataMember(IsRequired = true)]
        public long Interval
        {
            get;
            set;
        }
        [DataMember(IsRequired = true)]
        public SampleDataType DataType
        {
            get;
            set;
        }

    }

    [Serializable, DataContract]
    [KnownType(typeof(AnaHistoricalData))]
    [KnownType(typeof(DigHistoricalData))]
    public class HistoricalData
    {
        [DataMember(IsRequired = true)]
        public DateTime Time
        {
            get;
            set;
        }

        [DataMember(IsRequired = true)]
        public SCHistStatus State
        {
            get;
            set;
        }
    }

    [Serializable, DataContract]
    public class AnaHistoricalData : HistoricalData
    {
        [DataMember(IsRequired = true)]
        public double Value
        {
            get;
            set;
        }
    }

    [Serializable, DataContract]
    public class DigHistoricalData : HistoricalData
    {
        [DataMember(IsRequired = true)]
        public ushort Value
        {
            get;
            set;
        }
    }
}
