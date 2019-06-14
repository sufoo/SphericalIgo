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
    /// Interaction logic for ConsoleIgo.xaml
    /// </summary>
    public partial class ConsoleIgo : UserControl, IConsole
    {
        public ConsoleIgo()
        {
            InitializeComponent();

            DataContext = new OperationState();
        }

        private void OnCapturedMouseDown(object sender, MouseButtonEventArgs e)
        {
            Ellipse obj = sender as Ellipse;
            if (obj.Name == "ellipse_black")
            {
                rule.RetrievePiece(PieceKind.PIECE_BLACK, (e.LeftButton == MouseButtonState.Pressed) ? 1 : -1);
            }
            else
            {
                rule.RetrievePiece(PieceKind.PIECE_WHITE, (e.LeftButton == MouseButtonState.Pressed) ? 1 : -1);
            }

            // ------------------------------
            // Show scores
            // ------------------------------
            UpdateDispScore();
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
            label_captured_black_num.Content = rule.RetrievePiece(PieceKind.PIECE_BLACK, 0);
            label_captured_white_num.Content = rule.RetrievePiece(PieceKind.PIECE_WHITE, 0);
        }

        public OperationState GetOperationState()
        {
            return (OperationState)DataContext;
        }
    }
}
