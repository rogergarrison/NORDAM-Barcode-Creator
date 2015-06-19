using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace BarcodeCreator
{
    public class Nordam2DLabel
    {
        ///  <summary>
        ///  Nordam2DLabel is a wrapper over ZPL that takes ins the dimensions of the label and creates
        ///  the appropriate ZPL commands to output a Nordam 2D label based on the specifications
        /// </summary>

        //private properties
        private readonly double setHeightInMM;
        private readonly double setWidthInMM;
        private double RowVal;
        private readonly Dictionary<string, string> LabelValues;

        private double InterChangeWidth 
        {
            get 
            {
                return Math.Round(((WidthMM - 10) * Dots) / 317, 1);
            } 
        }
        private double FontWidth 
        { 
            get 
            { 
                return 5 * InterChangeWidth; 
            } 
        }
        private double FontRatio { get { return Math.Round(FontWidth / 10, 1); } }

        //public properties
        public readonly int Dots;

        // constructor
        public Nordam2DLabel(double height, double width, int dots, bool millimeters, Dictionary<string, string> labelValues)
        {
            this.LabelValues = labelValues;
            this.Dots = dots;
            setHeightInMM = new Lazy<Double>(() =>
                {
                    if (millimeters)
                    {
                        return height;
                    }
                    else
                    {
                        return height.ToMillimeters();
                    }
                }).Value;
            setWidthInMM = new Lazy<Double>(() =>
                {
                    if (millimeters)
                    {
                        return width;
                    }
                    else
                    {
                        return width.ToMillimeters();
                    }
                }).Value;

        }

        // private methods
        private string SetFontSize()
        {
            //CF is the Font settings for Zebra print
            //CFC says to use the font setting "C" which is fixed width
            //The Format label Nordam gave had the text ~5 millimeters
            //'C' font has default size 18 dots
            return String.Format("^CFC,{0},10", FontRatio * 18 ); 
        }

        private string LabelFormat(string displayValue, int column, bool newRow = false, bool bold = false, bool barcode = false)
        {
            StringBuilder labelOutput = new StringBuilder();

            double rowVal = newRow ? GetNextRow() : RowVal;

            //FO is field origin 
            labelOutput.AppendFormat("^FO{0},{1}", GetColumnOffset(column), RowVal);

            if(barcode)
            {
                // Nordam specifies to use the 200 Barcode format
                labelOutput.AppendFormat("^BXN,{0},200", GetBarcodeHeight());
            }
            labelOutput.AppendFormat("^FD{0}^FS", displayValue);
            if(bold)
            {
                // There's not a good way to make the printer print in bold
                // you can "hack" it by printing the same thing with a small offset above and below
                labelOutput.AppendFormat("^FO{0},{1}^FD{2}^FS", GetColumnOffset(column), RowVal - 0.5, displayValue);
                labelOutput.AppendFormat("^FO{0},{1}^FD{2}^FS", GetColumnOffset(column), RowVal + 0.5, displayValue);
            }

            return labelOutput.ToString();
        }

        private double GetNextRow()
        {
            return RowVal += Math.Round(((HeightMM - 10) / 8) * Dots, 4);
        }

        private double GetColumnOffset(int colNum)
        {
            string longestWord;
            int letterVal;
            switch(colNum)
            {
                case 0:
                    // Nordam has the left margin 5 mm in
                    return 5 * this.Dots;
                case 1:
                    // Nordam has the second column 3 mm from the end of the largest word
                    // the field width for each letter on font C is 10
                    // with a 2 dot interchange
                    longestWord = "Expiration Date";
                    return (3 *  this.Dots) + ((FontWidth * longestWord.Length) + ((longestWord.Length - 1) * InterChangeWidth )) + GetColumnOffset(0);
                case 2:
                    // the Third column lines up with the 21th letter in the Nordam display
                    // the field width for each letter on font C is 10
                    // with a 2 dot interchange
                    letterVal = 21;
                    return (FontWidth * letterVal) + ((letterVal -1) * InterChangeWidth) + GetColumnOffset(1);
                case 3:
                    // the Fourth column lines up with the 30th letter in the Nordam display
                    // this means it's 10 letters plus case 2
                    // the field width for each letter on font C is 10
                    // with a 2 dot interchange
                    letterVal = 10;
                    return (FontWidth * letterVal) + ((letterVal -1) * InterChangeWidth) + GetColumnOffset(2);
                default:
                    break;
            }
            return 0;
        }

        private int GetBarcodeHeight()
        {
            // Nordam says that the individual pieces of the display
            // cannot exceed 0.635 mm
            return (int)(0.635 * this.Dots);
        }


        // public methods
        public string GetLabel()
        {
            RowVal = 3 * this.Dots; //Nordam has their first row at 3 mm from the top

            StringBuilder label = new StringBuilder();
            label.AppendLine("^XA"); // label begins with ^XA
            label.AppendLine(SetFontSize());
            // Add Label stuff from LabelFormat
            StringBuilder barcodeData = new StringBuilder();
            //barcodeData.Append(Regex.Replace(String.Join("", LabelValues.Keys.Select((x) => String.Format("{0}{1}:", x, LabelValues[x])).ToArray()), @"^(.*)\:$", "$1"));
            barcodeData.Append(String.Join(":", LabelValues.Keys.Select((x) => String.Format("{0}{1}", x, LabelValues[x])).ToArray()));

            // Add Purchase Order
            label.AppendLine(LabelFormat("PO #", 0, newRow: false, bold: true));
            label.AppendLine(LabelFormat(LabelValues["PO"], 1, newRow: false, bold: false));

            // Line ItemLine should be on the same row as Purchase order
            label.AppendLine(LabelFormat("Line Item", 2, newRow: false, bold: true));
            label.AppendLine(LabelFormat(LabelValues["L"], 3, newRow: false, bold: false));

            //Add Part NLineumber
            label.AppendLine(LabelFormat("Part #", 0, newRow: true, bold: true));
            label.AppendLine(LabelFormat(LabelValues["P"], 1, newRow: false, bold: false));

            //Add DescriLineption
            label.AppendLine(LabelFormat("Desc", 0, newRow: true, bold: true));
            label.AppendLine(LabelFormat(LabelValues["D"], 1, newRow: false, bold: false));

            //Add QuantiLinety
            label.AppendLine(LabelFormat("Quantity", 0, newRow: true, bold: true));
            label.AppendLine(LabelFormat(LabelValues["Q"], 1, newRow: false, bold: false));

            // UoM shoulLined be on the same row as Quantity
            label.AppendLine(LabelFormat("UoM", 2, newRow: false, bold: true));
            label.AppendLine(LabelFormat(LabelValues["U"], 3, newRow: false, bold: false));

            //Add PackinLineg Slip
            label.AppendLine(LabelFormat("Packing Slip #", 0, newRow: true, bold: true));
            label.AppendLine(LabelFormat(LabelValues["PS"], 1, newRow: false, bold: false));

            // Barcode sLinehould be on the same row as Packing Slip
            label.AppendLine(LabelFormat(barcodeData.ToString(), 2, newRow: false, bold: false, barcode: true));

            //Add ExpiraLinetion Date
            label.AppendLine(LabelFormat("Expiration Date", 0, newRow: true, bold: true));
            label.AppendLine(LabelFormat(LabelValues["ED"], 1, newRow: false, bold: false));

            //Add MFG BaLinetch Number
            label.AppendLine(LabelFormat("MFG Batch #", 0, newRow: true, bold: true));
            label.AppendLine(LabelFormat(LabelValues["MB"], 1, newRow: false, bold: false));

            //Add SerialLine Number
            label.AppendLine(LabelFormat("Serial #", 0, newRow: true, bold: true));
            label.AppendLine(LabelFormat(LabelValues["SN"], 1, newRow: false, bold: false));

            label.AppendLine("^XZ");
            return label.ToString();
        }

        public double HeightInches { get { return Math.Round(HeightMM.ToInches(), 1); }}
        public double HeightMM{ get { return setHeightInMM; }}

        public double WidthInches { get { return Math.Round(WidthMM.ToInches(), 1); }}
        public double WidthMM{ get { return setWidthInMM; }}
    }

    public static class Conversions
    {
        public static double ToInches(this double input)
        {
            // 1 millimeter == 0.039 inches
            return input * 0.039;
        }

        public static double ToMillimeters(this double input)
        {
            // 1 inch == 25.400 Millimeters
            return input * 25.400;
        }
    }
}
