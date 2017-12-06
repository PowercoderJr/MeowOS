using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MeowOS.ProcScheduler
{
    //TOOD 03.12: добавить создание процессов с пользовательскими параметрами
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
            stepBtn.IsEnabled = toQuantumBtn.IsEnabled = toEndBtn.IsEnabled = false;
            unitsGrid.Children.Clear();
            unitsGrid.RowDefinitions.Clear();
            unitsGrid.ColumnDefinitions.Clear();
            processesGrid.Children.Clear();
            processesGrid.RowDefinitions.Clear();
            processesGrid.ColumnDefinitions.Clear();
            actionGrid.Children.Clear();
            actionGrid.RowDefinitions.Clear();
            actionGrid.ColumnDefinitions.Clear();
            logTextbox.Clear();
            scheduler.clear();
            refreshFreeMemLabel();
        }

        private void refreshFreeMemLabel()
        {
            frMemLabel.Content = "Свободная память: " + scheduler.FreeMem;
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

            pvs = new ProcessView[procAmount];
            string[] menuItemsHeaders = Enum.GetNames(typeof(Process.Priorities));
            for (int i = 0; i < procAmount; ++i)
            {
                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(ProcessView.CONTROL_HEIGHT);
                processesGrid.RowDefinitions.Add(row);
                pvs[i] = new ProcessView(scheduler.Procs[i]);
                pvs[i].SetValue(Grid.RowProperty, i);
                pvs[i].SetValue(Grid.ColumnProperty, 0);
                processesGrid.Children.Add(pvs[i]);

                row = new RowDefinition();
                row.Height = new GridLength(ProcessView.CONTROL_HEIGHT);
                actionGrid.RowDefinitions.Add(row);

                for (int j = 0; j < menuItemsHeaders.Length; ++j)
                {
                    MenuItem item = new MenuItem();
                    item.Header = menuItemsHeaders[j];
                    item.Tag = j;
                    item.Click += priorityMenuItemClick;
                    pvs[i].chPriorityMenuItem.Items.Add(item);
                }
                pvs[i].killMenuItem.Click += killMenuItemClick;
            }
            RowDefinition lastRow = new RowDefinition();
            lastRow.Height = new GridLength(ProcessView.CONTROL_HEIGHT + 17);
            processesGrid.RowDefinitions.Add(lastRow);
            lastRow = new RowDefinition();
            lastRow.Height = new GridLength(ProcessView.CONTROL_HEIGHT);
            actionGrid.RowDefinitions.Add(lastRow);
            
            for (int i = 0; i < scheduler.UnitsAmount; ++i)
            {
                ColumnDefinition col = new ColumnDefinition();
                col.Width = new GridLength(ProcessView.CONTROL_HEIGHT);
                unitsGrid.ColumnDefinitions.Add(col);
                col = new ColumnDefinition();
                col.Width = new GridLength(ProcessView.CONTROL_HEIGHT);
                actionGrid.ColumnDefinitions.Add(col);
                Label l = new Label();
                l.Content = (i + 1).ToString();
                l.SetValue(Grid.RowProperty, 0);
                l.SetValue(Grid.ColumnProperty, i);
                l.HorizontalContentAlignment = HorizontalAlignment.Center;
                unitsGrid.Children.Add(l);
            }
            ColumnDefinition lastCol = new ColumnDefinition();
            lastCol.Width = new GridLength(ProcessView.CONTROL_HEIGHT + 17);
            unitsGrid.ColumnDefinitions.Add(lastCol);
            lastCol = new ColumnDefinition();
            lastCol.Width = new GridLength(ProcessView.CONTROL_HEIGHT);
            actionGrid.ColumnDefinitions.Add(lastCol);
            stepBtn.IsEnabled = toQuantumBtn.IsEnabled = toEndBtn.IsEnabled = true;
        }

        private void priorityMenuItemClick(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            ProcessView pv = ((item.Parent as MenuItem).Parent as ContextMenu).PlacementTarget as ProcessView;
            Process.Priorities old = pv.Proc.Priority;
            pv.Proc.Priority = (Process.Priorities)item.Tag;
            if (pv.Proc.Priority != old)
            {
                string putToQueue = "";
                if (pv.Proc.IsAlive)
                {
                    scheduler.deqProc(pv.Proc);
                    scheduler.enqProcByPriority(pv.Proc);
                    if (pv.Proc.State == Process.States.RUNNING)
                    {
                        pv.Proc.State = Process.States.READY;
                        scheduler.endCurrQuantum();
                    }
                    putToQueue = " и помещён в конец соответствующей очереди";
                }
                pv.refresh();
                log("Процесс " + pv.Proc.PID + " (" + old + ") сменил приоритет на " + pv.Proc.Priority + putToQueue);
            }
        }

        private void killMenuItemClick(object sender, RoutedEventArgs e)
        {
            ProcessView pv = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget as ProcessView;
            if (pv.Proc.State == Process.States.RUNNING)
                scheduler.endCurrQuantum();
            pv.Proc.State = Process.States.KILLED;
            pv.refresh();
            scheduler.FreeMem += pv.Proc.MemRequired;
            refreshFreeMemLabel();
            log("Процесс " + pv.Proc + " убит (" + pv.Proc.MemRequired + " байт памяти освобождено)");
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
            int pid = scheduler.doUnit();
            if (pid > 0)
            {
                pvs[pid - 1].refresh();
                actionGrid.Children.Add(buildRectangle(pid - 1, scheduler.CurrUnit - 2, Brushes.Black));
            }
            if (autoscrollChb.IsChecked.Value)
            {
                int targetX = (scheduler.CurrUnit - 1) * ProcessView.CONTROL_HEIGHT;
                int leftBorder = (int)actionSV.HorizontalOffset, rightBorder = (int)(actionSV.HorizontalOffset + actionSV.ActualWidth);
                if (targetX < leftBorder || targetX > rightBorder)
                    actionSV.ScrollToHorizontalOffset(targetX - actionSV.ActualWidth + 21);

                if (pid > 0)
                {
                    int targetY = pid * ProcessView.CONTROL_HEIGHT;
                    int topBorder = (int)actionSV.VerticalOffset, botBorder = (int)(actionSV.VerticalOffset + actionSV.ActualHeight);
                    if (targetY < topBorder || targetY > botBorder)
                        actionSV.ScrollToVerticalOffset(targetY - actionSV.ActualHeight + 21);
                }
            }

            for (int i = 0; i < pvs.Length; ++i)
            {
                Process.States state = pvs[i].Proc.State;
                if (i + 1 != pid && (state == Process.States.READY || state == Process.States.WAITING || state == Process.States.BORN))
                    actionGrid.Children.Add(buildRectangle(i, scheduler.CurrUnit - 2, Brushes.Gray));
                pvs[i].refresh();
            }
            refreshFreeMemLabel();
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

        private Rectangle buildRectangle(int row, int column, Brush fillBrush)
        {
            Rectangle r = new Rectangle();
            r.HorizontalAlignment = HorizontalAlignment.Stretch;
            r.VerticalAlignment = VerticalAlignment.Stretch;
            r.Fill = fillBrush;
            r.SetValue(Grid.RowProperty, row);
            r.SetValue(Grid.ColumnProperty, column);
            return r;
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender == actionSV)
            {
                if (actionSV.HorizontalOffset != unitsSV.HorizontalOffset)
                    unitsSV.ScrollToHorizontalOffset(e.HorizontalOffset);
                if (actionSV.VerticalOffset != processesSV.VerticalOffset)
                    processesSV.ScrollToVerticalOffset(e.VerticalOffset);
            }
            else if (sender == unitsSV)
            {
                if (actionSV.HorizontalOffset != unitsSV.HorizontalOffset)
                    actionSV.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
            else if (sender == processesSV)
            {
                if (actionSV.VerticalOffset != processesSV.VerticalOffset)
                    actionSV.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }
    }
}
