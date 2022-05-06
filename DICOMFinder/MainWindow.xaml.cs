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
        BackgroundWorker? bgWorker;

        public MainWindow()
        {           
            InitializeComponent();
            rootPath = @"C:\__Dev\11.Image\";
            tbRootFolderPath.Text = rootPath;
            outputPath = @"C:\__Dev\";
            tbOutputFolderPath.Text = outputPath;
            bgWorker = null;
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
            return;
            // 1. Fix conditions
            List<string> modalities = tbModality.Text.Split(';').ToList().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            List<DicomTransferSyntax> xferSyntaxes = new List<DicomTransferSyntax>();
            foreach (string xferSyntaxUid in tbTransferSyntax.Text.Split(';').ToList().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList())
            {
                DicomTransferSyntax syntax = DicomTransferSyntax.Query(DicomUID.Parse(xferSyntaxUid));
                xferSyntaxes.Add(syntax);
            }
            List<string> sopClassUids = tbSopClassUid.Text.Split(';').ToList().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            if (modalities.Count == 0 && xferSyntaxes.Count == 0 && sopClassUids.Count == 0)
            {
                System.Windows.MessageBox.Show("Failed to search. No search conditions.", "Warn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Get all dicom file path.
            rootPath = tbRootFolderPath.Text;
            string[] files = Directory.GetFiles(rootPath, "*.dcm", SearchOption.AllDirectories);

            // 3. Fix output file path and create output file.
            string outputFileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
            string outputFileFullePath = Path.Combine(outputPath, outputFileName);
            StreamWriter outputStreamWriter = File.CreateText(outputFileFullePath);

            //4. Set backgroung worker.
            //bgWorker = new BackgroundWorker();
            //bgWorker.WorkerReportsProgress = true;
            //bgWorker.DoWork += _worker_DoWork;
            //bgWorker.ProgressChanged += _worker_ProgressChanged;
            //bgWorker.RunWorkerCompleted += _worker_RunWorkerCompleted;
            //bgWorker.RunWorkerAsync();

            // 5. Traversal dicom files.
            int foundCount = 0, processedCount = 0;
            string tagValue = "";
            DicomFile? dicomFile = null;
            foreach (string file in files)
            {
                processedCount++;
                if (File.Exists(file))
                {
                    try
                    {
                        dicomFile = DicomFile.Open(file, FileReadOption.SkipLargeTags);
                    }
                    catch(Exception ex)
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
                            if (!xferSyntaxes.Contains(dicomFile.FileMetaInfo.TransferSyntax)) continue;
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
    }
}
