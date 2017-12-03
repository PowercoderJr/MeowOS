using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MeowOS.ProcScheduler
{
    /// <summary>
    /// Логика взаимодействия для ProcessView.xaml
    /// </summary>
    public partial class ProcessView : UserControl
    {
        public const int CONTROL_WIDTH = 290;
        public const int CONTROL_HEIGHT = 53;
        private static readonly Brush[] PRIORITY_BRUSHES = { Brushes.LimeGreen, Brushes.Yellow, Brushes.Orange, Brushes.OrangeRed };
        private Process proc;
        public Process Proc => proc;

        public ProcessView()
        {
            InitializeComponent();
        }

        public ProcessView(Process proc) : this()
        {
            this.proc = proc;
            refresh();
            pidLabel.Content = "PID: " + proc.PID.ToString();
            memLabel.Content = "Память: " + proc.MemRequired.ToString();
            bornLabel.Content = "Рождение: " + proc.BornTime.ToString();
        }

        public void refresh()
        {
            Background = PRIORITY_BRUSHES[(int)proc.Priority];
            burstLabel.Content = "Burst: " + proc.Burst.ToString();
            priorityLabel.Content = "Приоритет: " + proc.Priority.ToString();
            stateLabel.Content = "Состояние: " + proc.State.ToString();
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            killMenuItem.IsEnabled = proc.IsAlive;
            chPriorityMenuItem.IsEnabled = proc.IsAlive || proc.State == Process.States.UNBORN;
        }
    }
}
