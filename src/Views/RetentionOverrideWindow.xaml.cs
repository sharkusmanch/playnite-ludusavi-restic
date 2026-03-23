using System.Windows;

namespace LudusaviRestic
{
    public partial class RetentionOverrideWindow : Window
    {
        public int? KeepLast { get; private set; }
        public int? KeepDaily { get; private set; }
        public int? KeepWeekly { get; private set; }
        public int? KeepMonthly { get; private set; }
        public int? KeepYearly { get; private set; }
        public bool Removed { get; private set; }

        public RetentionOverrideWindow(string gameName, LudusaviResticSettings settings, GameOverride existing)
        {
            InitializeComponent();

            GameNameText.Text = gameName;

            // Show global defaults as hint text
            string defaultFmt = "(default: {0})";
            KeepLastDefault.Text = string.Format(defaultFmt, settings.KeepLast);
            KeepDailyDefault.Text = string.Format(defaultFmt, settings.KeepDaily);
            KeepWeeklyDefault.Text = string.Format(defaultFmt, settings.KeepWeekly);
            KeepMonthlyDefault.Text = string.Format(defaultFmt, settings.KeepMonthly);
            KeepYearlyDefault.Text = string.Format(defaultFmt, settings.KeepYearly);

            // Pre-fill with existing override values
            if (existing != null)
            {
                KeepLastBox.Text = existing.KeepLast.HasValue ? existing.KeepLast.Value.ToString() : "";
                KeepDailyBox.Text = existing.KeepDaily.HasValue ? existing.KeepDaily.Value.ToString() : "";
                KeepWeeklyBox.Text = existing.KeepWeekly.HasValue ? existing.KeepWeekly.Value.ToString() : "";
                KeepMonthlyBox.Text = existing.KeepMonthly.HasValue ? existing.KeepMonthly.Value.ToString() : "";
                KeepYearlyBox.Text = existing.KeepYearly.HasValue ? existing.KeepYearly.Value.ToString() : "";
            }

            // Only show Remove button if there's an existing override with retention
            RemoveButton.Visibility = (existing != null && existing.HasRetentionOverride)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private int? ParseField(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            int val;
            if (int.TryParse(text.Trim(), out val) && val >= 0)
                return val;
            return null;
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            // Validate: non-empty fields must be valid non-negative integers
            if (!ValidateField(KeepLastBox.Text) || !ValidateField(KeepDailyBox.Text) ||
                !ValidateField(KeepWeeklyBox.Text) || !ValidateField(KeepMonthlyBox.Text) ||
                !ValidateField(KeepYearlyBox.Text))
            {
                MessageBox.Show(
                    "Values must be blank (use global default) or a non-negative integer.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            KeepLast = ParseField(KeepLastBox.Text);
            KeepDaily = ParseField(KeepDailyBox.Text);
            KeepWeekly = ParseField(KeepWeeklyBox.Text);
            KeepMonthly = ParseField(KeepMonthlyBox.Text);
            KeepYearly = ParseField(KeepYearlyBox.Text);
            Removed = false;
            DialogResult = true;
        }

        private void OnRemove(object sender, RoutedEventArgs e)
        {
            Removed = true;
            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private bool ValidateField(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;
            int val;
            return int.TryParse(text.Trim(), out val) && val >= 0;
        }
    }
}
