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
        private const int MEM_AVAILABLE = 1024;
        private Process[] procs;
        private int[] bornTimes;

        public SchedulerWindow()
        {
            InitializeComponent();
            avMemLabel.Content = "Доступная память: " + MEM_AVAILABLE;
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
            procs = null;
            bornTimes = null;
            logTextbox.Clear();
        }

        private void generateBtn_Click(object sender, RoutedEventArgs e)
        {
            int procAmount = int.Parse(procAmountEdit.Text);
            if (procAmount <= 0)
            {
                MessageBox.Show("Количество процессов должно быть положительным числом");
                return;
            }
            int maxBurst = int.Parse(maxBurstEdit.Text);
            if (maxBurst <= 0)
            {
                MessageBox.Show("Максимальное значение burst должно быть положительным числом");
                return;
            }

            clear();
            procs = new Process[procAmount];
            bornTimes = new int[procAmount];
            Random rnd = new Random();
            int unitsAmount = 0;
            RowDefinition firstRow = new RowDefinition();
            firstRow.Height = new GridLength(30);
            grid.RowDefinitions.Add(firstRow);
            for (int i = 0; i < procAmount; ++i)
            {
                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(50);
                grid.RowDefinitions.Add(row);

                int j;
                do
                {
                    j = rnd.Next(procAmount);
                } while (procs[j] != null);

                procs[j] = new Process(i + 1, (Process.Priorities)rnd.Next(Enum.GetNames(typeof(Process.Priorities)).Length), rnd.Next(maxBurst) + 1, rnd.Next(MEM_AVAILABLE) + 1);
                bornTimes[j] = rnd.Next(unitsAmount) + 1;
                unitsAmount += procs[j].Burst;
                logTextbox.AppendText("Сгенерирован процесс: " +
                    "PID = " + procs[j].PID +
                    ", приоритет = " + procs[j].Priority +
                    ", burst = " + procs[j].Burst +
                    ", потребляемая память = " + procs[j].MemRequired +
                    ", время появления = " + bornTimes[j] +
                    "\n");
            }

            ColumnDefinition firstColumn = new ColumnDefinition();
            firstColumn.Width = new GridLength(150);
            grid.ColumnDefinitions.Add(firstColumn);
            for (int i = 1; i <= unitsAmount; ++i)
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
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            clear();
        }
    }
}
