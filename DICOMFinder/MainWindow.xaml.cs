using FellowOakDicom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace DICOMFinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string rootPath;
        private string outputPath;

        // Search conditions
        List<string> modalities;
        List<DicomTransferSyntax> xferSyntaxes;
        List<string> sopClassUids;

        BackgroundWorker? bgWorker;

        public MainWindow()
        {           
            InitializeComponent();
            rootPath = @"C:\__Dev\11.Image\";
            tbRootFolderPath.Text = rootPath;
            outputPath = @"C:\__Dev\";
            tbOutputFolderPath.Text = outputPath;

            modalities = new List<string>();
            xferSyntaxes =  new List<DicomTransferSyntax>();
            sopClassUids = new List<string>(); 

            bgWorker = null;
        }
        private void changeUIStatus(bool isEnable)
        {
            btnBrowseRootPath.IsEnabled = isEnable;
            tbModality.IsEnabled = isEnable;
            tbTransferSyntax.IsEnabled = isEnable;
            tbSopClassUid.IsEnabled = isEnable;
            btnBrowseOutputPath.IsEnabled = isEnable;
            btnSearch.IsEnabled = isEnable;
        }
        private void clickBrowseRootPath(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = tbRootFolderPath.Text;
            /* Display folder selector. */
            var result = dialog.ShowDialog();
            /* Get the selected folder path and display in a TextBox */
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                /* Display the config file path to textbox */
                rootPath = dialog.SelectedPath + "\\";
                tbRootFolderPath.Text = rootPath;
            }
        }
        private void clickBrowseOutputPath(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = tbOutputFolderPath.Text;
            /* Display folder selector. */
            var result = dialog.ShowDialog();
            /* Get the selected folder path and display in a TextBox */
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                /* Display the config file path to textbox */
                outputPath = dialog.SelectedPath + "\\";
                tbOutputFolderPath.Text = outputPath;
            }
        }
        private void clickSearchDicom(object sender, RoutedEventArgs e)
        {
            // Check search conditions.
            if (!validateSearchConditions())
            {
                System.Windows.MessageBox.Show("Failed to search. Check path and search conditions.", "Warn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            changeUIStatus(false);
            // Set backgroung worker.
            bgWorker = new BackgroundWorker();
            bgWorker.WorkerReportsProgress = true;
            bgWorker.DoWork += progressStarted;
            bgWorker.ProgressChanged += progressChanged;
            bgWorker.RunWorkerCompleted += progressCompleted;
            bgWorker.RunWorkerAsync();
        }
        private bool validateSearchConditions()
        {
            rootPath = tbRootFolderPath.Text;
            outputPath = tbOutputFolderPath.Text;
            if(string.IsNullOrEmpty(rootPath) || string.IsNullOrEmpty(outputPath))
            {
                return false;
            }
            // Fix conditions
            modalities.Clear();
            modalities = tbModality.Text.Split(';').ToList().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            xferSyntaxes.Clear();
            foreach (string xferSyntaxUid in tbTransferSyntax.Text.Split(';').ToList().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList())
            {
                DicomTransferSyntax syntax = DicomTransferSyntax.Query(DicomUID.Parse(xferSyntaxUid));
                xferSyntaxes.Add(syntax);
            }
            sopClassUids.Clear();
            sopClassUids = tbSopClassUid.Text.Split(';').ToList().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            // Validate conditions.
            if (modalities.Count == 0 && xferSyntaxes.Count == 0 && sopClassUids.Count == 0)
            {
                return false;
            }
            return true;
        }
        private void findDicomFiles(string[] files)
        {
            // Fix output file path and create output file.
            string outputFileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
            string outputFileFullePath = Path.Combine(outputPath, outputFileName);
            StreamWriter outputStreamWriter = File.CreateText(outputFileFullePath);
            // Traversal dicom files.
            int foundCount = 0, processedCount = 0;
            string tagValue = "";
            DicomFile? dicomFile = null;
            foreach (string file in files)
            {
                processedCount++;
                int progress = (processedCount * 100) / files.Count();
                if(progress > 10 ) 
                {
                    int a = 1; 
                }
                bgWorker.ReportProgress(progress);
                if (File.Exists(file))
                {
                    try
                    {
                        dicomFile = DicomFile.Open(file, FileReadOption.SkipLargeTags);
                    }
                    catch (Exception ex)
                    {
                        // Invalid DICOM file.
                        dicomFile = null;
                        continue;
                    }
                    if (dicomFile != null)
                    {
                        DicomDataset ds = dicomFile.Dataset;
                        if (modalities.Count != 0)
                        {
                            try
                            {
                                tagValue = ds.GetString(DicomTag.Modality);
                                if (!modalities.Contains(tagValue)) continue;
                            }
                            catch (DicomDataException ex)
                            {
                                tagValue = "";
                                continue;
                            }
                        }
                        if (xferSyntaxes.Count != 0)
                        {
                            try
                            {
                                if (!xferSyntaxes.Contains(dicomFile.FileMetaInfo.TransferSyntax)) continue;
                            }
                            catch
                            {
                                tagValue = "";
                                continue;
                            }
                        }
                        if (sopClassUids.Count != 0)
                        {
                            try
                            {
                                tagValue = ds.GetString(DicomTag.SOPClassUID);
                                if (!sopClassUids.Contains(tagValue)) continue;
                            }
                            catch (DicomDataException ex)
                            {
                                tagValue = "";
                                continue;
                            }
                        }
                        outputStreamWriter.WriteLine(file);
                        foundCount++;
                    }
                }
            }
            outputStreamWriter.Close();
            System.Windows.MessageBox.Show("Search finished. Found " + foundCount + " DICOM files.", "Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void progressStarted(object sender, DoWorkEventArgs e)
        {
            // Start work.
            findDicomFiles(Directory.GetFiles(rootPath, "*.dcm", SearchOption.AllDirectories));
        }
        private void progressChanged(object sender, ProgressChangedEventArgs e)
        {
            pgbSearch.Value = e.ProgressPercentage;
        }
        private void progressCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pgbSearch.Value = pgbSearch.Maximum;
            changeUIStatus(true);
        }
    }
}
