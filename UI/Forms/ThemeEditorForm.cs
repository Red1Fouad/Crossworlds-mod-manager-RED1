using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public class ThemeEditorForm : Form
    {
        private SerializableTheme _theme;
        private PictureBox _previewBox = null!;
        public SerializableTheme ResultTheme => _theme;

        public ThemeEditorForm(SerializableTheme theme)
        {
            // Create a copy so we don't modify the settings directly until saved
            _theme = new SerializableTheme
            {
                BackColor = theme.BackColor,
                ForeColor = theme.ForeColor,
                ControlBackColor = theme.ControlBackColor,
                ControlForeColor = theme.ControlForeColor,
                ButtonBackColor = theme.ButtonBackColor,
                ButtonForeColor = theme.ButtonForeColor,
                AccentColor = theme.AccentColor,
                PlayButtonColor = theme.PlayButtonColor,
                BorderColor = theme.BorderColor,
                MenuBackColor = theme.MenuBackColor,
                MenuForeColor = theme.MenuForeColor
            };
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.Text = "Edit Custom Theme";
            this.Size = new Size(800, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var mainContainer = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 300, IsSplitterFixed = true };

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            AddColorPicker(layout, "Background", () => Color.FromArgb(_theme.BackColor), c => _theme.BackColor = c.ToArgb());
            AddColorPicker(layout, "Control Background", () => Color.FromArgb(_theme.ControlBackColor), c => _theme.ControlBackColor = c.ToArgb());
            AddColorPicker(layout, "Button Background", () => Color.FromArgb(_theme.ButtonBackColor), c => _theme.ButtonBackColor = c.ToArgb());
            AddColorPicker(layout, "Menu Background", () => Color.FromArgb(_theme.MenuBackColor), c => _theme.MenuBackColor = c.ToArgb());
            AddColorPicker(layout, "Accent Color", () => Color.FromArgb(_theme.AccentColor), c => _theme.AccentColor = c.ToArgb());

            _previewBox = new PictureBox 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle 
            };
            _previewBox.Paint += PreviewBox_Paint;
            
            mainContainer.Panel1.Controls.Add(layout);
            mainContainer.Panel2.Controls.Add(_previewBox);
            mainContainer.Panel2.Padding = new Padding(10);

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.RightToLeft };
            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, Size = new Size(80, 30) };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(63, 63, 70), ForeColor = Color.White, Size = new Size(80, 30) };
            
            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnOk);

            this.Controls.Add(mainContainer);
            this.Controls.Add(btnPanel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void PreviewBox_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var r = _previewBox.ClientRectangle;
            
            // Background
            using (var b = new SolidBrush(Color.FromArgb(_theme.BackColor))) g.FillRectangle(b, r);

            // Menu Strip
            var menuRect = new Rectangle(0, 0, r.Width, 24);
            using (var b = new SolidBrush(Color.FromArgb(_theme.MenuBackColor))) g.FillRectangle(b, menuRect);
            using (var b = new SolidBrush(Color.FromArgb(_theme.MenuForeColor))) 
                g.DrawString("File   Tools   Help", SystemFonts.DefaultFont, b, 5, 5);

            // Status Strip
            var statusRect = new Rectangle(0, r.Height - 22, r.Width, 22);
            using (var b = new SolidBrush(Color.FromArgb(_theme.AccentColor))) g.FillRectangle(b, statusRect);
            using (var b = new SolidBrush(Color.White)) 
                g.DrawString("Ready", SystemFonts.DefaultFont, b, 5, r.Height - 18);

            // Fake Mod List (Left side)
            var listRect = new Rectangle(10, 34, 150, r.Height - 80);
            using (var b = new SolidBrush(Color.FromArgb(_theme.ControlBackColor))) g.FillRectangle(b, listRect);
            using (var p = new Pen(Color.FromArgb(_theme.BorderColor))) g.DrawRectangle(p, listRect);
            
            // List Header
            var headerRect = new Rectangle(listRect.X, listRect.Y, listRect.Width, 20);
            using (var b = new SolidBrush(Color.FromArgb(_theme.ButtonBackColor))) g.FillRectangle(b, headerRect);
            using (var p = new Pen(Color.FromArgb(_theme.BorderColor))) g.DrawRectangle(p, headerRect);
            using (var b = new SolidBrush(Color.FromArgb(_theme.ButtonForeColor))) 
                g.DrawString("Mod Name", SystemFonts.DefaultFont, b, headerRect.X + 2, headerRect.Y + 3);

            // List Item (Selected)
            var itemRect = new Rectangle(listRect.X + 1, listRect.Y + 21, listRect.Width - 2, 18);
            using (var b = new SolidBrush(Color.FromArgb(_theme.ButtonBackColor))) g.FillRectangle(b, itemRect);
            using (var b = new SolidBrush(Color.FromArgb(_theme.MenuForeColor))) 
                g.DrawString("Selected Mod", SystemFonts.DefaultFont, b, itemRect.X + 2, itemRect.Y + 2);

            // Fake Mod Card (Right side)
            var cardRect = new Rectangle(170, 34, 140, 180);
            using (var b = new SolidBrush(Color.FromArgb(_theme.ButtonBackColor))) g.FillRectangle(b, cardRect);
            using (var p = new Pen(Color.FromArgb(_theme.BorderColor))) g.DrawRectangle(p, cardRect);
            
            // Card Image placeholder
            var imgRect = new Rectangle(cardRect.X + 10, cardRect.Y + 10, cardRect.Width - 20, 80);
            using (var b = new SolidBrush(Color.Black)) g.FillRectangle(b, imgRect);
            
            // Card Text
            using (var b = new SolidBrush(Color.FromArgb(_theme.ButtonForeColor))) 
                g.DrawString("Mod Title", new Font(SystemFonts.DefaultFont, FontStyle.Bold), b, cardRect.X + 10, cardRect.Y + 95);
            
            // Card Button
            var cardBtnRect = new Rectangle(cardRect.X + 10, cardRect.Y + 140, cardRect.Width - 20, 30);
            using (var b = new SolidBrush(Color.FromArgb(_theme.AccentColor))) g.FillRectangle(b, cardBtnRect);
            using (var b = new SolidBrush(Color.White)) 
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("Download", SystemFonts.DefaultFont, b, cardBtnRect, sf);
            }
        }

        private void AddColorPicker(TableLayoutPanel layout, string name, Func<Color> getter, Action<Color> setter)
        {
            var lbl = new Label { Text = name, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ThemeManager.CurrentTheme.ForeColor };
            var pnl = new Panel { Height = 25, Width = 50, BorderStyle = BorderStyle.FixedSingle, BackColor = getter(), Cursor = Cursors.Hand, Tag = "ColorSwatch" };
            
            pnl.Click += (s, e) => {
                using (var cd = new ColorDialog())
                {
                    cd.Color = pnl.BackColor;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        pnl.BackColor = cd.Color;
                        setter(cd.Color);
                        ThemeManager.ReloadCustomTheme(_theme);
                        ThemeManager.ApplyTheme(this);
                        _previewBox.Invalidate();
                    }
                }
            };

            layout.RowCount++;
            layout.Controls.Add(lbl);
            layout.Controls.Add(pnl);
        }
    }
}