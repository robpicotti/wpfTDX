using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace wpfTDX
{
    public class Util
    {
        public static void DataTableToCSV(DataTable dt,string nameOfFile)
        {
            StringBuilder sb = new StringBuilder();

            string[] columnNames = dt.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName).
                                              ToArray();
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                string[] fields = row.ItemArray.Select(field => field.ToString()).
                                                ToArray();
                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText(@"c:\TDX\" + nameOfFile, sb.ToString());
        }
        public static DataTable ReorderColumns(DataTable originalTable, string[] desiredColumnOrder)
        {
            // Create a new DataTable to store the reordered columns
            DataTable reorderedTable = new DataTable();

            // Add the columns in the desired order
            foreach (string columnName in desiredColumnOrder)
            {
                if (originalTable.Columns.Contains(columnName))
                {
                    reorderedTable.Columns.Add(columnName, originalTable.Columns[columnName].DataType);
                }
            }

            // Now, add the rows from the original DataTable into the new DataTable
            foreach (DataRow row in originalTable.Rows)
            {
                DataRow newRow = reorderedTable.NewRow();
                foreach (string columnName in desiredColumnOrder)
                {
                    if (originalTable.Columns.Contains(columnName))
                    {
                        newRow[columnName] = row[columnName];
                    }
                }
                reorderedTable.Rows.Add(newRow);
            }

            return reorderedTable;
        }


        public static DataTable ConvertJsonArrayToDataTable(string jsonArray)
        {
            DataTable dataTable = new DataTable();
            JArray array = JArray.Parse(jsonArray);

            if (array.Count == 0)
                return dataTable;

            // Create columns based on the first row
            foreach (var column in array[0].ToObject<JObject>())
            {
                dataTable.Columns.Add(column.Key);
            }

            // Add rows
            foreach (JObject row in array)
            {
                DataRow dataRow = dataTable.NewRow();
                foreach (var column in row)
                {
                    dataRow[column.Key] = column.Value.ToString(); // Use column.Key and column.Value
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }
    }
}
