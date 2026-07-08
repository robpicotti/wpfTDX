using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
        string FUNDGROUPNAME;
        string SUBACCOUNT;
        string TICKERNAME;
        string SCALE_TYPE;
        double? SCALE_FACTOR;
        double? SCALED_PERCENT;
        SqlConnection GBL_CONN;

        double step_size { get; set; }
        double scaled_target { get; set; }

        // For scaled_positions edits: the current rows, so we can tell the user whether this
        // scale will actually apply or be superseded (see ScaleResolver). Set by the caller
        // (winScaledPositions) before ShowDialog; null for the direct-SQL scaling paths.
        public System.Collections.Generic.List<ScaledPositionsDataModel> ExistingScales { get; set; }

        // When true, inserts go through the API (ScaleService) for ANY scale_type — not just
        // "manual" — because the calling screen (winScaledPositions) has no SqlConnection.
        public bool UseApiInsert { get; set; }

        // Existing row's scaled_timestep, preserved when editing a non-manual scale via the API
        // (winScale has no field for it). Null → default 5.
        public double? ExistingTimeStep { get; set; }

        private Position position { get; set; }
        public winScale(object position, string fundname,string subaccount,string tickername, string scale_type,
            double? ScaleFactor,double? ScaledPercent,SqlConnection conn,string fundgroupname)
        {
            InitializeComponent();
            FUNDNAME = fundname;
            FUNDGROUPNAME = fundgroupname;
            SUBACCOUNT = subaccount;
            TICKERNAME = tickername;
            SCALE_TYPE = scale_type;
            this.SCALED_PERCENT = ScaledPercent;
            this.GBL_CONN = conn;
            this.SCALE_FACTOR = ScaleFactor;
            this.position = (Position)position;
            LoadForm();
        }
        private void LoadForm()
        {
            txtSubaccount.Text = FUNDNAME;
            txtFundGroupName.Text = FUNDGROUPNAME;
            txtTickername.Text = TICKERNAME;
            
            if (this.position != null)
            {
                txtScaledPosition.Text = this.position.get_scaled_percent(FUNDNAME, TICKERNAME).ToString();
            }
            else
            {
                // Show the current scaled percent when editing an existing scale (any type).
                if (this.SCALED_PERCENT.HasValue)
                {
                    txtScaledPosition.Text = this.SCALED_PERCENT.ToString();
                }

                lblScaledPosition.IsEnabled = false;
                txtScaledPosition.IsEnabled = false;
                lblSubaccount.IsEnabled = false;
                txtSubaccount.IsEnabled = false;

            }
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!validate_scaling())
                {
                    MessageBox.Show("Either step size or target has an incorrect value",
                        "Value validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Scaled-positions inserts (any scale_type when UseApiInsert, plus MANUAL from
                // anywhere) go through the API (server-side Scale class) — no direct SQL.
                if (UseApiInsert || (SCALE_TYPE != null && SCALE_TYPE.ToUpper() == "MANUAL"))
                {
                    string scaleType = string.IsNullOrWhiteSpace(SCALE_TYPE) ? "manual" : SCALE_TYPE.ToLower();
                    double newTarget = double.Parse(txtScaledTarget.Text);

                    // Tell the user the concrete effect: whether this scale will actually apply
                    // for this fund, or be superseded by an existing (e.g. more-specific) scale.
                    var effect = ScaleResolver.Resolve(FUNDGROUPNAME, FUNDNAME, TICKERNAME, newTarget, ExistingScales, scaleType);

                    string scale_msg = "Scale " + TICKERNAME + " (fund: " + FUNDNAME + ", type: " + scaleType + ")"
                        + "\r\n" + "step size : " + txtStepSize.Text + "\r\n" + "target : " + txtScaledTarget.Text;
                    if (!string.IsNullOrEmpty(effect.Message))
                        scale_msg += "\r\n\r\n" + effect.Message;
                    scale_msg += "\r\n\r\nContinue?";

                    // Default to "No" when the scale would have no effect, otherwise "Yes".
                    var defaultBtn = (!string.IsNullOrEmpty(effect.Message) && !effect.NewApplies)
                        ? MessageBoxResult.No : MessageBoxResult.Yes;
                    var img = (!string.IsNullOrEmpty(effect.Message) && !effect.NewApplies)
                        ? MessageBoxImage.Warning : MessageBoxImage.Question;

                    if (MessageBox.Show(scale_msg, "Scale position", MessageBoxButton.YesNo, img, defaultBtn) != MessageBoxResult.Yes)
                        return;

                    var row = new ScaledPositionsInsertRow
                    {
                        FundGroupName = FUNDGROUPNAME,
                        FundName = FUNDNAME,
                        TickerName = TICKERNAME,
                        ScaledStepSize = double.Parse(txtStepSize.Text),
                        ScaledTarget = newTarget,
                        ScaledPercent = SCALED_PERCENT ?? 1.0,
                        ScaledTimeStep = ExistingTimeStep ?? 5,   // preserve the row's cadence when editing
                        ScaledType = scaleType
                    };

                    var res = await new ScaleService().InsertAndWaitAsync(row);
                    if (res.Ok)
                    {
                        MessageBox.Show("Scaling entered for tickername: " + TICKERNAME + " (fund: " + FUNDNAME + ", type: " + scaleType + ").",
                            "Scaled positions", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.DialogResult = true;   // let the caller reload
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show(res.Message, "Scaling position error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                // Existing direct-SQL path (positions scale-out / filtered scaling)
                string dialog_msg = "Are you sure you want to scale position for: " + "\r\n" + "subaccount : " + SUBACCOUNT + "\r\n" + "tickername : " + TICKERNAME;
                dialog_msg += "\r\n" + "step size : " + txtStepSize.Text + "\r\n" + "target : " + txtScaledTarget.Text + "?";
                if (MessageBox.Show(dialog_msg, "Position Scaling", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                if (this.position == null)
                {
                    this.position = new Position(SUBACCOUNT, GBL_CONN);
                }
                this.position.scale_position(FUNDNAME, TICKERNAME, double.Parse(txtStepSize.Text), double.Parse(txtScaledTarget.Text), SCALE_TYPE, FUNDGROUPNAME);
                MessageBox.Show("position has been entered for scaling for subaccount: " + SUBACCOUNT + " and tickername: " + TICKERNAME,
                    "Scaled positions", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
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
                // Scale-in types (manual/filtered/in) can go above 1 up to the server-driven
                // ScaleLimits.Max (Scale.max_scale) — not hardcoded, so raising max_scale
                // server-side needs no client change. Scale-out types keep the local <= 1 cap.
                // (The server also enforces this as a backstop.)
                string t = SCALE_TYPE == null ? "" : SCALE_TYPE.ToUpper();
                bool isScaleIn = t == "MANUAL" || t == "FILTERED" || t == "IN";
                double maxTarget = isScaleIn ? ScaleLimits.Max : 1.0;
                if (target < 0 || target > maxTarget)
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
