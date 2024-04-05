using RHGMTool.Models;
using RHGMTool.ViewModels;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace RHGMTool.Views
{
    public partial class ItemWindow : Window
    {
        #region Class Variables
        private readonly int activeItemIndex;
        private readonly MailTemplateData? templateData = new();
        private readonly bool isDatabase;
        private readonly ToolTip pictureBoxToolTip = new();
        #endregion

        private readonly FrameViewModel _viewModel;
        private readonly DataTable? itemDataTable;

        public ItemWindow()
        {
            InitializeComponent();
            _viewModel = new FrameViewModel();
            DataContext = _viewModel;
            itemDataTable = ItemDataTable.CachedItemDataTable;
            //dataGridView.ItemsSource = itemDataTable?.DefaultView;

            UpdateItemFrameValues();
        }

        private void UpdateItemFrameValues()
        {
            ItemData item = new()
            {
                Name = "Anelas The 'Other'",
                Description = "Test",
                Type = "Weapon",
                WeaponID00 = 6162051,
                Category = 5,
                SubCategory = 1,
                Weight = 2000,
                LevelLimit = 50,
                ItemTrade = 0,
                Branch = 6,
                SellPrice = 4000095,
                PetFood = 1,
                JobClass = 1,
                OptionCountMax = 3,
                SocketCountMax = 3,
                FixOption00 = 1707,
                FixOptionValue00 = 100,
                FixOption01 = 1707,
                FixOptionValue01 = 80,
                ReconstructionMax = 2,
                Durability = 10000,
                OverlapCnt = 999,
                SetId = 1002


            };

            var gearFrameViewModel = _viewModel;
            gearFrameViewModel.Gear = item;
            gearFrameViewModel.EnchantLevel = 1;
            gearFrameViewModel.RankValue = 5;
            gearFrameViewModel.RandomOption01 = 1707;
            gearFrameViewModel.RandomOption01Value = 10;
            gearFrameViewModel.RandomOption02 = 1707;
            gearFrameViewModel.RandomOption02Value = 20;
            gearFrameViewModel.RandomOption03 = 1707;
            gearFrameViewModel.RandomOption03Value = 30;
            gearFrameViewModel.SocketCount = 3;
            gearFrameViewModel.Socket01Color = 4;
            gearFrameViewModel.Socket02Color = 3;
            gearFrameViewModel.Socket03Color = 2;
            gearFrameViewModel.SocketOption01 = 1707;
            gearFrameViewModel.SocketOption01Value = 50;
            gearFrameViewModel.SocketOption02 = 1707;
            gearFrameViewModel.SocketOption02Value = 500;
            gearFrameViewModel.SocketOption03 = 1707;
            gearFrameViewModel.SocketOption03Value = 500;

        }

        private void ComboBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                if (!(bool)e.NewValue)
                {
                    if (comboBox.Items.Count > 0)
                    {
                        comboBox.SelectedIndex = 0;
                    }
                }

            }
        }


        private void dataGridView_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Hide all columns except for nID, wszDesc, and szIconName
            if (e.PropertyName != "nID" && e.PropertyName != "wszDesc" && e.PropertyName != "szIconName")
            {
                e.Cancel = true;
            }
            else
            {
                // Rename columns
                if (e.PropertyName == "nID")
                    e.Column.Header = "ID";
                else if (e.PropertyName == "wszDesc")
                    e.Column.Header = "Description";
                else if (e.PropertyName == "szIconName")
                    e.Column.Header = "Icon Name";
            }
        }




        //        #region Template Configuration
        //        public void SetTemplateData(MailTemplateData data, int itemIndex)
        //        {
        //            templateData = data;
        //            activeItemIndex = itemIndex;
        //            LoadTemplateData();
        //            SetIconSlot();
        //        }
        //        bool isLoadingTemplate = false;
        //        private void LoadTemplateData()
        //        {
        //            try
        //            {
        //                if (templateData == null)
        //                {
        //                    System.Windows.MessageBox.Show("Error loading template data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                    return;
        //                }

        //                // No equivalent of InvokeRequired in WPF, since it's single-threaded by design

        //                isLoadingTemplate = true;

        //                int templateItemType = templateData?.ItemTypes?[activeItemIndex] ?? 0;
        //                int templateItemID = templateData?.ItemIDs?[activeItemIndex] ?? 0;

        //                cmbItemType.SelectedValue = templateItemType;

        //                var selectedRow = GetSelectedRowByItemID(dataGridView, templateItemID);

        //                if (selectedRow != null)
        //                {
        //                    selectedRow.IsSelected = true;
        //                    dataGridView.ScrollIntoView(selectedRow);
        //                }

        //                numAmount.Value = templateData?.ItemAmounts?[activeItemIndex] ?? 0;
        //                numDurability.Value = templateData?.Durabilities?[activeItemIndex] ?? 0;
        //                numMaxDurability.Value = templateData?.DurabilityMaxValues?[activeItemIndex] ?? 0;
        //                numEnchantLevel.Value = templateData?.EnchantLevels?[activeItemIndex] ?? 0;
        //                numRank.Value = templateData?.Ranks?[activeItemIndex] ?? 0;
        //                numReconstructionMax.Value = templateData?.ReconNums?[activeItemIndex] ?? 0;
        //                tbReconNum.Text = (templateData?.ReconStates?[activeItemIndex] ?? 0).ToString();
        //                numWeight.Value = templateData?.WeightValues?[activeItemIndex] ?? 0;

        //                cbOptionCode1.SelectedValue = templateData?.OptionCodes1?[activeItemIndex] ?? 0;
        //                cbOptionCode2.SelectedValue = templateData?.OptionCodes2?[activeItemIndex] ?? 0;
        //                cbOptionCode3.SelectedValue = templateData?.OptionCodes3?[activeItemIndex] ?? 0;
        //                SetOptionValues(numOptionValue1, templateData?.OptionCodes1?[activeItemIndex], templateData?.OptionValues1?[activeItemIndex]);
        //                SetOptionValues(numOptionValue2, templateData?.OptionCodes2?[activeItemIndex], templateData?.OptionValues2?[activeItemIndex]);
        //                SetOptionValues(numOptionValue3, templateData?.OptionCodes3?[activeItemIndex], templateData?.OptionValues3?[activeItemIndex]);

        //                cbSocketColor1.SelectedValue = templateData?.SocketColors1?[activeItemIndex] ?? 0;
        //                cbSocketColor2.SelectedValue = templateData?.SocketColors2?[activeItemIndex] ?? 0;
        //                cbSocketColor3.SelectedValue = templateData?.SocketColors3?[activeItemIndex] ?? 0;

        //                cbSocketCode1.SelectedValue = templateData?.SocketCodes1?[activeItemIndex] ?? 0;
        //                cbSocketCode2.SelectedValue = templateData?.SocketCodes2?[activeItemIndex] ?? 0;
        //                cbSocketCode3.SelectedValue = templateData?.SocketCodes3?[activeItemIndex] ?? 0;
        //                SetOptionValues(numSocketValue1, templateData?.SocketCodes1?[activeItemIndex], templateData?.SocketValues1?[activeItemIndex]);
        //                SetOptionValues(numSocketValue2, templateData?.SocketCodes2?[activeItemIndex], templateData?.SocketValues2?[activeItemIndex]);
        //                SetOptionValues(numSocketValue3, templateData?.SocketCodes3?[activeItemIndex], templateData?.SocketValues3?[activeItemIndex]);

        //                isLoadingTemplate = false;
        //            }
        //            catch (Exception ex)
        //            {
        //                System.Windows.MessageBox.Show($"Error loading template data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }

        //        private DataGridRow? GetSelectedRowByItemID(DataGrid dataGrid, int? itemID)
        //        {
        //            return dataGrid.ItemContainerGenerator.ContainerFromIndex(itemID ?? 0) as DataGridRow;
        //        }

        //        private void SetOptionValues(IntegerUpDown numericUpDown, int? optionCode, int? optionValue)
        //        {
        //            (int maxValue, int minValue) = SQLiteDatabaseReader.GetOptionValue(optionCode);
        //            numericUpDown.Maximum = maxValue;
        //            numericUpDown.Minimum = minValue;
        //            numericUpDown.Value = optionValue ?? 0;
        //        }
        //        #endregion


        //        #region Datagrid Configuration

        //        private void FilterDataGridView()
        //        {
        //            try
        //            {
        //                // Get selected values from cmbItemCategory, cmbSubCategory, cmbItemBranch, and cmbJobClass
        //                int selecteditemType = (cmbItemType.SelectedItem as NameID)?.ID ?? 0;
        //                int selectedCategoryID = (cmbItemCategory.SelectedItem as NameID)?.ID ?? 0;
        //                int selectedSubCategoryID = (cmbSubCategory.SelectedItem as NameID)?.ID ?? 0;
        //                int selectedBranchIndex = (cmbItemBranch.SelectedItem as NameID)?.ID ?? 0;
        //                int selectedJobClass = (cmbJobClass.SelectedItem as NameID)?.ID ?? 0;

        //                // Map selectedBranchIndex to nBranch values
        //                IEnumerable<int> nBranchValues = MapBranchIndexToValues(selectedBranchIndex);

        //                // Use a DataView to filter the DataTable based on the selected category, subcategory, branch, and job class
        //                DataView dataView = new(itemDataTable);

        //                StringBuilder filterExpressionBuilder = new();
        //                filterExpressionBuilder.Append($"(nCategory = {selectedCategoryID} OR {selectedCategoryID} = 0) ");
        //                filterExpressionBuilder.Append($"AND (nSubCategory = {selectedSubCategoryID} OR {selectedSubCategoryID} = 0) ");

        //                // Conditionally include nBranch filter
        //                if (selectedBranchIndex != 0)
        //                {
        //                    string nBranchFilter = $"(nBranch IN ({string.Join(",", nBranchValues)}))";
        //                    filterExpressionBuilder.Append($"AND {nBranchFilter} ");
        //                }

        //                // Conditionally include nJobClass filter
        //                if (selectedJobClass != 0)
        //                {
        //                    filterExpressionBuilder.Append($"AND (nJobClass = {selectedJobClass}) ");
        //                }

        //                filterExpressionBuilder.Append($"AND (ItemType = {selecteditemType} OR '{selecteditemType}' = 0)");

        //                dataView.RowFilter = filterExpressionBuilder.ToString();
        //                DataTable filteredDataTable = dataView.ToTable();

        //                // Update the DataGridView with the filtered DataTable
        //                UpdateDataGrid(filteredDataTable, dataGridView);
        //                ConfigureDataGrid(dataGridView);
        //            }
        //            catch (Exception ex)
        //            {
        //                System.Windows.MessageBox.Show($"An error occurred in category filter: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }

        //        private void CmbFilterDataGridView_SelectedIndexChanged(object sender, EventArgs e)
        //        {
        //            FilterDataGridView();
        //        }

        //        private void CmbItemType_SelectedIndexChanged(object sender, EventArgs e)
        //        {
        //            // get the selected item type
        //            int selectedItemType = (cmbItemType.SelectedItem as NameID)?.ID ?? 0;

        //            // Populate cmbItemCategory with categories
        //            SQLiteDatabaseReader.PopulateCategoryComboBox(cmbItemCategory, (ItemType)selectedItemType, isSubCategory: false);

        //            // Populate cmbSubCategory with subcategories
        //            SQLiteDatabaseReader.PopulateCategoryComboBox(cmbSubCategory, (ItemType)selectedItemType, isSubCategory: true);
        //        }

        //        private static void UpdateDataGrid(DataTable itemDataTable, DataGrid dataGrid)
        //        {
        //            if (!dataGrid.Dispatcher.CheckAccess())
        //            {
        //                dataGrid.Dispatcher.Invoke(() => UpdateDataGrid(itemDataTable, dataGrid));
        //                return;
        //            }

        //            dataGrid.Columns.Clear();

        //            if (dataGrid.ItemsSource == null || dataGrid.ItemsSource != itemDataTable.DefaultView)
        //            {
        //                if (dataGrid.Columns.Count == 0)
        //                {
        //                    dataGrid.ItemsSource = itemDataTable.DefaultView;

        //                    DataGridTextColumn idColumn = new()
        //                    {
        //                        Header = "ID",
        //                        Binding = new Binding("nID")
        //                    };
        //                    dataGrid.Columns.Add(idColumn);

        //                    DataGridTextColumn nameColumn = new()
        //                    {
        //                        Header = "Name",
        //                        Binding = new Binding("wszDesc")
        //                    };
        //                    dataGrid.Columns.Add(nameColumn);

        //                    DataGridTemplateColumn iconColumn = new()
        //                    {
        //                        Header = "Icon"
        //                    };
        //                    FrameworkElementFactory factory = new(typeof(Image));
        //                    factory.SetBinding(Image.SourceProperty, new Binding("szIconName"));
        //                    factory.SetValue(Image.StretchProperty, Stretch.Uniform);
        //                    DataTemplate cellTemplate = new() { VisualTree = factory };
        //                    iconColumn.CellTemplate = cellTemplate;
        //                    dataGrid.Columns.Add(iconColumn);
        //                }

        //                if (dataGrid.Items.Count > 0)
        //                {
        //                    lbTotal.Content = $"Showing: {dataGrid.Items.Count} items";
        //                    dataGrid.SelectedItem = dataGrid.Items[0];
        //                }
        //            }
        //        }

        //        private void ConfigureDataGrid(DataGrid dataGrid)
        //        {
        //            if (!dataGrid.Dispatcher.CheckAccess())
        //            {
        //                dataGrid.Dispatcher.Invoke(() => ConfigureDataGrid(dataGrid));
        //                return;
        //            }

        //            dataGrid.AutoGenerateColumns = false;
        //            dataGrid.SelectionMode = DataGridSelectionMode.Single;
        //            dataGrid.CanUserAddRows = false;
        //            dataGrid.CanUserDeleteRows = false;
        //            dataGrid.CanUserResizeRows = false;
        //            dataGrid.RowHeight = 36;

        //            DataGridTextColumn idColumn = new()
        //            {
        //                Header = "ID",
        //                Binding = new Binding("nID")
        //            };
        //            dataGrid.Columns.Add(idColumn);

        //            DataGridTextColumn nameColumn = new()
        //            {
        //                Header = "Name",
        //                Binding = new Binding("wszDesc"),
        //                Width = new DataGridLength(1, DataGridLengthUnitType.Star) // Auto sizing to fill remaining space
        //            };
        //            dataGrid.Columns.Add(nameColumn);

        //            DataGridTemplateColumn iconColumn = new()
        //            {
        //                Header = "Icon",
        //                Width = 36
        //            };
        //            FrameworkElementFactory factory = new(typeof(Image));
        //            factory.SetBinding(Image.SourceProperty, new Binding("szIconName"));
        //            factory.SetValue(Image.StretchProperty, Stretch.Uniform);
        //            DataTemplate cellTemplate = new() { VisualTree = factory };
        //            iconColumn.CellTemplate = cellTemplate;
        //            dataGrid.Columns.Add(iconColumn);

        //            dataGrid.Loaded += (sender, e) =>
        //            {
        //                if (VisualTreeHelper.GetChildrenCount(dataGrid) > 0)
        //                {
        //                    if (VisualTreeHelper.GetChild(dataGrid, 0) is Decorator border)
        //                    {
        //                        if (border.Child is ScrollViewer scroll)
        //                        {
        //                            scroll.Background = new SolidColorBrush(Colors.Red);
        //                        }
        //                    }
        //                }
        //            };


        //            SetColumnVisibility("ItemType", false);
        //            SetColumnVisibility("nID", false);
        //            SetColumnVisibility("wszDesc", false);
        //            SetColumnVisibility("nWeaponID00", false);
        //            SetColumnVisibility("nSocketCountMin", false);
        //            SetColumnVisibility("nSocketCountMax", false);
        //            SetColumnVisibility("nJobClass", false);
        //            SetColumnVisibility("nDefense", false);
        //            SetColumnVisibility("nMagicDefense", false);
        //            SetColumnVisibility("nOptionCountMin", false);
        //            SetColumnVisibility("nOptionCountMax", false);
        //            SetColumnVisibility("nSetId", false);
        //            SetColumnVisibility("nFixOption00", false);
        //            SetColumnVisibility("nFixOptionValue00", false);
        //            SetColumnVisibility("nFixOption01", false);
        //            SetColumnVisibility("nFixOptionValue01", false);
        //            SetColumnVisibility("nSocketCountMin", false);
        //            SetColumnVisibility("wszItemDescription", false);
        //            SetColumnVisibility("nBranch", false);
        //            SetColumnVisibility("nSocketCountMax", false);
        //            SetColumnVisibility("nReconstructionMax", false);
        //            SetColumnVisibility("nLevelLimit", false);
        //            SetColumnVisibility("nItemTrade", false);
        //            SetColumnVisibility("nOverlapCnt", false);
        //            SetColumnVisibility("nDurability", false);
        //            SetColumnVisibility("nWeight", false);
        //            SetColumnVisibility("nSellPrice", false);
        //            SetColumnVisibility("szIconName", false);
        //            SetColumnVisibility("nPetEatGroup", false);
        //            SetColumnVisibility("nCategory", false);
        //            SetColumnVisibility("nSubCategory", false);
        //        }

        //        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        //        {
        //            try
        //            {
        //                if (e.PropertyName == "Icon")
        //                {
        //                    DataGridTemplateColumn iconColumn = new()
        //                    {
        //                        Header = "Icon",
        //                        Width = 36
        //                    };
        //                    FrameworkElementFactory factory = new(typeof(Image));
        //                    factory.SetBinding(Image.SourceProperty, new Binding("szIconName"));
        //                    factory.SetValue(Image.StretchProperty, Stretch.Uniform);
        //                    DataTemplate cellTemplate = new() { VisualTree = factory };
        //                    iconColumn.CellTemplate = cellTemplate;
        //                    e.Column = iconColumn;
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                System.Windows.MessageBox.Show($"Error formatting DataGrid: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }

        //        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //        {
        //            try
        //            {
        //                if (dataGridView.SelectedItems.Count > 0)
        //                {
        //                    DataRowView selectedRow = (DataRowView)dataGridView.SelectedItem;

        //                    int rowSelectedItemType = (int)selectedRow["ItemType"];
        //                    SetControlValues(selectedRow, (ItemType)rowSelectedItemType);
        //                    UpdateControls((ItemType)rowSelectedItemType);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                System.Windows.MessageBox.Show($"Error in DataGrid SelectionChanged: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }

        //        private void SetColumnVisibility(string columnName, bool isVisible)
        //        {
        //            var column = dataGridView.Columns.FirstOrDefault(col => col.Header.ToString() == columnName);
        //            if (column != null)
        //            {
        //                column.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        //            }
        //        }


        //        public int itemID;

        //        private void SetControlValues(DataRowView row, ItemType selectedItemType)
        //        {
        //            try
        //            {
        //                if (!Dispatcher.CheckAccess())
        //                {
        //                    Dispatcher.Invoke(() => SetControlValues(row, selectedItemType));
        //                    return;
        //                }

        //                if (selectedItemType != ItemType.Item)
        //                {
        //                    OpenGearFrame();
        //                    UpdateGearFrameValues(row);
        //                }
        //                else
        //                {
        //                    OpenItemFrame();
        //                    UpdateItemFrameValues(row);
        //                }

        //                int id = row["nID"] != DBNull.Value ? Convert.ToInt32(row["nID"]) : 0;
        //                string? name = row["wszDesc"] != DBNull.Value ? row["wszDesc"].ToString() : "";
        //                string? iconName = row["szIconName"] != DBNull.Value ? row["szIconName"].ToString() : "";
        //                int socketCountMax = row["nSocketCountMax"] != DBNull.Value ? Convert.ToInt32(row["nSocketCountMax"]) : 0;
        //                int reconstructionMax = row["nReconstructionMax"] != DBNull.Value ? Convert.ToInt32(row["nReconstructionMax"]) : 0;
        //                int durability = row["nDurability"] != DBNull.Value ? Convert.ToInt32(row["nDurability"]) : 0;
        //                int weight = row["nWeight"] != DBNull.Value ? Convert.ToInt32(row["nWeight"]) : 0;
        //                int maxItemStack = row["nOverlapCnt"] != DBNull.Value ? Convert.ToInt32(row["nOverlapCnt"]) : 0;
        //                int optionCountMax = row["nOptionCountMax"] != DBNull.Value ? Convert.ToInt32(row["nOptionCountMax"]) : 0;
        //                int category = row["nCategory"] != DBNull.Value ? Convert.ToInt32(row["nCategory"]) : 0;
        //                int branch = row["nBranch"] != DBNull.Value ? Convert.ToInt32(row["nBranch"]) : 0;

        //                itemID = id;

        //                numDurability.IsEnabled = durability > 0;
        //                numMaxDurability.IsEnabled = durability > 0;
        //                numDurability.Minimum = 0;
        //                numDurability.Maximum = 100000;
        //                numMaxDurability.Minimum = 0;
        //                numMaxDurability.Maximum = 100000;
        //                numDurability.Value = Math.Min(durability, (int)numDurability.Maximum);
        //                numMaxDurability.Value = Math.Min(durability, (int)numMaxDurability.Maximum);
        //                numReconstructionMax.Maximum = reconstructionMax;
        //                numReconstructionMax.IsEnabled = reconstructionMax > 0;
        //                numReconstructionMax.Value = Math.Min(reconstructionMax, (int)numReconstructionMax.Maximum);
        //                tbReconNum.Text = reconstructionMax.ToString();
        //                numSocketCount.Minimum = 0;
        //                numSocketCount.Maximum = socketCountMax;
        //                tbSocketCount.Text = socketCountMax.ToString();
        //                //gbSocket.IsEnabled = socketCountMax > 0;
        //                numWeight.Value = weight;
        //                numAmount.Minimum = 1;
        //                numAmount.Maximum = maxItemStack == 0 ? 1 : maxItemStack;
        //                numAmount.Value = 1;

        //                //gbGearStats.Visibility = selectedItemType == ItemType.Weapon || selectedItemType == ItemType.Armor ? Visibility.Visible : Visibility.Collapsed;
        //                //gbRandom.Visibility = (selectedItemType != ItemType.Item || (selectedItemType == ItemType.Item && category == 29)) && optionCountMax > 0 ? Visibility.Visible : Visibility.Collapsed;
        //                //gbSocket.Visibility = (selectedItemType == ItemType.Weapon || selectedItemType == ItemType.Armor) && socketCountMax > 0 ? Visibility.Visible : Visibility.Collapsed;

        //                if (!isLoadingTemplate)
        //                {
        //                    numRank.IsEnabled = category != 17;
        //                    numEnchantLevel.IsEnabled = category != 17;
        //                    numSocketCount.Value = socketCountMax;
        //                    //gbRandom.IsEnabled = category == 29 || selectedItemType != ItemType.Item && optionCountMax > 0;
        //                    cbOptionCode1.IsEnabled = category == 29 || selectedItemType != ItemType.Item && optionCountMax > 0;
        //                    cbOptionCode2.IsEnabled = selectedItemType != ItemType.Item && optionCountMax > 1;
        //                    cbOptionCode3.IsEnabled = selectedItemType != ItemType.Item && optionCountMax > 2;
        //                    numOptionValue1.IsEnabled = category == 29 || selectedItemType != ItemType.Item && optionCountMax > 0;
        //                    numOptionValue2.IsEnabled = selectedItemType != ItemType.Item && optionCountMax > 1;
        //                    numOptionValue3.IsEnabled = selectedItemType != ItemType.Item && optionCountMax > 2;
        //                    cbSocketColor1.IsEnabled = socketCountMax > 0;
        //                    cbSocketColor2.IsEnabled = socketCountMax > 1;
        //                    cbSocketColor3.IsEnabled = socketCountMax > 2;
        //                    cbSocketCode1.IsEnabled = socketCountMax > 0;
        //                    cbSocketCode2.IsEnabled = socketCountMax > 1;
        //                    cbSocketCode3.IsEnabled = socketCountMax > 2;
        //                    numSocketValue1.IsEnabled = socketCountMax > 0;
        //                    numSocketValue2.IsEnabled = socketCountMax > 1;
        //                    numSocketValue3.IsEnabled = socketCountMax > 2;
        //                }

        //                OptionComboBox_SelectionChanged(cbOptionCode1, EventArgs.Empty);
        //                OptionComboBox_SelectionChanged(cbOptionCode2, EventArgs.Empty);
        //                OptionComboBox_SelectionChanged(cbOptionCode3, EventArgs.Empty);

        //                OptionComboBox_SelectionChanged(cbSocketCode1, EventArgs.Empty);
        //                OptionComboBox_SelectionChanged(cbSocketCode2, EventArgs.Empty);
        //                OptionComboBox_SelectionChanged(cbSocketCode3, EventArgs.Empty);

        //                lbSelectedItem.Content = name;
        //                //lbSelectedItem.Foreground = FrameData.GetItemNameColor(branch);
        //                pbItemIcon.Source = LoadIconImage(iconName);
        //                pictureBoxToolTip.Content = name;
        //            }
        //            catch (Exception ex)
        //            {
        //                System.Windows.MessageBox.Show($"Error updating control values: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }


        //        private void UpdateControls(ItemType itemType)
        //        {
        //            if (!Dispatcher.CheckAccess())
        //            {
        //                Dispatcher.Invoke(() => UpdateControls(itemType));
        //                return;
        //            }

        //            switch (itemType)
        //            {
        //                case ItemType.All:
        //                case ItemType.Item:
        //                    //gbRandom.Location = new Point(671, 36);
        //                    numEnchantLevel.Value = 0;
        //                    numAmount.Value = 1;
        //                    break;
        //                case ItemType.Costume:
        //                    //gbRandom.Location = new Point(671, 36);
        //                    numEnchantLevel.Value = 0;
        //                    numAmount.Value = 1;
        //                    break;
        //                case ItemType.Armor:
        //                case ItemType.Weapon:
        //                    //if (!gbRandom.Visible)
        //                    //{
        //                    //    gbSocket.Location = new Point(671, 157);
        //                    //}
        //                    //else
        //                    //{
        //                    //    gbSocket.Location = new Point(669, 275);
        //                    //    gbRandom.Location = new Point(671, 157);
        //                    //}
        //                    numAmount.Value = 1;
        //                    break;
        //                default:
        //                    numEnchantLevel.Value = 0;
        //                    numAmount.Value = 1;
        //                    break;
        //            }
        //        }

        //        private void NumSocketCount_ValueChanged(object sender, EventArgs e)
        //        {
        //            int socketCount = (int)numSocketCount.Value;

        //            if (gearFrame != null)
        //            {
        //                gearFrame.UpdateSocketCountLabel(socketCount);
        //            }

        //            if (!isLoadingTemplate)
        //            {
        //                cbSocketColor1.IsEnabled = socketCount > 0;
        //                cbSocketColor2.IsEnabled = socketCount > 1;
        //                cbSocketColor3.IsEnabled = socketCount > 2;
        //                cbSocketCode1.IsEnabled = socketCount > 0;
        //                cbSocketCode2.IsEnabled = socketCount > 1;
        //                cbSocketCode3.IsEnabled = socketCount > 2;
        //                numSocketValue1.IsEnabled = socketCount > 0;
        //                numSocketValue2.IsEnabled = socketCount > 1;
        //                numSocketValue3.IsEnabled = socketCount > 2;
        //            }

        //        }

        //        private void ComboBox_EnabledChanged(object sender, EventArgs e)
        //        {
        //            ComboBox comboBox = (ComboBox)sender;

        //            if (!comboBox.IsEnabled && comboBox.Items.Count > 0)
        //            {
        //                comboBox.SelectedIndex = 0;
        //            }
        //        }

        //        private void NumericUpDown_EnabledChanged(object sender, EventArgs e)
        //        {
        //            IntegerUpDown num = (IntegerUpDown)sender;

        //            if (!num.IsEnabled)
        //            {
        //                num.Value = 0;
        //            }
        //        }

        //        private ItemFrame? itemFrame = null;

        //        private void OpenItemFrame()
        //        {
        //            try
        //            {
        //                if (itemFrame == null)
        //                {
        //                    itemFrame = new ItemFrame
        //                    {
        //                        Location = new Point(350, 35)
        //                    };

        //                    itemFrame.Closed += (sender, e) => itemFrame = null;
        //                }

        //                gearFrame?.Hide();

        //                if (!itemFrame.IsVisible)
        //                {
        //                    itemFrame.Show();
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                System.Windows.MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }

        //        private static ItemData ExtractItemFromRow(DataRowView row)
        //        {
        //            ItemData item = new()
        //            {
        //                Name = row["wszDesc"] as string ?? string.Empty,
        //                Description = row["wszItemDescription"] as string ?? string.Empty,
        //                Category = row["nCategory"] != DBNull.Value ? Convert.ToInt32(row["nCategory"]) : 0,
        //                SubCategory = row["nSubCategory"] != DBNull.Value ? Convert.ToInt32(row["nSubCategory"]) : 0,
        //                Weight = row["nWeight"] != DBNull.Value ? Convert.ToInt32(row["nWeight"]) : 0,
        //                LevelLimit = row["nLevelLimit"] != DBNull.Value ? Convert.ToInt32(row["nLevelLimit"]) : 0,
        //                ItemTrade = row["nItemTrade"] != DBNull.Value ? Convert.ToInt32(row["nItemTrade"]) : 0,
        //                Branch = row["nBranch"] != DBNull.Value ? Convert.ToInt32(row["nBranch"]) : 0,
        //                SellPrice = row["nSellPrice"] != DBNull.Value ? Convert.ToInt32(row["nSellPrice"]) : 0,
        //                PetFood = row["nPetEatGroup"] != DBNull.Value ? Convert.ToInt32(row["nPetEatGroup"]) : 0,
        //                JobClass = row["nJobClass"] != DBNull.Value ? Convert.ToInt32(row["nJobClass"]) : 0,
        //            };

        //            return item;
        //        }

        //        private void UpdateItemFrameValues(DataRowView row)
        //        {
        //            if (itemFrame != null)
        //            {
        //                ItemData item = ExtractItemFromRow(row);

        //                var itemFrameViewModel = (ItemFrameViewModel)DataContext;
        //                itemFrameViewModel.Item = item;
        //            }
        //        }

        //        private GearFrame? gearFrame = null;
        //        private void OpenGearFrame()
        //        {
        //            try
        //            {
        //                if (gearFrame == null)
        //                {
        //                    gearFrame = new GearFrame
        //                    {
        //                        MdiParent = this,
        //                        Location = new Point(350, 35)
        //                    };

        //                    gearFrame.Closed += (sender, e) => gearFrame = null;
        //                }

        //                itemFrame?.Hide();

        //                if (!gearFrame.Visible)
        //                {
        //                    gearFrame.Show();
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                System.Windows.MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }

        //        private static ItemData ExtractGearItemFromRow(DataRowView row)
        //        {
        //            ItemData gearItem = new()
        //            {
        //                Name = row["wszDesc"] as string ?? string.Empty,
        //                Description = row["wszItemDescription"] as string ?? string.Empty,
        //                Type = row["ItemType"] as string ?? string.Empty,
        //                Category = row["nCategory"] != DBNull.Value ? Convert.ToInt32(row["nCategory"]) : 0,
        //                WeaponID00 = row["nWeaponID00"] != DBNull.Value ? Convert.ToInt32(row["nWeaponID00"]) : 0,
        //                SubCategory = row["nSubCategory"] != DBNull.Value ? Convert.ToInt32(row["nSubCategory"]) : 0,
        //                Weight = row["nWeight"] != DBNull.Value ? Convert.ToInt32(row["nWeight"]) : 0,
        //                LevelLimit = row["nLevelLimit"] != DBNull.Value ? Convert.ToInt32(row["nLevelLimit"]) : 0,
        //                ItemTrade = row["nItemTrade"] != DBNull.Value ? Convert.ToInt32(row["nItemTrade"]) : 0,
        //                Durability = row["nDurability"] != DBNull.Value ? Convert.ToInt32(row["nDurability"]) : 0,
        //                Defense = row["nDefense"] != DBNull.Value ? Convert.ToInt32(row["nDefense"]) : 0,
        //                MagicDefense = row["nMagicDefense"] != DBNull.Value ? Convert.ToInt32(row["nMagicDefense"]) : 0,
        //                Branch = row["nBranch"] != DBNull.Value ? Convert.ToInt32(row["nBranch"]) : 0,
        //                SocketCountMin = row["nSocketCountMin"] != DBNull.Value ? Convert.ToInt32(row["nSocketCountMin"]) : 0,
        //                SocketCountMax = row["nSocketCountMax"] != DBNull.Value ? Convert.ToInt32(row["nSocketCountMax"]) : 0,
        //                ReconstructionMax = row["nReconstructionMax"] != DBNull.Value ? Convert.ToInt32(row["nReconstructionMax"]) : 0,
        //                SellPrice = row["nSellPrice"] != DBNull.Value ? Convert.ToInt32(row["nSellPrice"]) : 0,
        //                PetFood = row["nPetEatGroup"] != DBNull.Value ? Convert.ToInt32(row["nPetEatGroup"]) : 0,
        //                JobClass = row["nJobClass"] != DBNull.Value ? Convert.ToInt32(row["nJobClass"]) : 0,
        //                OptionCountMin = row["nOptionCountMin"] != DBNull.Value ? Convert.ToInt32(row["nOptionCountMin"]) : 0,
        //                OptionCountMax = row["nOptionCountMax"] != DBNull.Value ? Convert.ToInt32(row["nOptionCountMax"]) : 0,
        //                SetId = row["nSetId"] != DBNull.Value ? Convert.ToInt32(row["nSetId"]) : 0,
        //                FixOption00 = row["nFixOption00"] != DBNull.Value ? Convert.ToInt32(row["nFixOption00"]) : 0,
        //                FixOptionValue00 = row["nFixOptionValue00"] != DBNull.Value ? Convert.ToInt32(row["nFixOptionValue00"]) : 0,
        //                FixOption01 = row["nFixOption01"] != DBNull.Value ? Convert.ToInt32(row["nFixOption01"]) : 0,
        //                FixOptionValue01 = row["nFixOptionValue01"] != DBNull.Value ? Convert.ToInt32(row["nFixOptionValue01"]) : 0
        //            };

        //            return gearItem;
        //        }


        //        private void UpdateGearFrameValues(DataRowView row)
        //        {
        //            if (gearFrame != null)
        //            {
        //                ItemData gearItem = ExtractGearItemFromRow(row);
        //                gearFrame.SetItemData(gearItem);
        //            }
        //        }

        //        private readonly Dictionary<string, Image> iconImageCache = [];

        //        private BitmapImage LoadIconImage(string? iconName)
        //        {
        //            try
        //            {
        //                if (iconName == null)
        //                {
        //                    return new BitmapImage(new Uri("pack://application:,,,/YourWPFAppName;component/Resources/question_icon.png"));
        //                }

        //                if (iconImageCache.TryGetValue(iconName, out var cachedImage))
        //                {
        //                    return cachedImage;
        //                }

        //                string[] subfolders = Directory.GetDirectories(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources"));

        //                foreach (string subfolder in subfolders)
        //                {
        //                    string[] imageFiles = Directory.GetFiles(subfolder, "*.png", SearchOption.AllDirectories);
        //                    string lowercaseIconName = iconName.ToLower();

        //                    foreach (string imageFilePath in imageFiles)
        //                    {
        //                        string fileName = Path.GetFileNameWithoutExtension(imageFilePath);

        //                        if (string.Equals(fileName, lowercaseIconName, StringComparison.OrdinalIgnoreCase))
        //                        {
        //                            BitmapImage loadedImage = new BitmapImage(new Uri(imageFilePath));
        //                            iconImageCache[iconName] = loadedImage; // Cache the loaded image
        //                            return loadedImage;
        //                        }
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                System.Windows.MessageBox.Show("Error loading image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }

        //            return new BitmapImage(new Uri("pack://application:,,,/YourWPFAppName;component/Resources/question_icon.png"));
        //        }

        //        public void SetIconSlot()
        //        {
        //            try
        //            {
        //                DataGridRow selectedRow = dataGridView.SelectedRows[0];

        //                string? name = selectedRow.Cells["Name"].Value != DBNull.Value ? selectedRow.Cells["Name"].Value.ToString() : "";
        //                string? iconName = selectedRow.Cells["szIconName"].Value != DBNull.Value ? selectedRow.Cells["szIconName"].Value.ToString() : "";
        //                int branch = selectedRow.Cells["nBranch"].Value != DBNull.Value ? Convert.ToInt32(selectedRow.Cells["nBranch"].Value) : 0;

        //                Image image = LoadIconImage(iconName);
        //                int branchId = MapBranchIdToImageIndex(branch);
        //                int amount = (int)numAmount.Value;

        //                Dictionary<int, (Image, Image, Image, Image, Label, int, int, bool, string?)> iconSlotMap = new()
        //                {
        //                    { 0, (mailWindow!.pbIcon01, mailWindow.pbItem01Branch, mailWindow.pbEmpty1, image, mailWindow.lbItemAmount1, amount, branchId, true, name) },
        //                    { 1, (mailWindow.pbIcon02, mailWindow.pbItem02Branch, mailWindow.pbEmpty2, image, mailWindow.lbItemAmount2, amount, branchId, true, name) },
        //                    { 2, (mailWindow.pbIcon03, mailWindow.pbItem03Branch, mailWindow.pbEmpty3, image, mailWindow.lbItemAmount3, amount, branchId, true, name) },
        //                };

        //                if (iconSlotMap.TryGetValue(activeItemIndex, out var iconSlot))
        //                {
        //                    string tooltip = iconSlot.Item9 ?? string.Empty;

        //                    mailWindow.SetpbIconImage(iconSlot.Item1, iconSlot.Item2, iconSlot.Item3, iconSlot.Item4, iconSlot.Item5, iconSlot.Item6, iconSlot.Item7, iconSlot.Item8, tooltip);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                System.Windows.MessageBox.Show($"An error occurred in updating icon slot: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }

        //        private static int MapBranchIdToImageIndex(int branchId)
        //        {
        //            return branchId switch
        //            {
        //                0 or 1 => 0,
        //                2 => 1,
        //                4 => 2,
        //                5 => 3,
        //                6 => 4,
        //                _ => 0,
        //            };
        //        }

        //        #endregion
        //    }
    }
}
