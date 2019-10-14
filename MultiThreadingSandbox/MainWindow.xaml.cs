using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MultiThreadingSandbox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _asyncOutput;
        private string _syncOutput;
        private int _min;
        private int _max;

        List<Task> _tasksToRun = new List<Task>();
        List<Job> _asyncJobs = new List<Job>();
        List<Job> _syncjobs = new List<Job>();
        Random random = new Random();
        StringBuilder output = new StringBuilder();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string AsyncOutput
        {
            get { return _asyncOutput; }
            set
            {
                if (value != _asyncOutput)
                {
                    _asyncOutput = value;
                    OnPropertyChanged(nameof(AsyncOutput));
                }
            }
        }
        public string SyncOutput
        {
            get { return _syncOutput; }
            set
            {
                if (value != _syncOutput)
                {
                    _syncOutput = value;
                    OnPropertyChanged(nameof(SyncOutput));
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void createJobs(List<Job> jobs)
        {
            int numberOfTasks = 10;
            int.TryParse(tbNumberOfTasks.Text, out numberOfTasks);
            jobs.Clear();
            for (int i = 1; i <= numberOfTasks; i++)
            {
                jobs.Add(new Job(i));
            }
        }

        private async void runAllTasks()
        {
            var progress = new SyncReportJobProcess(this);
            SyncOutput = string.Empty;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            foreach (var job in _syncjobs)
            {
                await printJob(job, progress);
            }
            watch.Stop();
            SyncOutput += Environment.NewLine;
            SyncOutput += Environment.NewLine;
            SyncOutput += $"Synchronous job took {watch.ElapsedMilliseconds}ms...";
        }

        private async Task runAllTasksAsync()
        {
            var progress = new AsyncReportJobProcess(this);
            AsyncOutput = string.Empty;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            foreach (var job in _asyncJobs)
            {
                _tasksToRun.Add(printJob(job, progress));
            }

            await Task.WhenAll(_tasksToRun.ToArray());
            watch.Stop();
            AsyncOutput += Environment.NewLine;
            AsyncOutput += Environment.NewLine;
            AsyncOutput += $"Asynchronous job took {watch.ElapsedMilliseconds}ms...";
        }

        private async Task printJob(Job job, IProgress<JobStatus> progress)
        {
            var randomLatency = random.Next(_min, _max);

            await job.Run(randomLatency, progress);
        }        

        private void BtnRunAllAsync_Click(object sender, RoutedEventArgs e)
        {
            setMinAndMax();
            createJobs(_asyncJobs);
            Task.Run(async () => await runAllTasksAsync());
        }
        private void setMinAndMax()
        {
            int.TryParse(tbMinResponse.Text, out _min);
            int.TryParse(tbMaxResponse.Text, out _max);
        }
        private void BtnRunAllSync_Click(object sender, RoutedEventArgs e)
        {
            setMinAndMax();
            createJobs(_syncjobs);
            runAllTasks();
        }
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            AsyncOutput = null;
            SyncOutput = null;
        }

        #region random classes

        class SyncReportJobProcess : IProgress<JobStatus>
        {
            readonly MainWindow _window;
            public SyncReportJobProcess(MainWindow window)
            {
                _window = window;
            }
            public void Report(JobStatus status)
            {
                if (status.State == JobState.Finished)
                    _window.SyncOutput += $"{Environment.NewLine}Job #{status.JobId} took {status.ElpasedTime.TotalMilliseconds}ms to run on thread {status.ThreadId}";
            }
        }
        
        class AsyncReportJobProcess : IProgress<JobStatus>
        {
            readonly MainWindow _window;
            public AsyncReportJobProcess(MainWindow window)
            {
                _window = window;
            }
            public void Report(JobStatus status)
            {
                if (status.State == JobState.Finished)
                    _window.AsyncOutput += $"{Environment.NewLine}Job #{status.JobId} took {status.ElpasedTime.TotalMilliseconds}ms to run on thread {status.ThreadId}";
            }
        }
        
        class Job
        {
            public int JobNumber { get; private set; }

            public Job(int jobNumber)
            {
                JobNumber = jobNumber;
            }

            public async Task Run(double delay, IProgress<JobStatus> progress)
            {
                // arrange
                JobStatus status = new JobStatus(this.JobNumber);
                // report
                status.State = JobState.Started;
                status.ElpasedTime = TimeSpan.FromMilliseconds(delay);
                
                // run
                await Task.Delay((int)delay).ConfigureAwait(false);

                status.ThreadId = Thread.CurrentThread.ManagedThreadId;
                // report
                status.State = JobState.Finished;
                progress.Report(status);
            }
        }
        
        class JobStatus
        {
            public int JobId { get; private set;}
            public int ThreadId { get; set; }
            public TimeSpan ElpasedTime { get; set; }
            public JobState State { get; set; }

            public JobStatus(int jobId)
            {
                JobId = jobId;
            }

        }

        enum JobState
        {
            Started,
            Finished
        }
        #endregion
        
    }
}
