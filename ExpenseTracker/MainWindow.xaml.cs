using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using OfficeOpenXml;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System.Globalization;


namespace ExpenseTracker
{
    public partial class MainWindow : Window
    {
        private readonly string excelFilePath = "expenses.xlsx";

        public SeriesCollection SeriesCollection { get; set; }
        public List<string> Labels { get; set; }
        public Func<double, string> Formatter { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            SeriesCollection = new SeriesCollection();
            Labels = new List<string>();
            Formatter = value => value.ToString("C");

            DataContext = this;

            if (!File.Exists(excelFilePath))
            {
                CreateNewExcelFile();
            }

            LoadExpenses();
        }

        private void CreateNewExcelFile()
        {
            using (ExcelPackage package = new ExcelPackage(new FileInfo(excelFilePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Expenses");
                worksheet.Cells[1, 1].Value = "Наименование";
                worksheet.Cells[1, 2].Value = "Дата";
                worksheet.Cells[1, 3].Value = "Сумма";
                worksheet.Cells[1, 4].Value = "Категория";
                package.Save();
            }
        }


        private void LoadExpenses(DateTime? startDate = null, DateTime? endDate = null)
        {
            var expenses = new Dictionary<string, List<(double, DateTime)>>();

            using (ExcelPackage package = new ExcelPackage(new FileInfo(excelFilePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    string category = worksheet.Cells[row, 4].GetValue<string>();
                    double amount = worksheet.Cells[row, 3].GetValue<double>();
                    DateTime date = worksheet.Cells[row, 2].GetValue<DateTime>();

                    if (startDate.HasValue && date < startDate.Value)
                    {
                        continue;
                    }

                    if (endDate.HasValue && date > endDate.Value)
                    {
                        continue;
                    }

                    if (expenses.ContainsKey(category))
                    {
                        expenses[category].Add((amount, date));
                    }
                    else
                    {
                        expenses[category] = new List<(double, DateTime)> { (amount, date) };
                    }
                }
                double totalExpenses = expenses.Values.SelectMany(list => list.Select(tuple => tuple.Item1)).Sum();

                // Обновление значения TextBlock
                totalExpensesTextBlock.Text = $"Общая сумма: {totalExpenses.ToString("C")}";

            }

            Labels.Clear();
            List<string> categoryDates = new List<string>();
            foreach (var category in expenses.Keys)
            {
                foreach (var expense in expenses[category])
                {
                    categoryDates.Add($"{category}, {expense.Item2:yyyy-MM-dd}: {expense.Item1}");
                }
            }

            Labels.AddRange(categoryDates);
            SeriesCollection.Clear();
            SeriesCollection.Add(new LineSeries
            {
                Title = "Расходы",
                Values = new ChartValues<double>(expenses.Values.SelectMany(list => list.Select(tuple => tuple.Item1)))
            });
            expensesListBox.Items.Clear();
            foreach (var category in expenses.Keys)
            {
                foreach (var expense in expenses[category])
                {
                    expensesListBox.Items.Add($"{category}, {expense.Item2:yyyy-MM-dd}: {expense.Item1}");
                }
            }
        }







        private void btnSave_Click_1(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text;
            DateTime date = datePicker.SelectedDate.Value;
            double amount = double.Parse(txtAmount.Text);
            string category = txtCategory.Text;

            using (ExcelPackage package = new ExcelPackage(new FileInfo(excelFilePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int newRow = worksheet.Dimension.End.Row + 1;
                worksheet.Cells[newRow, 1].Value = name;
                worksheet.Cells[newRow, 2].Value = date;
                worksheet.Cells[newRow, 3].Value = amount;
                worksheet.Cells[newRow, 4].Value = category;
                package.Save();
            }

            LoadExpenses();
        }

        private void btnExport_Click_1(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = "ExportedExpenses.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(excelFilePath, saveFileDialog.FileName, true);
                    MessageBox.Show("Экспорт успешно завершен!", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnFilter_Click_1(object sender, RoutedEventArgs e)
        {
            DateTime? startDate = startDatePicker.SelectedDate;
            DateTime? endDate = endDatePicker.SelectedDate;
            LoadExpenses(startDate, endDate);
        }


        

        private void expensesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (expensesListBox.SelectedIndex != -1)
            {
               
                string[] splitByColon = expensesListBox.SelectedItem.ToString().Split(':');
                string[] expenseData = splitByColon[0].Split(',');

                string category = expenseData[0].Trim();
                string dateString = expenseData[1].Trim();
                double amount = double.Parse(splitByColon[1].Trim().Split(' ')[0]);

                DateTime date;
                string dateFormat = "yyyy-MM-dd";
                if (!DateTime.TryParseExact(dateString, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    MessageBox.Show($"Не удалось разобрать дату \"{dateString}\". Ожидаемый формат: \"{dateFormat}\"");
                    return;
                }

           
                txtName.Text = category;
                datePicker.SelectedDate = date;
                txtAmount.Text = amount.ToString();
                txtCategory.Text = category;

              
                using (ExcelPackage package = new ExcelPackage(new FileInfo(excelFilePath)))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        string excelCategory = worksheet.Cells[row, 4].GetValue<string>();
                        DateTime excelDate = worksheet.Cells[row, 2].GetValue<DateTime>();
                        double excelAmount = worksheet.Cells[row, 3].GetValue<double>();

                        if (excelCategory == category && excelDate == date && excelAmount == amount)
                        {
                            _selectedExpenseRowIndex = row;
                            break;
                        }
                    }
                }
            }
            else
            {
                _selectedExpenseRowIndex = null;
            }

        }
        private int? _selectedExpenseRowIndex;
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedExpenseRowIndex.HasValue)
            {
                string name = txtName.Text;
                DateTime date = datePicker.SelectedDate.Value;
                double amount = double.Parse(txtAmount.Text);
                string category = txtCategory.Text;

                using (ExcelPackage package = new ExcelPackage(new FileInfo(excelFilePath)))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    int rowToUpdate = _selectedExpenseRowIndex.Value;
                    worksheet.Cells[rowToUpdate, 1].Value = name;
                    worksheet.Cells[rowToUpdate, 2].Value = date;
                    worksheet.Cells[rowToUpdate, 3].Value = amount;
                    worksheet.Cells[rowToUpdate, 4].Value = category;
                    package.Save();
                }

                LoadExpenses();
            }
            else
            {
                MessageBox.Show("Выберите расход для редактирования", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedExpenseRowIndex.HasValue)
            {
                using (ExcelPackage package = new ExcelPackage(new FileInfo(excelFilePath)))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    int rowToDelete = _selectedExpenseRowIndex.Value;
                    worksheet.DeleteRow(rowToDelete);
                    package.Save();
                }

                LoadExpenses();
            }
            else
            {
                MessageBox.Show("Выберите расход для удаления", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
