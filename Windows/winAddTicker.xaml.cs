using System;
using System.Collections.Generic;
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

        public List<TickerRow> SelectedTickers { get; private set; }

        public winAddTicker(IEnumerable<TickerRow> items)
        {
            InitializeComponent();
            _all = items?.ToList() ?? new List<TickerRow>();
            ListViewTickers.ItemsSource = _all;
            _view = CollectionViewSource.GetDefaultView(ListViewTickers.ItemsSource);
            _view.Filter = FilterPredicate;
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
            var sel = ListViewTickers.SelectedItems;
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
