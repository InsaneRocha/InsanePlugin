using InsanePlugin;
using SimHub.Plugins.Styles;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InsanePlugin
{
    public class SettingsControlViewModel
    {
        public InsanePlugin Plugin { get; }

        public SettingsControlViewModel()
        {
        }
        public SettingsControlViewModel(InsanePlugin plugin) : this()
        {
            Plugin = plugin;
        }
    }

    public partial class SettingsControl : UserControl
    {
        public InsanePlugin Plugin { get; }

        public SettingsControl()
        {
            InitializeComponent();
        }

        public SettingsControl(InsanePlugin plugin) : this()
        {
            this.Plugin = plugin;
            this.DataContext = new SettingsControlViewModel(plugin);
        }
    }
}