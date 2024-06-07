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
                LoadNetPositionChartData(dtRun,endDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Trade history", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadNetPositionChartData(DataTable dataTable, DateTime endDate)
        {
            // Initialize SeriesCollection
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Net Position",
                    Values = new ChartValues<double>(),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 2
                }
            };

            // Initialize XLabels
            XLabels = new List<string>();

            // Calculate min and max dates
            DateTime minDate = DateTime.MaxValue;
            DateTime maxDate = endDate;//DateTime.MinValue;

            foreach (DataRow row in dataTable.Rows)
            {
                DateTime date;
                if (DateTime.TryParse(row["execution_date"].ToString(), out date))
                {
                    if (date < minDate)
                        minDate = date;
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
            // Ensure zero is included in the axis range
            if (minValue > 0) minValue = 0;
            if (maxValue < 0) maxValue = 0;

            // Calculate the step size dynamically
            double range = maxValue - minValue;
            double stepSize = CalculateStepSize(range);

            // Set dynamic min and max values for Y-axis
            MyChart.AxisY[0].MinValue = minValue;
            MyChart.AxisY[0].MaxValue = maxValue;

            // Set up zero line as bold
            MyChart.AxisY[0].Separator = new LiveCharts.Wpf.Separator
            {
                Step = stepSize, // Adjust step to fit your data range
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 2, 2 },
                Stroke = Brushes.Gray
            };

            // Add AxisSection for bold zero line
            MyChart.AxisY[0].Sections.Add(new AxisSection
            {
                Value = 0,
                Stroke = Brushes.Black,
                Label = "0",
                StrokeThickness = 2
            });


            MyChart.Series = SeriesCollection;
            MyChart.AxisX[0].Labels = XLabels;

        }
        private double CalculateStepSize(double range)
        {
            // Determine a reasonable step size that avoids cramped axis labels and uses whole numbers
            double step = Math.Pow(10, Math.Floor(Math.Log10(range)));
            if (range / step > 10) step *= 2;
            if (range / step > 10) step *= 5;
            return step;
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
