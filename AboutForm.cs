using System.Windows.Forms;

namespace CrossworldsModManager
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
        }
    }
}