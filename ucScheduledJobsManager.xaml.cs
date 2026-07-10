using System.Windows.Controls;

namespace wpfTDX
{
    /// <summary>
    /// Scheduled-jobs manager (rebuilt). Async, explicit-save, talks to the typed
    /// tapi endpoints via ScheduleJobViewModel. Loads on open.
    /// </summary>
    public partial class ucScheduledJobsManager : UserControl
    {
        private readonly ScheduleJobViewModel _vm;

        public ucScheduledJobsManager()
        {
            InitializeComponent();
            _vm = new ScheduleJobViewModel();
            DataContext = _vm;
            Loaded += async (s, e) => await _vm.LoadAsync();
        }
    }
}
