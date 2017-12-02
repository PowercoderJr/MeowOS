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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MeowOS.ProcScheduler
{
    /// <summary>
    /// Логика взаимодействия для ProcessView.xaml
    /// </summary>
    public partial class ProcessView : UserControl
    {
        public const int CONTROL_WIDTH = 270;
        public const int CONTROL_HEIGHT = 53;
        private static readonly Brush[] PRIORITY_BRUSHES = { Brushes.LimeGreen, Brushes.Yellow, Brushes.Orange, Brushes.OrangeRed };
        private Process proc;
        public Process Proc => proc;

        public ProcessView()
        {
            InitializeComponent();
            Width = CONTROL_WIDTH;
            Height = CONTROL_HEIGHT;
        }

        public ProcessView(Process proc, int bornTime) : this()
        {
            this.proc = proc;
            refresh();
            bornLabel.Content = "Время появления: " + bornTime.ToString();
        }

        public void refresh()
        {
            Background = PRIORITY_BRUSHES[(int)proc.Priority];
            pidLabel.Content = "PID: " + proc.PID.ToString();
            burstLabel.Content = "Burst: " + proc.Burst.ToString();
            priorityLabel.Content = "Приоритет: " + proc.Priority.ToString();
            stateLabel.Content = "Состояние: " + proc.State.ToString();
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            chPriorityMenuItem.IsEnabled = killMenuItem.IsEnabled = proc.State != Process.States.KILLED;
        }
    }
}
