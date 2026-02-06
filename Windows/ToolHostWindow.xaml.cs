using System;
using System.Windows;
using System.Windows.Controls;

namespace wpfTDX
{
    public partial class ToolHostWindow : Window
    {
        public ToolHostWindow(UserControl content, string title = null)
        {
            InitializeComponent();

            if (content == null) throw new ArgumentNullException(nameof(content));

            Host.Content = content;
            Title = title ?? content.GetType().Name;
        }
    }
}
