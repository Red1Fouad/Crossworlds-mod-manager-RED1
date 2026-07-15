using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public partial class GameBananaBrowserForm : Form
    {
        private const int GameId = 21640; // Game ID for Sonic Racing: Crossworlds on GameBanana
        private int _currentPage = 1;
        private string _currentSearch = "";
        private bool _isLoading = false;
        private readonly IProgress<string>? _logger;
        private readonly Action? _onModsChanged;

        private FlowLayoutPanel flowLayoutPanelMods = null!;
        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private Button btnPrevPage = null!;
        private Button btnNextPage = null!;
        private Label lblPage = null!;

        public GameBananaBrowserForm(IProgress<string>? logger = null, Action? onModsChanged = null)
        {
            _logger = logger;
            _onModsChanged = onModsChanged;
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.flowLayoutPanelMods = new FlowLayoutPanel();
            var pnlBottom = new Panel();
            var pnlTop = new Panel();

            // Form
            this.Text = "Browse GameBanana Mods";
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.ClientSize = new Size(960, 600);
            this.MinimumSize = new Size(640, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Load += async (s, e) => await LoadModsAsync();
            this.Resize += OnFormResized;

            // Initialize controls that were previously in the constructor
            this.txtSearch = new TextBox();
            this.btnSearch = new Button();
            this.btnPrevPage = new Button();
            this.btnNextPage = new Button();
            this.lblPage = new Label();

            // Top Panel (Search)
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 40;
            pnlTop.Padding = new Padding(5);
            pnlTop.BackColor = Color.FromArgb(45, 45, 48);

            // Search TextBox
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.BackColor = Color.FromArgb(63, 63, 70);
            txtSearch.ForeColor = Color.Gray; // Placeholder color
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.Text = "Search mods...";
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnSearch.PerformClick(); };
            txtSearch.Enter += TxtSearch_Enter;
            txtSearch.Leave += TxtSearch_Leave;

            // Search Button
            btnSearch.Dock = DockStyle.Right;
            btnSearch.Text = "Search";
            btnSearch.Width = 75;
            btnSearch.Enabled = true;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btnSearch.Click += btnSearch_Click;

            pnlTop.Controls.Add(txtSearch);
            pnlTop.Controls.Add(btnSearch);

            // FlowLayoutPanel for mods
            flowLayoutPanelMods.Dock = DockStyle.Fill;
            flowLayoutPanelMods.AutoScroll = true;
            flowLayoutPanelMods.BackColor = Color.FromArgb(37, 37, 38);
            flowLayoutPanelMods.Padding = new Padding(10);
            flowLayoutPanelMods.Resize += OnFormResized; // Use the same handler



            // Bottom Panel (Pagination)
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 40;
            pnlBottom.Padding = new Padding(5);
            pnlBottom.BackColor = Color.FromArgb(45, 45, 48);

            // Page Buttons and Label
            btnPrevPage.Text = "< Prev";
            btnPrevPage.Dock = DockStyle.Left;
            btnPrevPage.Click += btnPrevPage_Click;
            btnPrevPage.FlatStyle = FlatStyle.Flat;
            btnPrevPage.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);

            lblPage.Text = "Page 1";
            lblPage.Dock = DockStyle.Fill;
            lblPage.TextAlign = ContentAlignment.MiddleCenter;

            btnNextPage.Text = "Next >";
            btnNextPage.Dock = DockStyle.Right;
            btnNextPage.Click += btnNextPage_Click;
            btnNextPage.FlatStyle = FlatStyle.Flat;
            btnNextPage.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);

            pnlBottom.Controls.Add(lblPage);
            pnlBottom.Controls.Add(btnPrevPage);
            pnlBottom.Controls.Add(btnNextPage);

            // Add controls to form
            this.Controls.Add(flowLayoutPanelMods);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private void TxtSearch_Enter(object? sender, EventArgs e)
        {
            if (txtSearch.Text == "Search mods...")
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = Color.White;
            }
        }

        private void TxtSearch_Leave(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Search mods...";
                txtSearch.ForeColor = Color.Gray;
            }
        }

        private void OnFormResized(object? sender, EventArgs e)
        {
            UpdateTableLayout();
        }

        private void UpdateTableLayout()
        {
            // Ensure there are controls and the first one is a ModCardControl before proceeding.
            // This prevents errors during initial load or when the panel is empty.
            if (flowLayoutPanelMods.Controls.Count == 0 || flowLayoutPanelMods.Controls[0] is not ModCardControl sampleCard)
            {
                return;
            }

            int cardWidth = sampleCard.Width + sampleCard.Margin.Horizontal;
            int containerWidth = flowLayoutPanelMods.ClientSize.Width;
            int newColumnCount = Math.Max(1, containerWidth / cardWidth);

            // Calculate the total width of the cards for the number of columns that fit
            int totalCardWidth = newColumnCount * cardWidth;

            // Calculate the horizontal padding needed to center the block of cards
            int horizontalPadding = (containerWidth - totalCardWidth) / 2;

            // Apply the padding. Ensure it's not negative.
            flowLayoutPanelMods.Padding = new Padding(Math.Max(10, horizontalPadding), 10, Math.Max(10, horizontalPadding), 10);
        }

        private async Task LoadModsAsync(int page = 1, string search = "")
        {
            if (_isLoading) return;
            _isLoading = true;
            btnSearch.Enabled = false;
            btnPrevPage.Enabled = false;
            btnNextPage.Enabled = false;
            flowLayoutPanelMods.Controls.Clear();
            _logger?.Report($"Loading mods page {page} with search term: '{search}'...");
            var loadingLabel = new Label { Text = "Loading mods...", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(this.Font.FontFamily, 16) };
            flowLayoutPanelMods.Controls.Add(loadingLabel);
            _currentSearch = search; // Keep this to maintain state, even if unused by API

            try
            {
                var mods = await GameBananaApiService.SearchModsAsync(GameId, page, search);
                flowLayoutPanelMods.Controls.Clear();

                if (mods == null || !mods.Any())
                {
                    _logger?.Report("No mods found for the current query.");
                    flowLayoutPanelMods.Controls.Add(new Label { Text = "No mods found.", AutoSize = true });
                }
                else
                {
                    foreach (var mod in mods)
                    {
                        var card = new ModCardControl(mod);
                        card.DownloadClicked += OnModDownloadClicked;
                        ThemeManager.ApplyTheme(card); // Apply theme to dynamically added control
                        flowLayoutPanelMods.Controls.Add(card);
                    }
                    _logger?.Report($"Successfully loaded {mods.Count} mod(s).");
                }

                _currentPage = page;
                _currentSearch = search;
                lblPage.Text = $"Page {_currentPage}";
                btnPrevPage.Enabled = _currentPage > 1;
                btnNextPage.Enabled = mods?.Count > 0; // Simple check; assumes more pages if results are returned
                
                UpdateTableLayout(); // Adjust grid after loading
            }
            catch (Exception ex)
            {
                flowLayoutPanelMods.Controls.Clear();
                _logger?.Report($"ERROR loading mods: {ex.Message}");
                var errorTextBox = new TextBox
                {
                    Text = $"Failed to load mods:\r\n\r\n{ex.Message}",
                    Multiline = true,
                    ReadOnly = true,
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(37, 37, 38),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Consolas", 10F) // Use a monospaced font for readability
                };
                flowLayoutPanelMods.Controls.Add(errorTextBox);
            }
            finally
            {
                _isLoading = false;
                btnSearch.Enabled = true;
            }
        }

        private void OnModDownloadClicked(GameBananaMod mod)
        {
            // Open a new form to show mod details and download options
            if (PlatformUtils.IsLinux)
            {
                using (var modDetailsForm = new ModDetailsFormLinux(mod, _logger, _onModsChanged))
                {
                    modDetailsForm.ShowDialog(this);
                }
            }
            else
            {
                using (var modDetailsForm = new ModDetailsForm(mod, _logger, _onModsChanged))
                {
                    modDetailsForm.ShowDialog(this);
                }
            }
        }

        private void btnSearch_Click(object? sender, EventArgs e)
        {
            string searchTerm = txtSearch.Text;
            if (searchTerm == "Search mods...")
            {
                searchTerm = ""; // Don't search for the placeholder text
            }
            // Trigger a new search with the text box content.
            _ = LoadModsAsync(1, searchTerm);
        }

        private async void btnPrevPage_Click(object? sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                await LoadModsAsync(_currentPage - 1, _currentSearch);
            }
        }

        private async void btnNextPage_Click(object? sender, EventArgs e)
        {
            await LoadModsAsync(_currentPage + 1, _currentSearch);
        }
    }
}