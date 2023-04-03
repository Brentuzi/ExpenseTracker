using System;
using System.Windows;

namespace ExpenseTracker
{
    public partial class EditExpenseWindow : Window
    {
        public string NameValue { get; private set; }
        public DateTime DateValue { get; private set; }
        public double AmountValue { get; private set; }
        public string CategoryValue { get; private set; }

        public EditExpenseWindow(string name, DateTime date, double amount, string category)
        {
            InitializeComponent();
            txtEditName.Text = name;
            datePickerEdit.SelectedDate = date;
            txtEditAmount.Text = amount.ToString();
            txtEditCategory.Text = category;
        }

        private void btnSaveEdit_Click(object sender, RoutedEventArgs e)
        {
            NameValue = txtEditName.Text;
            DateValue = datePickerEdit.SelectedDate.Value;
            AmountValue = double.Parse(txtEditAmount.Text);
            CategoryValue = txtEditCategory.Text;
            DialogResult = true;
            Close();
        }
    }
}
