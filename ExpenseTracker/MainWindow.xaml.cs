using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

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
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // для некоммерческого

            InitializeComponent();
            InitializeCategories();
            CheckAndCreateExcelFile();
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
        private void CreateCategoryPieChart(Dictionary<string, List<(double, DateTime, string)>> expenses)
        {
            categoryPieChart.Series.Clear();

            foreach (var category in expenses.Keys)
            {
                double totalAmount = expenses[category].Sum(expense => expense.Item1);
                PieSeries pieSeries = new PieSeries
                {
                    Title = category,
                    Values = new ChartValues<double> { totalAmount },
                    DataLabels = true
                };

                categoryPieChart.Series.Add(pieSeries);
            }
        }
        private void CheckAndCreateExcelFile()
        {
            string filePath = "expenses.xlsx";
            if (!File.Exists(filePath))
            {
                using (ExcelPackage excelPackage = new ExcelPackage())
                {
                    // Создать лист
                    ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Expenses");

                    // Задать заголовки
                    worksheet.Cells[1, 1].Value = "Наименование";
                    worksheet.Cells[1, 2].Value = "Дата";
                    worksheet.Cells[1, 3].Value = "Сумма";
                    worksheet.Cells[1, 4].Value = "Категория";

                    // Добавить тестовые данные
                    worksheet.Cells[2, 1].Value = "Тестовая запись";
                    worksheet.Cells[2, 2].Value = DateTime.Now.ToShortDateString();
                    worksheet.Cells[2, 3].Value = 100;
                    worksheet.Cells[2, 4].Value = "Продукты";

                    // Сохранить файл
                    FileInfo excelFile = new FileInfo(filePath);
                    excelPackage.SaveAs(excelFile);
                }
            }
        }


        private void LoadExpenses(DateTime? startDate = null, DateTime? endDate = null)
        {

            var expenses = new Dictionary<string, List<(double, DateTime, string)>>();

            using (ExcelPackage package = new ExcelPackage(new FileInfo(excelFilePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    string category = worksheet.Cells[row, 4].GetValue<string>();
                    double amount = worksheet.Cells[row, 3].GetValue<double>();
                    DateTime date = worksheet.Cells[row, 2].GetValue<DateTime>();
                    string name = worksheet.Cells[row, 1].GetValue<string>();
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
                        expenses[category].Add((amount, date, name));
                    }
                    else
                    {
                        expenses[category] = new List<(double, DateTime, string)> { (amount, date, name) };
                    }
                }
                double totalExpenses = expenses.Values.SelectMany(list => list.Select(tuple => tuple.Item1)).Sum();

                // Обновление значения TextBlock
                totalExpensesTextBlock.Text = $"Общая сумма: {totalExpenses.ToString()} $";

            }

            Labels.Clear();
            List<string> categoryDates = new List<string>();
            foreach (var category in expenses.Keys)
            {
                foreach (var expense in expenses[category])
                {
                    categoryDates.Add($"{category}, {expense.Item2:dd-MM-yyyy}: {expense.Item1}");
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




            List<string> expenseItems = new List<string>();
            foreach (var category in expenses.Keys)
            {
                foreach (var expense in expenses[category])
                {
                    string itemName = expense.Item3;
                    expenseItems.Add($"{category}, {expense.Item2:dd-MM-yyyy}, {expense.Item1}, {itemName}");
                }
            }


            // список по категории
            expenseItems = expenseItems.OrderBy(item => item.Split(',')[0]).ToList();

            // отсортированные элементы в expensesListBox
            foreach (string item in expenseItems)
            {
                expensesListBox.Items.Add(item);
            }
            CreateCategoryPieChart(expenses);

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
            string newCategory = txtCategory.Text;

            if (!string.IsNullOrWhiteSpace(newCategory) && !_categories.Contains(newCategory))
            {
                _categories.Add(newCategory);
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
                string[] splitByColon = expensesListBox.SelectedItem.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                string category = splitByColon[0].Trim();
                string dateString = splitByColon[1].Trim();
                double amount = double.Parse(splitByColon[2].Trim().Split(' ')[0]);
                string name = splitByColon[3].Trim();


                DateTime date;
                string dateFormat = "dd-MM-yyyy";
                if (!DateTime.TryParseExact(dateString, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    MessageBox.Show($"Не удалось разобрать дату \"{dateString}\". Ожидаемый формат: \"{dateFormat}\"");
                    return;
                }


                txtName.Text = name;
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


        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    DateTime baseDate = new DateTime(1900, 1, 1);

                    for (int row = 2; row <= rowCount; row++)
                    {
                        string name = worksheet.Cells[row, 1].Value.ToString();
                        int dateInt = int.Parse(worksheet.Cells[row, 2].Value.ToString());
                        DateTime date = baseDate.AddDays(dateInt - 2); // Вычитаем 2, потому что Excel считает 1900 год високосным, хотя на самом деле это не так
                        double amount = double.Parse(worksheet.Cells[row, 3].Value.ToString());
                        string category = worksheet.Cells[row, 4].Value.ToString();

                        // Добавьте код для добавления импортированных данных в ваш файл данных
                        using (ExcelPackage appPackage = new ExcelPackage(new FileInfo(excelFilePath)))
                        {
                            ExcelWorksheet appWorksheet = appPackage.Workbook.Worksheets[0];
                            int newRow = appWorksheet.Dimension.End.Row + 1;
                            appWorksheet.Cells[newRow, 1].Value = name;
                            appWorksheet.Cells[newRow, 2].Value = date;
                            appWorksheet.Cells[newRow, 3].Value = amount;
                            appWorksheet.Cells[newRow, 4].Value = category;
                            appPackage.Save();
                        }
                    }
                }

                // Обновите список и график расходов после импорта данных
                LoadExpenses();
            }
        }

        private void txtAmount_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            double parsedValue;
            if (!double.TryParse(txtAmount.Text, out parsedValue))
            {
                MessageBox.Show("Введите только числа");
                txtAmount.Text = "";
            }
        }

        private void txtName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
        private ObservableCollection<string> _categories;
        private void InitializeCategories()
        {
            _categories = new ObservableCollection<string>
            {
                "Продукты",
                "Транспорт",
                "Развлечения",
                "Одежда",
                "Коммунальные услуги"
            };

            txtCategory.ItemsSource = _categories;
            txtCategory.SelectedIndex = 0;
            txtCategory.IsEditable = true;
        }
    }
}
