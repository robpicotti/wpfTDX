using System;
using System.Windows;
using System.Windows.Input;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for winErrorInfo.xaml
    /// View/edit documentation for a freezer error_code.
    /// </summary>
    public partial class winErrorInfo : Window
    {
        private readonly TickerFreezerDataModel _ticker;
        private readonly WebServiceData _wsd;
        private string _originalDocumentation;

        public winErrorInfo(TickerFreezerDataModel ticker, WebServiceData wsd)
        {
            InitializeComponent();
            _ticker = ticker;
            _wsd = wsd;
            _originalDocumentation = ticker?.Documentation ?? string.Empty;
            LoadForm();
            this.Loaded += WinErrorInfo_Loaded;
        }

        private void LoadForm()
        {
            txtErrorCode.Text = _ticker.Errorcode.ToString();
            txtErrorString.Text = _ticker.Errorstring ?? string.Empty;
            txtDocumentation.Text = _originalDocumentation;
        }

        private async void WinErrorInfo_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                cmdEdit.IsEnabled = false;
                string freshDoc = await _wsd.GetErrorDocumentationAsync(_ticker.Errorcode);
                _originalDocumentation = freshDoc ?? string.Empty;
                txtDocumentation.Text = _originalDocumentation;
                _ticker.Documentation = _originalDocumentation;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to fetch fresh documentation: " + ex.Message,
                    "Error Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                cmdEdit.IsEnabled = true;
                Cursor = Cursors.Arrow;
            }
        }

        private void cmdEdit_Click(object sender, RoutedEventArgs e)
        {
            txtDocumentation.IsReadOnly = false;
            txtDocumentation.Focus();
            cmdEdit.IsEnabled = false;
            cmdSave.IsEnabled = true;
            cmdCancel.IsEnabled = true;
        }

        private async void cmdSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                cmdSave.IsEnabled = false;
                cmdCancel.IsEnabled = false;

                string newDoc = txtDocumentation.Text;
                await _wsd.UpdateErrorDocumentationAsync(_ticker.Errorcode, newDoc);

                _ticker.Documentation = newDoc;
                _originalDocumentation = newDoc;

                txtDocumentation.IsReadOnly = true;
                cmdEdit.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save documentation: " + ex.Message,
                    "Error Info", MessageBoxButton.OK, MessageBoxImage.Error);
                cmdSave.IsEnabled = true;
                cmdCancel.IsEnabled = true;
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void cmdCancel_Click(object sender, RoutedEventArgs e)
        {
            txtDocumentation.Text = _originalDocumentation;
            txtDocumentation.IsReadOnly = true;
            cmdEdit.IsEnabled = true;
            cmdSave.IsEnabled = false;
            cmdCancel.IsEnabled = false;
        }

        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
