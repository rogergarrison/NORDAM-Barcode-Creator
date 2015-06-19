using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Printing;

namespace BarcodeCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Nordam2DLabel labelInfo;
        public MainWindow()
        {
            InitializeComponent();
            PreviewImage.Visibility = System.Windows.Visibility.Hidden;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            SetLabelInfo();
            switch(menuItem.Name.ToLower())
            {
                case "exit":
                    ExitProgram();
                    break;
                case "preview":
                    ShowPreviewImage();
                    break;
                case "print":
                    PrintZebraCommand();
                    break;
                default:
                    break;
            }
        }

        private void SetLabelInfo()
        {
            int dotsPerMill = Int32.Parse(Regex.Replace(Dots.Text, @"^\s*([0-9]+)[^0-9].*$", "$1"));
            double height = Double.Parse(LabelHeight.Text);
            double width = Double.Parse(LabelWidth.Text);
            bool millimeters = !Inches.IsChecked.Value;
            
            // dictionary probably isn't the best way to do it -- figure out a better way
            Dictionary<string, string> labelValues = new Dictionary<string, string>();
            labelValues["PO"] = PurchaseOrderNum.Text;
            labelValues["P"] = PartNum.Text;
            labelValues["L"] = LineItem.Text;
            labelValues["D"] = Description.Text;
            labelValues["Q"] = Quantity.Text;
            labelValues["U"] = UoM.Text;
            labelValues["PS"] = PackingSlipNum.Text;
            labelValues["ED"] = ExpirationDate.Text;
            labelValues["MB"] = MFGBatchNum.Text;
            labelValues["SN"] = SerialNum.Text;

            labelInfo = new Nordam2DLabel(height, width, dotsPerMill, millimeters, labelValues);
        }

        private async void ShowPreviewImage()
        {
            var previewImage = await Task<System.Drawing.Image>.Factory.StartNew(() => GetPreviewImage());
            SetPreviewImage(previewImage);
            //1inch == 96 px
            //PreviewImage.Height = labelInfo.HeightInches * 96;
            PreviewImage.Height = previewImage.Height * 0.50;
            PreviewImage.Width = previewImage.Width * 0.50;
            PreviewImage.Visibility = System.Windows.Visibility.Visible;
        }

        private System.Drawing.Image GetPreviewImage()
        {
            string url = String.Format("http://api.labelary.com/v1/printers/{0}dpmm/labels/{1}x{2}/0/{3}"
                ,labelInfo.Dots, labelInfo.WidthInches, labelInfo.HeightInches,  HttpUtility.UrlEncode(labelInfo.GetLabel()).Replace("+", "%20"));
            WebRequest getUrl = WebRequest.Create(url);
            System.Drawing.Image img = System.Drawing.Image.FromStream(getUrl.GetResponse().GetResponseStream());
            return img;
        }

        private void SetPreviewImage(System.Drawing.Image img)
        {
            MemoryStream ms = new MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

            BitmapImage bImg = new BitmapImage();
                bImg.BeginInit();
                    bImg.StreamSource = new MemoryStream(ms.ToArray());
                bImg.EndInit();
            PreviewImage.Source = bImg;
        }

        private void ExitProgram()
        {
            Application.Current.Shutdown();
        }

        private void PrintZebraCommand()
        {
            // Have the user Select a printer
            PrintDialog pd = new PrintDialog();
            if(pd.ShowDialog() == true)
            {
                // Send commands to printer 
                RawPrintHelper.SendStringToPrinter(pd.PrintQueue.FullName, labelInfo.GetLabel());
            }
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            SetLabelInfo();
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            string defaultBaseName = String.Format("ZebraPrint-{0}-", DateTime.Now.ToShortDateString().Replace("/", "-"));
            bool png = false;
            switch(menuItem.Name)
            {
                case "PreviewSave":
                    dlg.FileName = String.Format("{0}PreviewImage", defaultBaseName); // Default File name
                    dlg.DefaultExt = ".png"; //Default file extension
                    dlg.Filter = "PNG Files (*.png)|*.png";  // Filter files by extension
                    png = true;
                    break;
                case "CommandSave":
                    dlg.FileName = String.Format("{0}PrintCommand", defaultBaseName); // Default File name
                    dlg.DefaultExt = ".txt"; //Default file extension
                    dlg.Filter = "Text Files (*.txt)|*.txt";  // Filter files by extension
                    break;
                default:
                    break;
            }


            if(dlg.ShowDialog() == true)
            {
                string filename = dlg.FileName;
                if(png)
                {
                    GetPreviewImage().Save(filename);
                }
                else
                {
                    File.WriteAllText(filename, labelInfo.GetLabel());
                }
            }
        }

        private void Textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Regex badCharRegex = new Regex(@"[\^:]", RegexOptions.Compiled);
            if(badCharRegex.IsMatch(((TextBox)sender).Text))
            {
                ((TextBox)sender).Text = badCharRegex.Replace(((TextBox)sender).Text, "");
            }
        }

    }
}
