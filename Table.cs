using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Reflection;
using System.Data;


namespace TDX
{
    public class Table
    {
        public SqlConnection gbl_conn;
        TDX.db _db = new TDX.db();
        public string table_name { get; set; }
        /// <summary>
        /// editable columns
        /// </summary>
        public List<string> edit_columns { get; set; }
        List<string> exclude_cols { get; set; }
        /// <summary>
        /// primary keys string
        /// </summary>
        public string primary_keys { get; set; }
        public string PK_dataTypes { get; set; }
        /// <summary>
        /// primary key list
        /// </summary>
        public List<string> primary_key_list { get; set; }
        /// <summary>
        /// primary keys data types list
        /// </summary>
        public List<string> PK_dataTypes_list { get; set; }
        public DataTable table_data { get; set; }
        public List<string> list_numeric_types { get; set; }
        public string custom_pks { get; set; }
        public List<string> custom_pks_list { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="table_name"></param>
        /// <param name="conn"></param>
        /// <param name="where_clause"></param>
        /// <param name="exclude_columns"></param>
        /// <param name="pks">set this if you dont want the select latest to use the tables PKs, and base the select off of a custom PK</param>
        /// <param name="select_data"></param> if you want to select_latest from this object or not
        public Table(string table_name,SqlConnection conn, string where_clause = "",List<string> exclude_columns=null,string pks = "",bool select_data=true)
        {
            if (pks != "") { this.custom_pks = pks; } else { this.custom_pks = ""; }
            this.table_name = table_name;
            if (exclude_columns != null)
            {
                this.exclude_cols = exclude_columns;
            }
            else { this.exclude_cols = new List<string>(); }
            this.gbl_conn = conn;
            if (select_data)
            {
                this.select_latest(where_clause);
                this.edit_columns = this.get_updatable_columns();
            }
            
            list_numeric_types = new List<string>();
            populate_numericTypes();
        }
        
        public DataTable select()
        {
            DataTable dtOut = new DataTable();
            string sqlText = "SELECT * FROM " + this.table_name;
            dtOut = _db.execSQL(sqlText, gbl_conn);
            return dtOut;
        }
        
        public bool isNumericType(string data_type)
        {
            bool bln = false;
            if(list_numeric_types.Contains(data_type))
            {
                bln = true;
            }
            return bln;
        }
        
        public Table select_latest(string where_clause="")
        {
            DataTable dtOut = new DataTable();
            this.primary_keys = get_primKeys();
            this.primary_key_list = this.primary_keys.Split(',').ToList();
            if(this.custom_pks != "")
            {
                this.custom_pks_list = this.custom_pks.Split(',').ToList();
                this.primary_key_list = this.primary_key_list.Except(this.custom_pks_list).ToList();
            }
            this.PK_dataTypes = get_PK_data_types();
            this.PK_dataTypes_list = this.PK_dataTypes.Split(',').ToList();
            string[,] arrParams = new string[3, 2];
            arrParams[0, 0] = "@table";
            arrParams[0, 1] = this.table_name;
            arrParams[1, 0] = "@pks";
            arrParams[1, 1] = this.primary_keys;
            arrParams[2, 0] = "@where";
            arrParams[2, 1] = where_clause;
            this.table_data = _db.getSprocresults("select_latest", arrParams, gbl_conn);
            this.table_data.Columns.Remove("rownum");
            return this;
        }
        /// <summary>
        /// gets a list of all the table columns in the correct ordinal positions(order)
        /// </summary>
        /// <returns></returns>
        public List<string> get_table_columns()
        {
            List<string> list_tablecol = new List<string>();
            DataTable dt = new DataTable();
            string sql_text = "SELECT column_name FROM INFORMATION_SCHEMA.COLUMNS WHERE table_name  = '" + this.table_name + "' ";
            sql_text += " ORDER BY ordinal_position";
            dt = _db.execSQL(sql_text, gbl_conn);
            list_tablecol =  dt.AsEnumerable().Select(row => row["COLUMN_NAME"].ToString()).Distinct().ToList();
            return list_tablecol;
        }
        
        public DataTable get_table_schema()
        {
            DataTable dtOut = new DataTable();
            string sqlText = "select string_agg(C.COLUMN_NAME,',') as col_name,string_agg(col.DATA_TYPE,',') as data_type FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS T JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE C";
            sqlText += " ON C.CONSTRAINT_NAME = T.CONSTRAINT_NAME ";
            sqlText += " INNER JOIN INFORMATION_SCHEMA.COLUMNS col on col.COLUMN_NAME = c.COLUMN_NAME and col.TABLE_Name = c.TAble_name";
            sqlText += " WHERE C.TABLE_NAME = '" + this.table_name + "'";
            sqlText += " AND col.COLUMN_NAME != 'runtime'";
            sqlText += " and T.CONSTRAINT_TYPE = 'PRIMARY KEY'";
            dtOut = _db.execSQL(sqlText, gbl_conn);

            return dtOut;
        }
        
        public string get_primKeys()
        {
            string pks = "";
            DataTable dtOut = new DataTable();
            dtOut = get_table_schema();
            if (dtOut.Rows.Count > 0)
            {
                pks = dtOut.Rows[0][0].ToString();
            }
            return pks;
        }
        
        public string get_PK_data_types()
        {
            string types = "";
            DataTable dtOut = new DataTable();
            dtOut = get_table_schema();
            types = dtOut.Rows[0][1].ToString();
            return types;
        }
        
        public void load_dataTable(DataTable dtIn)
        {
            gbl_conn.Open();
            var transaction = gbl_conn.BeginTransaction();
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(gbl_conn,SqlBulkCopyOptions.TableLock, transaction))
            {
               
                bulkCopy.DestinationTableName = "dbo." + this.table_name;
                try
                {
                    bulkCopy.ColumnMappings.Clear();
                    foreach(DataColumn col in dtIn.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(col.ColumnName,col.ColumnName));
                        Console.WriteLine(col.ColumnName);
                    }
                    bulkCopy.WriteToServer(dtIn); 
                    
                }
                catch(Exception ex)
                {
                    Console.Write(ex.Message);
                }
                finally
                {
                    transaction.Commit();
                    gbl_conn.Close();
                }
            }
        }
        
        public List<string> get_updatable_columns()
        {
            List<string> edit_cols = new List<string>();

            foreach(DataColumn column in this.table_data.Columns)
            {
                if(!this.primary_key_list.Contains(column.ColumnName) && (column.ColumnName!="runtime") &&(!this.exclude_cols.Contains(column.ColumnName )))
                {
                    edit_cols.Add(column.ToString());
                }
            }
            return edit_cols;
        }
        
        private void populate_numericTypes()
        {
            list_numeric_types.Add("bigint");
            list_numeric_types.Add("bit");
            list_numeric_types.Add("decimal");
            list_numeric_types.Add("int");
            list_numeric_types.Add("money");
            list_numeric_types.Add("float");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="table_name"></param>
        /// <param name="where_clause">where clause must have the full where statement.i.e WHERE column_name = value</param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static (bool,string)  deleteFromTable(string table_name,string where_clause, SqlConnection conn)
        {
            bool blnOut = false;
            string sql_string = "DELETE FROM " + table_name;
            sql_string += where_clause;
            string error = "";
            try
            {
                TDX.db db = new TDX.db();
                db.execSQL(sql_string, conn);

            }
            catch(Exception ex)
            {
                error = ex.Message;
            }
 
            return (blnOut,error);
            
        }
    }
}
