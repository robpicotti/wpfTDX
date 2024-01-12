using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TDX;
namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for winScale.xaml
    /// </summary>
    public partial class winScale : Window
    {
        string FUNDNAME;
        string SUBACCOUNT;
        string TICKERNAME;
        double step_size { get; set; }
        double scaled_target { get; set; }
        private Position position { get; set; }
        public winScale(object position, string fundname,string subaccount,string tickername)
        {
            InitializeComponent();
            FUNDNAME = fundname;
            SUBACCOUNT = subaccount;
            TICKERNAME = tickername;
            this.position = (Position)position;
            LoadForm();
        }
        private void LoadForm()
        {
            txtSubaccount.Text = SUBACCOUNT;
            txtTickername.Text = TICKERNAME;
            txtScaledPosition.Text = this.position.get_scaled_percent(TICKERNAME).ToString();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            bool blnValidate;
            try
            {
                blnValidate = validate_scaling();
                if (!blnValidate)
                {
                    string msg = "Either step size or target has an incorrect value";
                    string title = "Value validation error";
                    MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    string dialog_msg = "Are you sure you want to scale position for: " + "\r\n" + "subaccount : " + SUBACCOUNT + "\r\n" + "tickername : " + TICKERNAME;
                    dialog_msg += "\r\n" + "step size : " + txtStepSize.Text + "\r\n" + "target : " + txtScaledTarget.Text + "?";
                    MessageBoxResult result = MessageBox.Show(dialog_msg, "Position Scaling", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        this.position.scale_position(FUNDNAME, TICKERNAME, double.Parse(txtStepSize.Text),double.Parse(txtScaledTarget.Text));
                        string title = "Scaled positions";
                        string mes = "position has been entered for scaling for subaccount: " + SUBACCOUNT + " and tickername: " + TICKERNAME;
                        MessageBox.Show(mes, title, MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Scaling position error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool validate_scaling()
        {
            bool bln_out = true;
            double step;
            double target;

            if (IsNumeric(txtStepSize.Text))
            {
                Double.TryParse(txtStepSize.Text, out step);
                this.step_size = step;
            }
            else { bln_out = false; step = -1; }
            if (IsNumeric(txtScaledTarget.Text))
            {
                Double.TryParse(txtScaledTarget.Text, out target);
                this.scaled_target = target;
            }
            else { bln_out = false; target = -1; }
            if (bln_out)
            {
                if (step > 1 || step <= 0)
                {
                    bln_out = false;
                }
                if (target < 0 || target > 1)
                {
                    bln_out = false;
                }
            }
            return bln_out;

        }
        public static bool IsNumeric(object value)
        {
            float test_value;
            return float.TryParse(value.ToString(), out test_value);
        }
    }
}
