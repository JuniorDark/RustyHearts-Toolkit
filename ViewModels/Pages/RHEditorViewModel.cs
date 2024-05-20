using Microsoft.Win32;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.RH;
using RHToolkit.Properties;
using RHToolkit.Services;
using System.Data;

namespace RHToolkit.ViewModels.Pages
{
    public partial class RHEditorViewModel(IDatabaseService databaseService) : ObservableObject
    {
        private readonly IDatabaseService _databaseService = databaseService;

        #region Read

        [ObservableProperty]
        private DataTable? _rhData;

        [RelayCommand]
        private async Task LoadFile()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Rusty Hearts Table Files (*.rh)|*.rh|All Files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string file = openFileDialog.FileName;

                    RhData = ProcessFile(file);

                }
                catch (Exception ex)
                {
                    RHMessageBox.ShowOKMessage($"{Resources.LoadTemplateError}: {ex.Message}", Resources.LoadTemplateError);
                }
                
            }
        }

        private static DataTable? ProcessFile(string sourceFile)
        {
            try
            {

                using FileStream sourceFileStream = File.OpenRead(sourceFile);
                byte[] buffer = new byte[4096];
                int bytesRead;

                using MemoryStream memoryStream = new();
                while ((bytesRead = sourceFileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }

                byte[] sourceBytes = memoryStream.ToArray();

               return DataTableCryptor.RhToDataTable(sourceBytes);

            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Properties
        [ObservableProperty]
        private string? _searchText;

        [ObservableProperty]
        private bool _isRestoreCharacterButtonEnabled = false;

        [ObservableProperty]
        private bool _isFirstTimeInitialized = true;

        #endregion
    }
}
