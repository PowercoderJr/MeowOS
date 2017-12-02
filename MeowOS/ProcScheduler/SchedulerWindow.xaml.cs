using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MeowOS.ProcScheduler
{
    /// <summary>
    /// Логика взаимодействия для SchedulerWindow.xaml
    /// </summary>
    public partial class SchedulerWindow : Window
    {
        private Scheduler scheduler;

        public SchedulerWindow()
        {
            InitializeComponent();
            scheduler = new Scheduler();
            avMemLabel.Content = "Доступная память: " + Scheduler.AVAILABLE_MEM;
        }

        private void controlDigits(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            int ss = tb.SelectionStart - 1;
            string text = tb.Text;
            tb.Text = string.Concat(tb.Text.Where(char.IsDigit));

            if (!tb.Text.Equals(text))
                tb.SelectionStart = Math.Max(0, ss);
        }

        private void clear()
        {
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();
            logTextbox.Clear();
            scheduler.clear();
        }

        private void generateBtn_Click(object sender, RoutedEventArgs e)
        {
            int procAmount = procAmountEdit.Text.Length > 0 ? int.Parse(procAmountEdit.Text) : 0;
            if (procAmount <= 0)
            {
                MessageBox.Show("Количество процессов должно быть положительным числом",
                    "Укажите входные данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int maxBurst = maxBurstEdit.Text.Length > 0 ? int.Parse(maxBurstEdit.Text) : 0;
            if (maxBurst <= 0)
            {
                MessageBox.Show("Максимальное значение burst должно быть положительным числом",
                    "Укажите входные данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            clear();
            scheduler.init(procAmount, maxBurst, log);

            RowDefinition firstRow = new RowDefinition();
            firstRow.Height = new GridLength(30);
            grid.RowDefinitions.Add(firstRow);
            for (int i = 1; i <= procAmount + 1; ++i)
            {
                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(50);
                grid.RowDefinitions.Add(row);
            }

            ColumnDefinition firstColumn = new ColumnDefinition();
            firstColumn.Width = new GridLength(150);
            grid.ColumnDefinitions.Add(firstColumn);
            for (int i = 1; i <= scheduler.UnitsAmount; ++i)
            {
                ColumnDefinition col = new ColumnDefinition();
                col.Width = new GridLength(50);
                grid.ColumnDefinitions.Add(col);
                Label l = new Label();
                l.Content = i.ToString();
                l.SetValue(Grid.RowProperty, 0);
                l.SetValue(Grid.ColumnProperty, i);
                l.HorizontalContentAlignment = HorizontalAlignment.Center;
                grid.Children.Add(l);
            }
            ColumnDefinition lastCol = new ColumnDefinition();
            lastCol.Width = new GridLength(50);
            grid.ColumnDefinitions.Add(lastCol);
        }

        private void log(string str)
        {
            logTextbox.AppendText(str + "\n");
            logTextbox.ScrollToEnd();
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            clear();
        }

        private void stepBtn_Click(object sender, RoutedEventArgs e)
        {
            scheduler.doUnit();
        }
    }
}
