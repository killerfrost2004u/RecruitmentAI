using RecruitmentAI.App.Models;
using System;
using System.Media;
using System.Windows;

namespace RecruitmentAI.App
{
    public partial class ManualAssessmentDialog : Window
    {
        public string EnglishLevel { get; private set; } = "";
        public string Feedback { get; private set; } = "";
        private Candidate _candidate;

        public ManualAssessmentDialog(Candidate candidate)
        {
            InitializeComponent();
            _candidate = candidate;
            txtCandidateInfo.Text = $"{candidate.FullName} | Phone: {candidate.PhoneNumber} | Current Level: {candidate.EnglishLevel}";
        }

        private void BtnPlayVoiceNote_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_candidate.VoiceNotePath) && System.IO.File.Exists(_candidate.VoiceNotePath))
            {
                try
                {
                    var player = new SoundPlayer(_candidate.VoiceNotePath);
                    player.Play();
                    MessageBox.Show("Playing voice note...", "Audio", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not play audio: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Voice note file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = cmbExpertLevel.SelectedItem as System.Windows.Controls.ComboBoxItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Please select an English level.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EnglishLevel = selectedItem.Content.ToString()!.Split(' ')[0]; // Get "A1", "B2", etc.
            Feedback = txtExpertFeedback.Text;

            if (string.IsNullOrWhiteSpace(Feedback))
            {
                Feedback = "Expert manual assessment completed.";
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}