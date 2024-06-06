using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using TDX;

namespace wpfTDX
{
    public partial class winTradeHistory : Window
    {
        string FundName;
        string Tad_id;
        string TickerName;
        DateTime PositionDate;
        public SeriesCollection SeriesCollection { get; set; }
        public List<string> XLabels { get; set; }
        SqlConnection gbl_conn;

        public winTradeHistory(string fundname, string tad_id, string tickername, DateTime positiondate, SqlConnection conn)
        {
            InitializeComponent();
            this.FundName = fundname;
            this.Tad_id = tad_id;
            this.TickerName = tickername;
            this.PositionDate = positiondate;
            this.gbl_conn = conn;
            loadForm();
        }

        private void loadForm()
        {
            txtFundName.Text = this.FundName;
            txtTadId.Text = this.Tad_id;
            txtTickerName.Text = this.TickerName;
            dtPickerPostionFrom.SelectedDate = this.PositionDate;
            dtPickerPostionTo.SelectedDate = this.PositionDate;
        }

        private void cmdRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime startDate = dtPickerPostionFrom.SelectedDate.Value;
                DateTime endDate = dtPickerPostionTo.SelectedDate.Value.AddDays(1).AddMilliseconds(-1);
                Position posn = new Position("", this.gbl_conn);
                DataTable dtRun = posn.get_daily_positions(this.FundName, this.Tad_id, this.TickerName, startDate, endDate);
                LoadNetPositionChartData(dtRun);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Trade history", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadNetPositionChartData(DataTable dataTable)
        {
            // Initialize SeriesCollection
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Net Position",
                    Values = new ChartValues<double>(),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10
                }
            };

            // Initialize XLabels
            XLabels = new List<string>();

            // Calculate min and max dates
            DateTime minDate = DateTime.MaxValue;
            DateTime maxDate = DateTime.MinValue;

            foreach (DataRow row in dataTable.Rows)
            {
                DateTime date;
                if (DateTime.TryParse(row["execution_date"].ToString(), out date))
                {
                    if (date < minDate)
                        minDate = date;
                    if (date > maxDate)
                        maxDate = date;
                }
            }

            // Ensure valid min and max dates were found
            if (minDate == DateTime.MaxValue || maxDate == DateTime.MinValue)
            {
                MessageBox.Show("No valid dates found in the execution_date column.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Fill forward missing dates
            DataTable filledDataTable = FillForwardDataTable(dataTable, minDate, maxDate);

            double minValue = double.MaxValue;
            double maxValue = double.MinValue;

            foreach (DataRow row in filledDataTable.Rows)
            {
                string executionDate = row["execution_date"].ToString();
                XLabels.Add(executionDate);

                if (double.TryParse(row["latest_net_position"].ToString(), out double netPosn))
                {
                    SeriesCollection[0].Values.Add(netPosn);
                    if (netPosn < minValue) minValue = netPosn;
                    if (netPosn > maxValue) maxValue = netPosn;
                }
            }

            // Adjust minValue and maxValue to add some padding
            double padding = (maxValue - minValue) * 0.1; // 10% padding
            minValue -= padding;
            maxValue += padding;

            // Set dynamic min and max values for Y-axis
            MyChart.AxisY[0].MinValue = minValue;
            MyChart.AxisY[0].MaxValue = maxValue;

            // Set bold zero line
            MyChart.AxisY.Add(new Axis
            {
                Position = AxisPosition.LeftBottom,
                IsEnabled = true,
                Separator = new LiveCharts.Wpf.Separator
                {
                    Step = (maxValue - minValue) / 5,
                    StrokeThickness = 2,
                    Stroke = Brushes.Black,
                    IsEnabled = true
                }
            });

            // Bind SeriesCollection to the chart
            MyChart.Series = SeriesCollection;

            // Update the XAxis labels
            MyChart.AxisX[0].Labels = XLabels;
        }

        private DataTable FillForwardDataTable(DataTable originalDataTable, DateTime minDate, DateTime maxDate)
        {
            DataTable filledDataTable = new DataTable();
            foreach (DataColumn column in originalDataTable.Columns)
            {
                filledDataTable.Columns.Add(column.ColumnName, column.DataType);
            }

            List<DateTime> allDates = new List<DateTime>();
            for (DateTime date = minDate.Date; date <= maxDate.Date; date = date.AddDays(1))
            {
                allDates.Add(date);
            }

            foreach (DateTime date in allDates)
            {
                DataRow[] rowsForDate = originalDataTable.Select($"execution_date = #{date:MM/dd/yyyy}#");
                if (rowsForDate.Length > 0)
                {
                    foreach (DataRow row in rowsForDate)
                    {
                        filledDataTable.ImportRow(row);
                    }
                }
                else
                {
                    DataRow newRow = filledDataTable.NewRow();
                    newRow["execution_date"] = date;
                    foreach (DataColumn column in filledDataTable.Columns)
                    {
                        if (column.ColumnName != "execution_date")
                        {
                            newRow[column.ColumnName] = DBNull.Value;
                        }
                    }
                    filledDataTable.Rows.Add(newRow);
                }
            }

            filledDataTable.DefaultView.Sort = "execution_date ASC";
            filledDataTable = filledDataTable.DefaultView.ToTable();

            foreach (DataColumn column in filledDataTable.Columns)
            {
                if (column.ColumnName != "execution_date")
                {
                    object lastValue = DBNull.Value;
                    foreach (DataRow row in filledDataTable.Rows)
                    {
                        if (row[column] != DBNull.Value)
                        {
                            lastValue = row[column];
                        }
                        else
                        {
                            row[column] = lastValue;
                        }
                    }
                }
            }

            return filledDataTable;
        }

        private IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
                yield return day;
        }
    }
}
