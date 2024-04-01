//using RHGMTool.Models;

//namespace RHGMTool.Utilities
//{
//    public class FormUtils
//    {
//        #region Move Form
//        private static bool isDragging = false;
//        private static Point offset;

//        public static void AttachFormDragEvent(Form form)
//        {
//            form.MouseDown += (sender, e) =>
//            {
//                isDragging = true;
//                offset = new Point(e.X, e.Y);
//            };

//            form.MouseMove += (sender, e) =>
//            {
//                if (isDragging)
//                {
//                    Point newLocation = form.PointToScreen(new Point(e.X, e.Y));
//                    form.Location = new Point(newLocation.X - offset.X, newLocation.Y - offset.Y);
//                }
//            };

//            form.MouseUp += (sender, e) =>
//            {
//                isDragging = false;
//            };
//        }
//        #endregion

//        #region Button Events
//        public static void AttachMouseButtonEvents(Button button)
//        {
//            button.MouseHover += (sender, e) => UpdateButtonImageIndex(sender, 2);
//            button.MouseDown += (sender, e) => UpdateButtonImageIndex(sender, 1);
//            button.MouseLeave += (sender, e) => UpdateButtonImageIndex(sender, 0);
//        }

//        public static void AttachCloseButtonEvents(Button button)
//        {
//            button.Click += CloseForm;
//            button.MouseHover += (sender, e) => UpdateButtonImageIndex(sender, 1);
//            button.MouseDown += (sender, e) => UpdateButtonImageIndex(sender, 2);
//            button.MouseLeave += (sender, e) => UpdateButtonImageIndex(sender, 0);
//        }

//        private static void CloseForm(object? sender, EventArgs e)
//        {
//            if (sender is Button button)
//            {
//                button?.Invoke((MethodInvoker)delegate
//                {
//                    button.FindForm()?.Close();
//                });
//            }
//        }

//        private static void UpdateButtonImageIndex(object? sender, int imageIndex)
//        {
//            if (sender is Button button)
//            {
//                button?.Invoke((MethodInvoker)delegate
//                {
//                    button.ImageIndex = imageIndex;
//                });
//            }
//        }
//        #endregion

//        #region Combobox Control Events
//        public static void PopulateComboBox<T>(ComboBox comboBox, Func<Enum, string> getDescriptionFunction, int selectedIndex, string? optionalItemName = null) where T : Enum
//        {
//            try
//            {
//                comboBox.Items.Clear();

//                if (!string.IsNullOrEmpty(optionalItemName))
//                {
//                    comboBox.Items.Add(new NameID { ID = 0, Name = optionalItemName });
//                }

//                foreach (var value in Enum.GetValues(typeof(T)))
//                {
//                    int id = (int)value;
//                    string description = getDescriptionFunction((Enum)value);

//                    comboBox.Items.Add(new NameID { ID = id, Name = description });
//                }

//                comboBox.DisplayMember = "Name";
//                comboBox.ValueMember = "ID";
//                comboBox.SelectedIndex = selectedIndex;
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error populating combobox: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        public static void SetComboBoxSelectedValue(ComboBox comboBox, int selectedValue)
//        {
//            NameID? selectedItem = comboBox.Items.Cast<NameID>().FirstOrDefault(item => item.ID == selectedValue);
//            comboBox.SelectedItem = selectedItem ?? comboBox.Items[0];
//        }

//        public static void RemoveComboBoxItemByID(ComboBox comboBox, int itemID)
//        {
//            var itemToRemove = comboBox.Items.Cast<NameID>().FirstOrDefault(item => item.ID == itemID);

//            if (itemToRemove != null)
//            {
//                comboBox.Items.Remove(itemToRemove);
//            }
//        }
//        #endregion

//        #region Message Box Events
//        public static bool ConfirmMessage(string message)
//        {
//            DialogResult dialogResult = MessageBox.Show($"{message}", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
//            return dialogResult == DialogResult.Yes;
//        }

//        public static void HandleError(string message)
//        {
//            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//        }

//        #endregion

//        #region Controls
//        public static void PositionControlAtCenter(Control control, int formWidth)
//        {
//            // Calculate the X-coordinate to center the control horizontally
//            int xCoordinate = (formWidth - control.Width) / 2;

//            // Keep the Y-coordinate unchanged
//            int yCoordinate = control.Location.Y;

//            // Set the control's new location
//            control.Location = new Point(xCoordinate, yCoordinate);
//        }

//        #endregion

//        public static void OpenForm<T>(params object?[] constructorArgs) where T : Form
//        {
//            try
//            {
//                var form = Application.OpenForms.OfType<T>().FirstOrDefault();
//                if (form == null || form.IsDisposed)
//                {
//                    form = Activator.CreateInstance(typeof(T), constructorArgs) as T;
//                }

//                if (form != null)
//                {
//                    if (form.InvokeRequired)
//                    {
//                        form.Invoke(new Action(() =>
//                        {
//                            if (form.WindowState == FormWindowState.Minimized)
//                            {
//                                form.WindowState = FormWindowState.Normal;
//                            }

//                            if (!form.Visible)
//                            {
//                                form.Show();
//                            }
//                        }));
//                    }
//                    else
//                    {
//                        if (form.WindowState == FormWindowState.Minimized)
//                        {
//                            form.WindowState = FormWindowState.Normal;
//                        }

//                        if (!form.Visible)
//                        {
//                            form.Show();
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error opening form: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//    }
//}