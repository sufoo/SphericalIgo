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
using System.Windows.Shapes;
using System.IO;
using System.Xml.Serialization;

namespace SphericalIgo
{
    public enum GameKind
    {
        GAMEKIND_OTHELLO = 1,
        GAMEKIND_IGO
    }

    public struct Parameters
    {
        public GameKind Kind { get; set; }
        public int Resolution { get; set; }
        public int Quality { get; set; }
        public Color ForeColorIgo { get; set; }
        public Color ForeColorOthello { get; set; }
        public Color BackColor { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }

        public void SetInitValue()
        {
            Kind = GameKind.GAMEKIND_IGO;
            Resolution = 2;
            Quality = 2;
            ForeColorIgo = new Color() { R = 247, G = 208, B = 105, A = 0xFF };
            ForeColorOthello = new Color() { R = 5, G = 229, B = 114, A = 0xFF };
            BackColor = new Color() { R = 0xFF, G = 0xFF, B = 0xFF, A = 0xFF };
            WindowWidth = 0;
            WindowHeight = 0;
        }
        public Color ForeColor
        {
            get
            {
                return (Kind == GameKind.GAMEKIND_IGO) ? ForeColorIgo : ForeColorOthello;
            }
            set
            {
                if (Kind == GameKind.GAMEKIND_IGO)
                {
                    ForeColorIgo = value;
                }
                else
                {
                    ForeColorOthello = value;
                }
            }
        }
    }

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Setting : Window
    {
        static public Parameters param;

        public Setting()
        {
            InitializeComponent();
            comboBoxKind.SelectedValue = (int)param.Kind;
            comboBoxResolution.SelectedValue = param.Resolution;
            comboBoxMeshQuality.SelectedValue = param.Quality;
            ColorSampleFore.Fill = new SolidColorBrush(param.ForeColor);
            ColorSampleBack.Fill = new SolidColorBrush(param.BackColor);
        }

        private void ButtonOkClicked(object sender, RoutedEventArgs e)
        {
            param.Kind = (GameKind)int.Parse(comboBoxKind.SelectedValue.ToString());
            param.Resolution = int.Parse(comboBoxResolution.SelectedValue.ToString());
            param.Quality = int.Parse(comboBoxMeshQuality.SelectedValue.ToString());

            e.Handled = true;

            DialogResult = true;
        }

        private void comboBoxKind_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxKind.SelectedValue != null)
            {
                param.Kind = (GameKind)int.Parse(comboBoxKind.SelectedValue.ToString());
                ColorSampleFore.Fill = new SolidColorBrush(param.ForeColor);
            }
        }

        private void ButtonColorChangeClicked(object sender, RoutedEventArgs e)
        {
            ColorPickerControl.ColorPickerDialog cPicker = new ColorPickerControl.ColorPickerDialog();
            cPicker.Owner = this;

            Button button = sender as Button;
            if (button.Name == "button_forecolor")
            {
                cPicker.StartingColor = param.ForeColor;
            }
            else
            {
                cPicker.StartingColor = param.BackColor;
            }

            bool? dialogResult = cPicker.ShowDialog();

            if (dialogResult != null && (bool)dialogResult == true)
            {
                if (button.Name == "button_forecolor")
                {
                    param.ForeColor = cPicker.SelectedColor;
                    ColorSampleFore.Fill = new SolidColorBrush(param.ForeColor);
                }
                else
                {
                    param.BackColor = cPicker.SelectedColor;
                    ColorSampleBack.Fill = new SolidColorBrush(param.BackColor);
                }
            }

            e.Handled = true;
        }

        //設定をロードする
        public static void Load(bool use_initial)
        {
            //ユーザ毎のアプリケーションデータディレクトリに保存する
            String appPath = String.Format(
                "{0}\\{1}", 
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SphericalIgo\\settings.xml");

            if (!use_initial && File.Exists(appPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Parameters));

                using (FileStream stream = new FileStream(appPath, FileMode.Open))
                {
                    Nullable<Parameters> temp = serializer.Deserialize(stream) as Nullable<Parameters>;
                    if (temp == null)
                        param.SetInitValue();
                    else
                        param = (Parameters)temp;
                }
            }
            else
            {
                String folderPath = String.Format(
                    "{0}\\{1}",
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SphericalIgo");
                System.IO.Directory.CreateDirectory(folderPath);
                param.SetInitValue();
            }
        }

        //設定を保存する
        public static void Save()
        {
            String appPath = String.Format(
                "{0}\\{1}",
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SphericalIgo\\settings.xml");
            XmlSerializer serializer = new XmlSerializer(typeof(Parameters));

            using (FileStream stream = new FileStream(appPath, FileMode.Create))
            {
                serializer.Serialize(stream, param);
            }
        }
    }
}
