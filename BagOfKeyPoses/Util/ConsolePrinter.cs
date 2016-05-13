using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Util
{
    public static class ConsolePrinter
    {
        static int tableWidth = 50;

        public static string getArrayString(double[,] array, List<string> verticalLabels, List<string> horizontalLabels = null)
        {
            string s = "";

            tableWidth = verticalLabels.Count * 7;
            int tableHeadPadding = 0;

            if(horizontalLabels != null)
            {
                foreach(string label in horizontalLabels)
                {
                    int width = label.Length;
                    if (width > tableHeadPadding)
                        tableHeadPadding = width;
                }
            }
            tableHeadPadding += 3;

            s += new string(' ', tableHeadPadding);
            s += PrintLine();
            s += new string(' ', tableHeadPadding);
            s += PrintRow(verticalLabels);
            s += new string(' ', tableHeadPadding);
            s += PrintLine();

            for (int i = 0; i < array.Length / verticalLabels.Count; i++)
            {
                string line = "";
                if (horizontalLabels != null)
                    s += PrintHorizontalLabel(horizontalLabels[i]);
                for (int j = 0; j < array.Length / verticalLabels.Count; j++)
                {
                    if (j == array.Length / 2 - 1)
                        line += "" + array[i, j];
                    else
                        line += "" + array[i, j] + ";";
                }
                List<string> splitted = line.Split(';').ToList<string>();
                splitted.RemoveAt(splitted.Count-1);

                s += PrintRow(splitted);
            }

            return s;
        }

        static string PrintLine()
        {
            return new string('-', tableWidth)+"\n";
        }

        static string PrintRow(List<string> columns)
        {
            int width = (tableWidth - columns.Count) / columns.Count;
            string row = "|";

            foreach (string column in columns)
            {
                row += AlignCentre(column, width) + "|";
            }

            return row + "\n";
        }

        static string PrintHorizontalLabel(string label)
        {
            int width = label.Length;
            string row = "|";

            row += AlignCentre(label, width) + "|";

            return row;
        }

        static string AlignCentre(string text, int width)
        {
            text = text.Length > width ? text.Substring(0, width - 1) + " " : text;

            if (string.IsNullOrEmpty(text))
            {
                return new string(' ', width);
            }
            else
            {
                return text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
            }
        }

    }
}
