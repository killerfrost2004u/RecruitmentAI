using Microsoft.Win32;
using RecruitmentAI.App.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;

namespace RecruitmentAI.App
{
    public partial class MainWindow : Window
    {
        private DatabaseService _databaseService;
        private string _selectedVoiceNotePath;

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
                if (string.IsNullOrWhiteSpace(txtFirstName.Text))
                {
                    MessageBox.Show("First name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create new candidate
                var candidate = new Candidate
                {
                    FirstName = txtFirstName.Text,
                    MiddleName = txtMiddleName.Text,
                    FatherName = txtFatherName.Text,
                    FamilyName = txtFamilyName.Text,
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
            txtFirstName.Text = "";
            txtMiddleName.Text = "";
            txtFatherName.Text = "";
            txtFamilyName.Text = "";
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