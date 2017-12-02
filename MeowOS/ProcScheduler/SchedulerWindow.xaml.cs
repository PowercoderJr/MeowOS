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
        private ProcessView[] pvs;

        public SchedulerWindow()
        {
            InitializeComponent();
            scheduler = new Scheduler();
            avMemLabel.Content = "Доступная память: " + Scheduler.AVAILABLE_MEM;
            frMemLabel.Content = "Свободная память: " + Scheduler.AVAILABLE_MEM;
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
            frMemLabel.Content = "Свободная память: " + Scheduler.AVAILABLE_MEM;
        }

        private void generateBtn_Click(object sender, RoutedEventArgs e)
        {
            stepBtn.IsEnabled = toQuantumBtn.IsEnabled = toEndBtn.IsEnabled = false;
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
            pvs = new ProcessView[procAmount];
            string[] menuItemsHeaders = Enum.GetNames(typeof(Process.Priorities));

            RowDefinition firstRow = new RowDefinition();
            firstRow.Height = new GridLength(30);
            grid.RowDefinitions.Add(firstRow);
            for (int i = 1; i <= procAmount; ++i)
            {
                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(ProcessView.CONTROL_HEIGHT);
                grid.RowDefinitions.Add(row);
                pvs[i - 1] = new ProcessView(scheduler.Procs[i - 1], scheduler.BornTimes[i - 1]);
                pvs[i - 1].SetValue(Grid.RowProperty, i);
                pvs[i - 1].SetValue(Grid.ColumnProperty, 0);
                grid.Children.Add(pvs[i - 1]);
                for (int j = 0; j < menuItemsHeaders.Length; ++j)
                {
                    MenuItem item = new MenuItem();
                    item.Header = menuItemsHeaders[j];
                    item.Tag = j;
                    item.Click += priorityMenuItemClick;
                    pvs[i - 1].chPriorityMenuItem.Items.Add(item);
                }
                pvs[i - 1].killMenuItem.Click += killMenuItemClick;
            }
            RowDefinition lastRow = new RowDefinition();
            lastRow.Height = new GridLength(ProcessView.CONTROL_HEIGHT);
            grid.RowDefinitions.Add(lastRow);

            ColumnDefinition firstColumn = new ColumnDefinition();
            firstColumn.Width = new GridLength(ProcessView.CONTROL_WIDTH);
            grid.ColumnDefinitions.Add(firstColumn);
            for (int i = 1; i <= scheduler.UnitsAmount; ++i)
            {
                ColumnDefinition col = new ColumnDefinition();
                col.Width = new GridLength(ProcessView.CONTROL_HEIGHT);
                grid.ColumnDefinitions.Add(col);
                Label l = new Label();
                l.Content = i.ToString();
                l.SetValue(Grid.RowProperty, 0);
                l.SetValue(Grid.ColumnProperty, i);
                l.HorizontalContentAlignment = HorizontalAlignment.Center;
                grid.Children.Add(l);
            }
            ColumnDefinition lastCol = new ColumnDefinition();
            lastCol.Width = new GridLength(ProcessView.CONTROL_HEIGHT);
            grid.ColumnDefinitions.Add(lastCol);
            stepBtn.IsEnabled = toQuantumBtn.IsEnabled = toEndBtn.IsEnabled = true;
        }

        private void priorityMenuItemClick(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            ProcessView pv = ((item.Parent as MenuItem).Parent as ContextMenu).PlacementTarget as ProcessView;
            pv.Proc.Priority = (Process.Priorities)item.Tag;
            pv.refresh();
        }

        private void killMenuItemClick(object sender, RoutedEventArgs e)
        {
            ProcessView pv = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget as ProcessView;
            pv.Proc.State = Process.States.KILLED;
            pv.refresh();
        }

        private void log(string str)
        {
            logTextbox.AppendText(str + "\n");
            logTextbox.ScrollToEnd();
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            stepBtn.IsEnabled = toQuantumBtn.IsEnabled = toEndBtn.IsEnabled = false;
            clear();
        }

        private void stepBtn_Click(object sender, RoutedEventArgs e)
        {
            int pid = scheduler.doUnit();
            if (pid > 0)
                pvs[pid - 1].refresh();
            for (int i = 0; i < pvs.Length; ++i)
            {
                if (pvs[i].Proc.State == Process.States.RUNNING || pvs[i].Proc.State == Process.States.READY || pvs[i].Proc.State == Process.States.WAITING)
                {
                    Rectangle r = new Rectangle();
                    r.HorizontalAlignment = HorizontalAlignment.Stretch;
                    r.VerticalAlignment = VerticalAlignment.Stretch;
                    r.Fill = pvs[i].Proc.State == Process.States.RUNNING ? Brushes.Black : Brushes.Gray;
                    r.SetValue(Grid.RowProperty, i + 1);
                    r.SetValue(Grid.ColumnProperty, scheduler.CurrUnit - 1);
                    grid.Children.Add(r);
                }
            }
            frMemLabel.Content = "Свободная память: " + scheduler.FreeMem;
        }

        private void toQuantumBtn_Click(object sender, RoutedEventArgs e)
        {
            do
            {
                stepBtn_Click(sender, e);
            } while (!scheduler.QuantumEndedFlag);
        }

        private void toEndBtn_Click(object sender, RoutedEventArgs e)
        {
            int n = scheduler.UnitsAmount;
            for (int i = scheduler.CurrUnit; i <= n; ++i)
                stepBtn_Click(sender, e);
        }
    }
}
