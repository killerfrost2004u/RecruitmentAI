using Microsoft.Win32;
using RecruitmentAI.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace RecruitmentAI.App
{
    public partial class MainWindow : Window
    {
        private DatabaseService _databaseService;
        private string? _selectedVoiceNotePath;

        public MainWindow()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            LoadCandidates();
        }

        private void BtnSelectVoiceNote_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Audio files (*.mp3;*.wav;*.m4a;*.aac)|*.mp3;*.wav;*.m4a;*.aac|All files (*.*)|*.*",
                Title = "Select Voice Note File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedVoiceNotePath = openFileDialog.FileName;
                txtVoiceNotePath.Text = System.IO.Path.GetFileName(_selectedVoiceNotePath);
            }
        }

        private void BtnAddCandidate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(txtFullName.Text))
                {
                    MessageBox.Show("Full name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create new candidate
                var candidate = new Candidate
                {
                    FullName = txtFullName.Text, // Single name field
                    Email = txtEmail.Text,
                    PhoneNumber = txtPhone.Text,
                    Age = int.TryParse(txtAge.Text, out int age) ? age : 0,
                    EducationStatus = (cmbEducation.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString(),
                    MilitaryStatus = (cmbMilitary.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString(),
                    RecruiterName = txtRecruiter.Text,
                    LeaderName = txtLeader.Text,
                    UnitManagerName = txtUnitManager.Text,
                    VoiceNotePath = _selectedVoiceNotePath,
                    EnglishLevel = "Pending", // Will be set by AI later
                    Status = "New"
                };

                // Save to database
                _databaseService.AddCandidate(candidate);

                MessageBox.Show("Candidate added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Clear form
                ClearForm();
                LoadCandidates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding candidate: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            txtFullName.Text = "";
            txtEmail.Text = "";
            txtPhone.Text = "";
            txtAge.Text = "";
            cmbEducation.SelectedIndex = -1;
            cmbMilitary.SelectedIndex = -1;
            txtRecruiter.Text = "";
            txtLeader.Text = "";
            txtUnitManager.Text = "";
            _selectedVoiceNotePath = "";
            txtVoiceNotePath.Text = "";
        }

        // Add this field to the class
        private EnglishAssessmentService _assessmentService = new EnglishAssessmentService();

        // Add these methods
        private void AssessCandidate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var candidateId = (int)button.Tag;

                // Find the candidate
                var candidates = _databaseService.GetAllCandidates();
                var candidate = candidates.FirstOrDefault(c => c.Id == candidateId);

                if (candidate != null && !string.IsNullOrEmpty(candidate.VoiceNotePath))
                {
                    // Show processing message
                    button.Content = "Processing...";
                    button.IsEnabled = false;

                    // Process in background to avoid UI freezing
                    Task.Run(() =>
                    {
                        // Get available job offers
                        var availableJobs = _databaseService.GetAllJobOffers();

                        // Use the NEW method for smart job matching
                        var result = _assessmentService.AssessEnglishLevelWithJobMatching(candidate.VoiceNotePath, candidate, availableJobs);

                        // Update UI on main thread
                        Dispatcher.Invoke(() =>
                        {
                            // Update candidate in database
                            var jobsText = string.Join(" | ", result.MatchedJobs);
                            _databaseService.UpdateCandidateAssessment(candidateId, result.EnglishLevel, jobsText);

                            // Show results
                            var message = $"Assessment Complete!\n\nEnglish Level: {result.EnglishLevel}\n" +
                                        $"Confidence: {result.ConfidenceScore:P0}\n\n" +
                                        $"Top Job Matches:\n";

                            foreach (var job in result.MatchedJobs)
                            {
                                message += $"- {job}\n";
                            }

                            message += $"\nFeedback: {result.Feedback}";

                            MessageBox.Show(message, "AI Assessment Result", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Reload candidates to show updated data
                            LoadCandidates();
                        });
                    });
                }
                else
                {
                    MessageBox.Show("Candidate or voice note not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Assessment failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MLAssessCandidate_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var candidateId = (int)button.Tag;
        
            var candidates = _databaseService.GetAllCandidates();
            var candidate = candidates.FirstOrDefault(c => c.Id == candidateId);
        
            if (candidate != null && !string.IsNullOrEmpty(candidate.VoiceNotePath))
            {
                button.Content = "ML Processing...";
                button.IsEnabled = false;
        
                try
                {
                    // Use ML assessment
                    var result = await _assessmentService.AssessWithMLModel(candidate.VoiceNotePath);
                    
                    // Update candidate in database
                    var jobsText = string.Join(" | ", result.MatchedJobs);
                    _databaseService.UpdateCandidateAssessment(candidateId, result.EnglishLevel, jobsText, "ML Assessed");
        
                    // Show results
                    var message = $"🤖 ML Assessment Complete!\n\nEnglish Level: {result.EnglishLevel}\n" +
                                $"Confidence: {result.ConfidenceScore:P0}\n" +
                                $"Model: Advanced AI\n\n" +
                                $"Feedback: {result.Feedback}";
        
                    MessageBox.Show(message, "Advanced ML Assessment", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    LoadCandidates();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ML assessment failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    button.Content = "ML";
                    button.IsEnabled = true;
                }
            }
            else
            {
                MessageBox.Show("Candidate or voice note not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ViewJobs_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var candidateId = (int)button.Tag;

            var candidates = _databaseService.GetAllCandidates();
            var candidate = candidates.FirstOrDefault(c => c.Id == candidateId);

            if (candidate != null && !string.IsNullOrEmpty(candidate.MatchedOffers))
            {
                MessageBox.Show($"Matched Jobs for {candidate.FullName}:\n\n{candidate.MatchedOffers}", 
                               "Job Matches", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No job matches available. Please assess English level first.", 
                               "No Data", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Information);
            }
        }
        private void ManualAssessment_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var candidateId = (int)button.Tag;

            var candidates = _databaseService.GetAllCandidates();
            var candidate = candidates.FirstOrDefault(c => c.Id == candidateId);

            if (candidate != null)
            {
                var dialog = new ManualAssessmentDialog(candidate);
                if (dialog.ShowDialog() == true)
                {
                    _assessmentService.SaveManualAssessment(
                        candidateId, 
                        dialog.EnglishLevel, 
                        dialog.Feedback
                    );

                    _databaseService.UpdateCandidateAssessment(
                        candidateId, 
                        dialog.EnglishLevel, 
                        "Expert Assessed", 
                        "Expert Reviewed"
                    );

                    LoadCandidates();
                    MessageBox.Show("Expert assessment saved for training!", "Success");
                }
            }
        }

        private void LoadCandidates()
        {
            try
            {
                var candidates = _databaseService.GetAllCandidates();
                dgCandidates.ItemsSource = candidates;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading candidates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}