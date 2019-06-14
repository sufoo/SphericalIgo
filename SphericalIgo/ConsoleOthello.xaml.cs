using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SphericalIgo
{
    /// <summary>
    /// Interaction logic for ConsoleOthello.xaml
    /// </summary>
    public partial class ConsoleOthello : UserControl, IConsole
    {
        public ConsoleOthello()
        {
            InitializeComponent();

            DataContext = new OperationState();
        }

        private void buttonSetting_Click(object sender, RoutedEventArgs e)
        {
            Setting dialog = new Setting();
            Nullable<bool> dialogResult = dialog.ShowDialog();
            if (dialogResult != null && dialogResult.Value)
            {
                OnConfigurationsChanged(sender, e);
            }
        }

        private void buttonHelp_Click(object sender, RoutedEventArgs e)
        {
            AboutDialog dialog = new AboutDialog();
            Nullable<bool> dialogResult = dialog.ShowDialog();
        }

        // ------------------------------
        // Implementation of ConsoleInterface
        // ------------------------------

        private EventHandler OnConfigurationsChanged;
        public void SetOnConfigurationsChanged(EventHandler handle)
        {
            OnConfigurationsChanged = handle;
        }

        private GameRule rule;
        public void SetGameRule(GameRule _rule)
        {
            rule = _rule;
        }

        public void UpdateDispCoordinate()
        {
            label_coord.Content = "[Coordinates] " + rule.GetCursorCoordinatesText();
        }

        public void UpdateDispScore()
        {
            label_status.Content = "[Scores] " + rule.GetStatus();
        }

        public OperationState GetOperationState()
        {
            return (OperationState)DataContext;
        }
    }
}
