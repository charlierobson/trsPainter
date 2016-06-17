using System;
using System.ComponentModel.Design;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace trspainter
{
    public partial class Form1 : Form
    {
        private float _grDx, _grDy;
        private int _grXCell, _grYCell;

        private float _chDx, _chDy;
        private int _chXCell, _chYCell;

        private Bitmap _canvas;

        private bool _drawing, _erasing;

        private readonly bool[,] _pixels;

        private bool _greenScreen;
        private bool _solidBgMode = true;

        private readonly Brush _darkBrush;
        private readonly Brush _slightlyLessDarkBrush;
        private bool _changes;

        private string _currentFile;

        private string CurrentFile
        {
            get { return _currentFile; }

            set
            {
                _currentFile = value;

                Text = @"TRS Pixel Graphic Designer";
                if (!string.IsNullOrWhiteSpace(_currentFile))
                {
                    Text += $" : {_currentFile}";
                }

                if (_changes)
                {
                    Text += @" *";
                }
            }
        }

        public Form1()
        {
            _darkBrush = new SolidBrush(Color.FromArgb(255, 40, 40, 40));
            _slightlyLessDarkBrush = new SolidBrush(Color.FromArgb(255, 50, 50, 50));

            _pixels = new bool[128, 48];

            InitializeComponent();
            OnResizeHandler();
        }

        private int ClientWidthAdjusted => ClientSize.Width;
        private int ClientHeightAdjusted => ClientSize.Height - menuStrip1.Height;

        private void OnResizeHandler()
        {
            _grDx = (float)ClientWidthAdjusted / 128;
            _grDy = (float)ClientHeightAdjusted / 48;

            _chDx = (float)ClientWidthAdjusted / 64;
            _chDy = (float)ClientHeightAdjusted / 16;

            _canvas = new Bitmap(ClientWidthAdjusted, ClientHeightAdjusted);

            var g = Graphics.FromImage(_canvas);

            for (var y = 0; y < 48; ++y)
            {
                for (var x = 0; x < 128; ++x)
                {
                    var hatch = (((y/3) & 1) ^ ((x/2) & 1)) == 0;
                    hatch |= _solidBgMode;

                    g.FillRectangle(_pixels[x,y] ? _greenScreen ? Brushes.LawnGreen : Brushes.WhiteSmoke : hatch ? _darkBrush: _slightlyLessDarkBrush, x*_grDx, y*_grDy, _grDx, _grDy);
                }
            }

            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            OnResizeHandler();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var pos = e.Location;

            var cxCell = (int)Math.Floor(pos.X / _grDx);
            var cyCell = (int)Math.Floor((pos.Y - menuStrip1.Height) / _grDy);

            _chXCell = (int)Math.Floor(pos.X / _chDx);
            _chYCell = (int)Math.Floor((pos.Y - menuStrip1.Height) / _chDy);

            labelCharCell.Text = $"{_chXCell},{_chYCell}";
            labelSubPix.Text = $"{_grXCell},{_grYCell}";

            if (cxCell == _grXCell && cyCell == _grYCell) return;

            _grXCell = cxCell;
            _grYCell = cyCell;

            if (_drawing)
            {
                DrawCell();
            }
            Invalidate();
        }

        private void DrawCell()
        {
            if (_grXCell < 0 || _grXCell > 127 || _grYCell < 0 || _grYCell > 48) return;

            _pixels[_grXCell, _grYCell] = _erasing == false;

            var hatch = ((_chYCell & 1) ^ (_chXCell & 1)) == 0;
            hatch |= _solidBgMode;

            Graphics.FromImage(_canvas)
                .FillRectangle(_pixels[_grXCell, _grYCell] ? _greenScreen ? Brushes.LawnGreen : Brushes.WhiteSmoke : (hatch ? _darkBrush : _slightlyLessDarkBrush), _grXCell * _grDx, _grYCell * _grDy, _grDx, _grDy);

            Invalidate();

            _changes = true;
            CurrentFile = CurrentFile; // to put the '*' on the name
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            _erasing = _pixels[_grXCell, _grYCell];
            _drawing = true;

            DrawCell();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _erasing = false;
            _drawing = false;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.DrawImage(_canvas, 0, menuStrip1.Height);
        }

        private void DoSaveToCurrentFile()
        {
            var total = 0;
            var output = new StringBuilder();

            for (var y = 0; y < 48; y += 3)
            {
                for (var x = 0; x < 128; x += 2)
                {
                    var c = 0;

                    if (_pixels[x, y]) c |= 1;
                    if (_pixels[x + 1, y]) c |= 2;

                    if (_pixels[x, y + 1]) c |= 4;
                    if (_pixels[x + 1, y + 1]) c |= 8;

                    if (_pixels[x, y + 2]) c |= 16;
                    if (_pixels[x + 1, y + 2]) c |= 32;

                    output.Append($"{c + 128}");

                    ++total;
                    output.Append((total & 63) == 0 ? "\n" : ", ");
                }
            }

            File.WriteAllText(CurrentFile, output.ToString());

            Change(false);
        }

        private void Change(bool changes)
        {
            _changes = changes;
            CurrentFile = CurrentFile;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentFile == null)
            {
                var s = new SaveFileDialog();
                var result = s.ShowDialog();
                if (result == DialogResult.OK)
                {
                    CurrentFile = s.FileName;
                }
                else return;
            }
            DoSaveToCurrentFile();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var s = new SaveFileDialog();
            var result = s.ShowDialog();
            if (result == DialogResult.OK)
            {
                CurrentFile = s.FileName;
            }
            else return;

            DoSaveToCurrentFile();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var o = new OpenFileDialog();;
            var result = o.ShowDialog();
            if (result != DialogResult.OK) return;

            Array.Clear(_pixels, 0, _pixels.Length);

            var input = File.ReadAllLines(o.FileName);

            CurrentFile = o.FileName;

            var ints = new int[64*16];
            var splits = new[] { ',' };

            var c = 0;
            foreach (var n in from line in input select line.Trim().Split(splits, StringSplitOptions.RemoveEmptyEntries) into elements from element in elements select int.Parse(element))
            {
                ints[c] = n;
                ++c;
            }

            c = 0;
            foreach (var n in ints)
            {
                var x = (c & 63) * 2;
                var y = (c / 64) * 3;
                ++c;

                _pixels[x, y] = (n & 1) != 0;
                _pixels[x + 1, y] = (n & 2) != 0;
                _pixels[x, y + 1] = (n & 4) != 0;
                _pixels[x + 1, y + 1] = (n & 8) != 0;
                _pixels[x, y + 2] = (n & 16) != 0;
                _pixels[x + 1, y + 2] = (n & 32) != 0;
            }

            OnResizeHandler();

            Change(false);
        }

        private void hatchedBackgroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _solidBgMode = !((ToolStripMenuItem)sender).Checked;
            OnResizeHandler();
        }

        private void greenScreenVDUToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _greenScreen = ((ToolStripMenuItem)sender).Checked;
            OnResizeHandler();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_changes && MessageBox.Show("You have unsaved changes. Continue with new?", "Unrecoverable Action", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            Array.Clear(_pixels, 0, _pixels.Length);
            OnResizeHandler();

            CurrentFile = null;
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_changes && MessageBox.Show("You have unsaved changes. Continue with quit?", "Unrecoverable Action", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            Close();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_changes && MessageBox.Show("You have unsaved changes. Continue with clear?", "Unrecoverable Action", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            Array.Clear(_pixels, 0, _pixels.Length);
            OnResizeHandler();
        }

        private void InvertPixels()
        {
            for (var y = 0; y < 48; ++y)
            {
                for (var x = 0; x < 128; ++x)
                {
                    _pixels[x, y] = !_pixels[x,y];
                }
            }
        }

        private void fillToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_changes && MessageBox.Show("You have unsaved changes. Continue with fill?", "Unrecoverable Action", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            Array.Clear(_pixels, 0, _pixels.Length);
            InvertPixels();
            OnResizeHandler();

            Change(false);
        }

        private void invertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InvertPixels();
            OnResizeHandler();
        }
    }
}
