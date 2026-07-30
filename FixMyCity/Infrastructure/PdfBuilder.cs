using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FixMyCity.Service
{
    public class PdfBuilder
    {
        private const double PageWidth = 595.28;
        private const double PageHeight = 841.89;

        private class PageContent
        {
            public StringBuilder Ops = new StringBuilder();
        }

        private readonly List<PageContent> _pages = new List<PageContent>();

        public double PageTop { get { return PageHeight - 40; } }
        public double PageBottom { get { return 40; } }

        public PdfBuilder()
        {
            AddPage();
        }

        public int AddPage()
        {
            _pages.Add(new PageContent());
            return _pages.Count - 1;
        }

        public int CurrentPageIndex { get { return _pages.Count - 1; } }
        private class RgbColor
        {
            public double R { get; set; }
            public double G { get; set; }
            public double B { get; set; }
        }
        private static RgbColor Hex(string hex)
        {
            hex = hex.TrimStart('#');
            return new RgbColor
            {
                R = Convert.ToInt32(hex.Substring(0, 2), 16) / 255.0,
                G = Convert.ToInt32(hex.Substring(2, 2), 16) / 255.0,
                B = Convert.ToInt32(hex.Substring(4, 2), 16) / 255.0
            };
        }

        public void AddText(int pageIndex, double x, double y, string text, double fontSize,
            bool bold = false, string colorHex = "#000000")
        {
            var c = Hex(colorHex);
            string font = bold ? "/F2" : "/F1";
            _pages[pageIndex].Ops.AppendLine(string.Format(
                "{0} {1} {2} rg BT {3} {4} Tf 1 0 0 1 {5} {6} Tm ({7}) Tj ET 0 0 0 rg",
                c.R.ToString("0.###"), c.G.ToString("0.###"), c.B.ToString("0.###"),
                font, fontSize.ToString("0.##"),
                x.ToString("0.##"), y.ToString("0.##"),
                Escape(text)));
        }

        public void AddLine(int pageIndex, double x1, double y1, double x2, double y2,
            double width = 0.5, string colorHex = "#CCCCCC")
        {
            var c = Hex(colorHex);
            _pages[pageIndex].Ops.AppendLine(string.Format(
                "{0} {1} {2} RG {3} w {4} {5} m {6} {7} l S 0 0 0 RG",
               c.R.ToString("0.###"), c.G.ToString("0.###"), c.B.ToString("0.###"),
                width.ToString("0.##"),
                x1.ToString("0.##"), y1.ToString("0.##"),
                x2.ToString("0.##"), y2.ToString("0.##")));
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Replace("₹", "Rs.");
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c == '(' || c == ')' || c == '\\') sb.Append('\\');
                sb.Append(c > 255 ? '?' : c);
            }
            return sb.ToString();
        }

        public byte[] Build()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                List<int> offsets = new List<int>();
                Write(ms, "%PDF-1.4\n");

                int pagesRootObj = 1;
                int fontRegularObj = 1 + _pages.Count * 2 + 1;
                int fontBoldObj = fontRegularObj + 1;
                int catalogObj = fontBoldObj + 1;

                StartObj(ms, offsets, pagesRootObj);
                List<int> kids = new List<int>();
                for (int i = 0; i < _pages.Count; i++) kids.Add(2 + i * 2);
                StringBuilder kidsRef = new StringBuilder();
                foreach (int k in kids) kidsRef.Append(k + " 0 R ");
                Write(ms, "<< /Type /Pages /Kids [" + kidsRef.ToString() + "] /Count " + _pages.Count + " >>\nendobj\n");

                for (int i = 0; i < _pages.Count; i++)
                {
                    int pageObj = 2 + i * 2;
                    int contentObj = pageObj + 1;

                    StartObj(ms, offsets, pageObj);
                    Write(ms,
                        "<< /Type /Page /Parent " + pagesRootObj +
                        " 0 R /MediaBox [0 0 " + PageWidth.ToString("0.##") + " " + PageHeight.ToString("0.##") +
                        "] /Resources << /Font << /F1 " + fontRegularObj +
                        " 0 R /F2 " + fontBoldObj +
                        " 0 R >> >> /Contents " + contentObj + " 0 R >>\nendobj\n");

                    string content = _pages[i].Ops.ToString();
                    StartObj(ms, offsets, contentObj);
                    Write(ms, "<< /Length " + Encoding.ASCII.GetByteCount(content) + " >>\nstream\n");
                    Write(ms, content);
                    Write(ms, "endstream\nendobj\n");
                }

                StartObj(ms, offsets, fontRegularObj);
                Write(ms, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

                StartObj(ms, offsets, fontBoldObj);
                Write(ms, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\nendobj\n");

                StartObj(ms, offsets, catalogObj);
                Write(ms, "<< /Type /Catalog /Pages " + pagesRootObj + " 0 R >>\nendobj\n");

                int xref = (int)ms.Position;
                Write(ms, "xref\n0 " + (catalogObj + 1) + "\n");
                Write(ms, "0000000000 65535 f \n");
                foreach (int off in offsets)
                    Write(ms, off.ToString("D10") + " 00000 n \n");

                Write(ms, "trailer\n<< /Size " + (catalogObj + 1) + " /Root " + catalogObj + " 0 R >>\n");
                Write(ms, "startxref\n" + xref + "\n%%EOF");

                return ms.ToArray();
            }
        }

        private static void Write(MemoryStream ms, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            ms.Write(bytes, 0, bytes.Length);
        }

        private static void StartObj(MemoryStream ms, List<int> offsets, int number)
        {
            offsets.Add((int)ms.Position);
            Write(ms, number + " 0 obj\n");
        }

        private static readonly Dictionary<char, int> HelveticaWidths = new Dictionary<char, int>
        {
            {' ',278},{'!',278},{'"',355},{'#',556},{'$',556},{'%',889},{'&',667},{'\'',191},
            {'(',333},{')',333},{'*',389},{'+',584},{',',278},{'-',333},{'.',278},{'/',278},
            {'0',556},{'1',556},{'2',556},{'3',556},{'4',556},{'5',556},{'6',556},{'7',556},
            {'8',556},{'9',556},{':',278},{';',278},{'<',584},{'=',584},{'>',584},{'?',556},
            {'@',1015},{'A',667},{'B',667},{'C',722},{'D',722},{'E',667},{'F',611},{'G',778},
            {'H',722},{'I',278},{'J',500},{'K',667},{'L',556},{'M',833},{'N',722},{'O',778},
            {'P',667},{'Q',778},{'R',722},{'S',667},{'T',611},{'U',722},{'V',667},{'W',944},
            {'X',667},{'Y',667},{'Z',611},{'a',556},{'b',556},{'c',500},{'d',556},{'e',556},
            {'f',278},{'g',556},{'h',556},{'i',222},{'j',222},{'k',500},{'l',222},{'m',833},
            {'n',556},{'o',556},{'p',556},{'q',556},{'r',333},{'s',500},{'t',278},{'u',556},
            {'v',500},{'w',722},{'x',500},{'y',500},{'z',500}
        };

        public double TextWidth(string text, double fontSize, bool bold = false)
        {
            double total = 0;
            foreach (char c in text ?? string.Empty)
            {
                int w;
                total += HelveticaWidths.TryGetValue(c, out w) ? w : 556;
            }
            return total * fontSize / 1000.0;
        }

        public void AddTextRightAligned(int pageIndex, double rightX, double y, string text,
            double fontSize, bool bold = false, string colorHex = "#000000")
        {
            double width = TextWidth(text, fontSize, bold);
            AddText(pageIndex, rightX - width, y, text, fontSize, bold, colorHex);
        }

        public void AddRect(int pageIndex, double x, double y, double width, double height,
            double lineWidth = 0.5, string colorHex = "#CCCCCC")
        {
            var c = Hex(colorHex);
            _pages[pageIndex].Ops.AppendLine(string.Format(
                "{0} {1} {2} RG {3} w {4} {5} {6} {7} re S 0 0 0 RG",
                c.R.ToString("0.###"), c.G.ToString("0.###"), c.B.ToString("0.###"),
                lineWidth.ToString("0.##"),
                x.ToString("0.##"), y.ToString("0.##"),
                width.ToString("0.##"), height.ToString("0.##")));
        }

        public void AddFilledRect(int pageIndex, double x, double y, double width, double height,
            string colorHex = "#FFFFFF")
        {
            var c = Hex(colorHex);
            _pages[pageIndex].Ops.AppendLine(string.Format(
                "{0} {1} {2} rg {3} {4} {5} {6} re f 0 0 0 rg",
                 c.R.ToString("0.###"), c.G.ToString("0.###"), c.B.ToString("0.###"),
                x.ToString("0.##"), y.ToString("0.##"),
                width.ToString("0.##"), height.ToString("0.##")));
        }
    }
}