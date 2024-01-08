using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDX;
using System.Data;
using System.Data.SqlClient;

namespace TDX
{
    class Portfolio
    {
        private TDX.db _db = new TDX.db();
        public Portfolio(string portfolioname, SqlConnection conn)
        {
            this.conn = conn;
            Table tbl = new Table("portfolio_weights",this.conn);
            this.portfolio_weight_columns = tbl.get_table_columns();
            tbl = new Table("max_position", this.conn);
            this.max_position_columns = tbl.get_table_columns();
            this.portfolioname = portfolioname;
        }

        public DateTime runtime
        {
            get;
            set;
        }
        public string portfolioname
        {
            get; set;
        }
        public string tickername
        {
            get; set;
        }
        public double weight
        {
            get; set;
        }
        public double max_position
        {
            get; set;
        }
        public string market_neutral
        {
            get; set;
        }
        public SqlConnection conn
        {
            get; set;
        }
        public List<string> portfolio_weight_columns
        {
            get;set;
        }
        public List<string> max_position_columns
        {
            get;set;
        }
        /// <summary>
        /// gets the portfolio details 
        /// </summary>
        /// <returns></returns>
        public DataTable Get()
        {
            DataTable dtOut = new DataTable();
            DataSet ds = new DataSet();
            Dictionary<string, string> arrParams = new Dictionary<string, string>();
            arrParams.Add("@portfolioname", this.portfolioname);
            ds = _db.getSprocResults("get_portfoliodetails", arrParams, this.conn);
            dtOut = ds.Tables[0];

            return dtOut;
        }
        public bool Add(DataTable dtPortfolio)
        {
            bool ret = false;
            add_portfolio_weights(dtPortfolio.Copy());
            add_max_positions(dtPortfolio.Copy());

            return ret;
        }
        private bool add_portfolio_weights(DataTable dtPortfolio)
        {
            bool ret = false;
            List<DataColumn> delete_cols = new List<DataColumn>();
            //remove any columns that dont belong to the portfolioweights table
            foreach (DataColumn col in dtPortfolio.Columns)
            {
                if(!this.portfolio_weight_columns.Contains(col.ColumnName))
                {
                    //dtPortfolio.Columns.Remove(col);
                    delete_cols.Add(col);
                }
            }
            for(int i=0;i < delete_cols.Count;i++)
            {
                dtPortfolio.Columns.Remove(delete_cols[i]);
            }
            if (!dtPortfolio.Columns.Contains("portfolioname"))
            {
                dtPortfolio.Columns.Add("portfolioname");
            }
            // Use the DefaultView to get a reference to the DataView
            DataView dataView = dtPortfolio.DefaultView;

            // Set the column value for all rows in the DataTable
            dataView.Table.Rows.Cast<DataRow>().ToList().ForEach(row => row["portfolioname"] = this.portfolioname);
            if (this.conn.State == ConnectionState.Closed)
            {
                this.conn.Open();
            }
            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(this.conn))
            {
                foreach (DataColumn column in dtPortfolio.Columns)
                {
                    //if(column.ColumnName in )
                    bulkcopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }
                bulkcopy.DestinationTableName = "portfolio_weights";
                bulkcopy.WriteToServer(dtPortfolio);
            }
            ret = true;
                
            return ret;
        }

        private bool add_max_positions(DataTable dtPortfolio)
        {
            bool ret = false;
            List<DataColumn> delete_cols = new List<DataColumn>();
            //remove any columns that dont belong to the portfolioweights table
            // first have to create list of the columns you want to delete
            foreach (DataColumn col in dtPortfolio.Columns)
            {
                if (!this.max_position_columns.Contains(col.ColumnName))
                {
                    delete_cols.Add(col);
                }
            }
            //then delete the col from the datatable
            for(int i =0;i<delete_cols.Count;i++)
            {
                dtPortfolio.Columns.Remove(delete_cols[i]);
            }

            if (!dtPortfolio.Columns.Contains("portfolioname"))
            {
                dtPortfolio.Columns.Add("portfolioname");
            }

            // Use the DefaultView to get a reference to the DataView
            DataView dataView = dtPortfolio.DefaultView;

            // Set the column value for all rows in the DataTable
            dataView.Table.Rows.Cast<DataRow>().ToList().ForEach(row => row["portfolioname"] = this.portfolioname);

            if (this.conn.State == ConnectionState.Closed)
            {
                this.conn.Open();
            }
            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(this.conn))
            {
                foreach (DataColumn column in dtPortfolio.Columns)
                {
                    //if(column.ColumnName in )
                    bulkcopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }
                bulkcopy.DestinationTableName = "max_position";
                bulkcopy.WriteToServer(dtPortfolio);
            }
            ret = true;

            return ret;
        }
    }


}



//}
