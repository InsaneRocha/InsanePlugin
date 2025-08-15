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
        public InsanePluginMain Plugin { get; }

        public SettingsControlViewModel()
        {
        }
        public SettingsControlViewModel(InsanePluginMain plugin) : this()
        {
            Plugin = plugin;
        }
    }

    public partial class SettingsControl : UserControl
    {
        public InsanePluginMain Plugin { get; }

        public SettingsControl()
        {
            InitializeComponent();
        }

        public SettingsControl(InsanePluginMain plugin) : this()
        {
            this.Plugin = plugin;
            this.DataContext = new SettingsControlViewModel(plugin);
        }
    }
}