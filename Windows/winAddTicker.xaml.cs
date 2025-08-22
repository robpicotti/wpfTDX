using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using static wpfTDX.FilterIntervalsViewModel;

namespace wpfTDX
{
    public partial class winAddTicker : Window
    {
        private readonly ICollectionView _view;
        private readonly List<TickerRow> _all;

        // Exposed for the DataGridComboBoxColumn ItemsSource
        public ObservableCollection<string> FundGroups { get; } = new ObservableCollection<string>();


        public List<TickerRow> SelectedTickers { get; private set; }

        public winAddTicker(IEnumerable<TickerRow> items)
        {
            InitializeComponent();
            Loaded += WinAddTicker_Loaded;

            _all = items?.ToList() ?? new List<TickerRow>();

            // Bind grid to the full ticker list
            GridTickers.ItemsSource = _all;

            // Set up filtering
            _view = CollectionViewSource.GetDefaultView(GridTickers.ItemsSource);
            _view.Filter = FilterPredicate;
        }

        private async void WinAddTicker_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Call your service
                var ws = new WebServiceData();
                string json = await ws.GetFundsDataASync();

                // Expecting an array of objects that include "fundgroupname"
                var arr = JArray.Parse(json);

                // Build distinct list, case-insensitive
                var groups = arr
                    .Select(x => (string)(x["fundgroupname"] ?? string.Empty))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Insert "ALL" manually at the top
                FundGroups.Clear();
                FundGroups.Add("ALL");
                foreach (var g in groups)
                    FundGroups.Add(g);

                // Refresh grid in case any edit was pending
                GridTickers.CommitEdit();
                GridTickers.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Load Fund Groups",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool FilterPredicate(object obj)
        {
            if (obj == null) return false;
            var row = obj as TickerRow;
            if (row == null) return false;

            var text = (FilterBox.Text ?? string.Empty).Trim();
            if (text.Length == 0) return true;

            var ti = StringComparison.OrdinalIgnoreCase;
            return (row.TickerName ?? "").IndexOf(text, ti) >= 0
                || (row.Description ?? "").IndexOf(text, ti) >= 0;
        }

        private void FilterBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_view != null) _view.Refresh();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var sel = GridTickers.SelectedItems;
            if (sel == null || sel.Count == 0)
            {
                MessageBox.Show(this, "Please select at least one ticker.", "Add Ticker",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedTickers = sel.Cast<TickerRow>().ToList();
            DialogResult = true;
        }
    }
}

