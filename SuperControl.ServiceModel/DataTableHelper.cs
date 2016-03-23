using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace SuperControl.ServiceModel
{
    public static class DataTableHelper
    {
        public static SCDataTable GetData(this DataTable dt)
        {
            SCDataTable dsd = new SCDataTable();
            if (string.IsNullOrWhiteSpace(dt.TableName))
                throw new InvalidCastException("无效的表明");
            dsd.TableName = dt.TableName;
            SCMetaData md = new SCMetaData();
            //提取元数据信息
            foreach (DataColumn col in dt.Columns)
            {
                SCField f = new SCField();
                f.Caption = col.Caption;
                f.DataType = col.DataType.ToString();
                f.Expression = col.Expression;
                f.FiledName = col.ColumnName;
                f.IsKey = col.Unique;
                f.IsReadOnly = col.ReadOnly;
                f.IsRequire = !col.AllowDBNull;
                f.MaxLength = col.MaxLength;
                md.AddField(f);
            }
            dsd.MetaData = md;
            //装数据
            List<List<object>> datas = new List<List<object>>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                List<object> rowData = new List<object>();
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    rowData.Add(dt.Rows[i][j]);
                }
                datas.Add(rowData);
            }
            dsd.Datas = datas;
            return dsd;
        }

        public static void SetData(this DataTable dt, SCDataTable scDataTable)
        {
            dt.Rows.Clear();
            dt.Columns.Clear();

            dt.TableName = scDataTable.TableName;
            foreach (SCField field in scDataTable.MetaData.Fields)
            {
                DataColumn col = new DataColumn();
                col.Caption = field.Caption;
                col.DataType = Type.GetType(field.DataType);
                col.Expression = field.Expression;
                col.ColumnName = field.FiledName;
                col.Unique = field.IsKey;
                col.ReadOnly = field.IsReadOnly;
                col.AllowDBNull = !field.IsRequire;
                col.MaxLength = field.MaxLength;
                dt.Columns.Add(col);
            }
            //装数据
            foreach (List<object> values in scDataTable.Datas)
            {
                dt.Rows.Add(values.ToArray());
            }
        }
    }
}
