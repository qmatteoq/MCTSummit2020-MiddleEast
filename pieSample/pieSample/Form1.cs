using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.Charting;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using System.Runtime.InteropServices;
#if NETCOREAPP
using System.IO.Enumeration;
#endif

namespace pieSample
{
    public partial class Form1 : Form
    {
        PieSeries pieSeries;
        public Form1()
        {
            InitializeComponent();
            //this.Text += $" ({RuntimeInformation.FrameworkDescription.Substring(0, RuntimeInformation.FrameworkDescription.LastIndexOf(' '))})";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.radChartView1.Controllers.Add(new ChartSelectionController());
            this.radChartView1.Controllers.Add(new SmartLabelsController()
            {
                Strategy = new PieTwoLabelColumnsStrategy()
            });
            var toolTipController = new ChartTooltipController();
            toolTipController.DataPointTooltipTextNeeded += ToolTipController_DataPointTooltipTextNeeded;
            this.radChartView1.Controllers.Add(toolTipController);

            this.radChartView1.SelectionMode = Telerik.WinControls.UI.ChartSelectionMode.MultipleDataPoints;
            this.radChartView1.SelectedPointChanged += new ChartViewSelectedChangedEventHandler(selectionController_SelectedPointChanged);
            //this.radChartView1.ShowLegend = true;
            //this.radChartView1.ChartElement.LegendElement.Font = this.radChartView1.Font;
            

            this.pieSeries = new PieSeries();
            this.pieSeries.ShowLabels = true;
            this.pieSeries.DrawLinesToLabels = true;
            this.pieSeries.SyncLinesToLabelsColor = true;
            this.pieSeries.Font = this.radChartView1.Font;
            this.pieSeries.RadiusFactor = 0.9f;
            this.pieSeries.Range = new AngleRange(270, 360);

            Theme theme = Theme.ReadCSSText(@"
                                        theme
                                        {
                                           name: ControlDefault;
                                           elementType: Telerik.WinControls.UI.RadChartElement; 
                                           controlType: Telerik.WinControls.UI.RadChartView; 
                                        }

                                        PieSegment
                                        {    
                                            RadiusAspectRatio
                                            {
                                                Value: 0.5;
                                                EndValue: 1;
                                                MaxValue: 1;
                                                Frames: 20;
                                                Interval: 10;
                                                EasingType: OutCircular;
                                                RandomDelay: 100;
                                                RemoveAfterApply: true; 
                                            }
                                        }
                                        ");

            ThemeRepository.Add(theme, false);
        }

        private void ToolTipController_DataPointTooltipTextNeeded(object sender, DataPointTooltipTextNeededEventArgs e)
        {
            e.Text = FormatDataPoint((PieDataPoint)e.DataPoint);
        }

        private void selectionController_SelectedPointChanged(object sender, ChartViewSelectedPointChangedEventArgs args)
        {
            
            this.UpdateDataPoint(args.OldSelectedPoint as PieDataPoint);
            this.UpdateDataPoint(args.NewSelectedPoint as PieDataPoint);
        }

        private void UpdateDataPoint(PieDataPoint point)
        {
            if (point != null)
            {
                if (point.IsSelected)
                {
                    point.OffsetFromCenter = 0.1;
                    point.Label = FormatDataPoint(point);

                }
                else
                {
                    point.OffsetFromCenter = 0;
                    point.Label = String.Empty;
                }
            }
        }

        private string FormatDataPoint(PieDataPoint point)
        {
            return $"{point.LegendTitle} {FormatBytes(point.Value.Value)} {point.Percent:N2} %";
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = this.folderBrowserDialog1.SelectedPath;
            }
        }

        private async void runButton_Click(object sender, EventArgs e)
        {
            this.radChartView1.Series.Clear();
            this.pieSeries.DataPoints.Clear();


            this.runButton.Enabled = false;
            this.radWaitingBar1.Visible = true;
            this.radWaitingBar1.StartWaiting();
            this.statusLabel.Text = "Status: running";
            var sw = Stopwatch.StartNew();
            await Task.Run(() => UpdatePie(this.textBox1.Text, (int)this.numericUpDown1.Value, false));
            sw.Stop();
            this.statusLabel.Text = $"Status: complete. {sw.ElapsedMilliseconds} ms.";
            this.radWaitingBar1.StopWaiting();
            this.radWaitingBar1.Visible = false;
            this.runButton.Enabled = true;

            this.radChartView1.Series.Add(this.pieSeries);
        }

        private void aboutButton_Click(object sender, EventArgs e)
        {
            new AboutBox1().ShowDialog();
        }

        private static string[] byteFormats = { "B", "KB", "MB", "GB", "TB" };

        private static string FormatBytes(double bytes)
        {
            string units = null;
            foreach(var byteFormat in byteFormats)
            {
                units = byteFormat;
                if (bytes > 1024)
                {
                    bytes /= 1024;
                }
                else
                {
                    break;
                }
            }

            return $"{bytes:N3} {units}";
        }
#if NETCOREAPP
        
        private void UpdatePie(string rootDirectory, int depth, bool showFiles)
        {
            if (!Directory.Exists(rootDirectory) && !File.Exists(rootDirectory))
            {
                return;
            }

            var dataPoints = new FileSystemEnumerable<PieDataPoint>(rootDirectory, (ref FileSystemEntry entry) => CreateDataPoint(ref entry, depth), new EnumerationOptions() { RecurseSubdirectories = true, AttributesToSkip = 0 })
            {
                ShouldRecursePredicate = (ref FileSystemEntry entry) => GetDepth(ref entry) < depth,
                ShouldIncludePredicate = (ref FileSystemEntry entry) => showFiles || entry.IsDirectory
            };

            if (!showFiles)
            {
                var dataPoint = new PieDataPoint(GetDirectorySize(rootDirectory, recurse: depth == 0), ".");
                dataPoint.Label = String.Empty;
                this.pieSeries.DataPoints.Add(dataPoint);
            }

            foreach(var dataPoint in dataPoints)
            {
                dataPoint.Label = String.Empty;
                this.pieSeries.DataPoints.Add(dataPoint);
            }

        }


        private static PieDataPoint CreateDataPoint(ref FileSystemEntry entry, int depth)
        {
            string relativeFile;

            if (entry.RootDirectory.Length == entry.Directory.Length)
            {
                relativeFile = entry.FileName.ToString();
            }
            else
            {
                var relativeDir = entry.Directory.Slice(entry.RootDirectory.Length + 1);
                relativeFile = Path.Join(relativeDir, entry.FileName);
            }

            long size = 0;

            if (entry.IsDirectory)
            {
                size = GetDirectorySize(entry.ToFullPath(), GetDepth(ref entry) >= depth);
            }
            else
            {
                size = entry.Length;
            }
            return new PieDataPoint(size, relativeFile);
        }

        private static long GetDirectorySize(string directory, bool recurse)
        {
            return (new FileSystemEnumerable<long>(directory, (ref FileSystemEntry childEntry) => childEntry.Length, new EnumerationOptions() { RecurseSubdirectories = recurse })
            {
                ShouldIncludePredicate = (ref FileSystemEntry childEntry) => !childEntry.IsDirectory
            }).Sum();
        }

        private static int GetDepth(ref FileSystemEntry entry)
        {
            int result = 1;
            for(int i = entry.RootDirectory.Length; i < entry.Directory.Length; i++)
            {
                if (entry.Directory[i] == '\\')
                {
                    result++;
                }
            }
            return result;
        }
#else

        private void UpdatePie(string rootDirectory, int depth, bool showFiles)
        {
            var dataPoints = EnumDirectory(rootDirectory, depth, showFiles);

            foreach (var dataPoint in dataPoints)
            {
                dataPoint.Label = String.Empty;
                this.pieSeries.DataPoints.Add(dataPoint);
            }
        }

        private IEnumerable<PieDataPoint> EnumDirectory(string path, int depth, bool showFiles)
        {
            if (Directory.Exists(path))
            {
                if (path[path.Length - 1] != '\\')
                {
                    path += '\\';
                }
                foreach(var dataPoint in EnumDirectoryEntry(new DirectoryInfo(path), path, depth, showFiles))
                {
                    yield return dataPoint;
                }
            }
            else if (File.Exists(path))
            {
                yield return CreateFileDataPoint(new FileInfo(path), path);
            }
        }

        private IEnumerable<PieDataPoint> EnumDirectoryEntry(DirectoryInfo info, string rootPath, int depth, bool showFiles)
        {
            if (depth <= 0)
            {
                yield return CreateDirectoryDataPoint(info, rootPath);
            }
            else
            {
                long dirSize = 0;

                foreach (var entry in info.EnumerateFileSystemInfos())
                {
                    var dirInfo = entry as DirectoryInfo;

                    if (dirInfo != null)
                    {
                        foreach (var dataPoint in EnumDirectoryEntry(dirInfo, rootPath, depth - 1, showFiles))
                        {
                            yield return dataPoint;
                        }
                    }
                    else
                    {
                        FileInfo fileInfo = entry as FileInfo;

                        if (fileInfo != null)
                        {
                            if (showFiles)
                            {
                                yield return CreateFileDataPoint(fileInfo, rootPath);
                            }
                            else
                            {
                                dirSize += fileInfo.Length;
                            }
                        }
                    }
                }

                if (dirSize != 0)
                {
                    var relativeName = info.FullName.Substring(rootPath.Length);

                    if (relativeName.Length == 0)
                    {
                        relativeName = ".";
                    }

                    yield return new PieDataPoint(dirSize, relativeName);
                }
            }
        }

        private static PieDataPoint CreateFileDataPoint(FileInfo file, string rootPath)
        {
            return new PieDataPoint(file.Length, file.FullName.Substring(rootPath.Length));
        }

        private static PieDataPoint CreateDirectoryDataPoint(DirectoryInfo dir, string rootPath)
        {
            long value = 0;
            foreach(var entry in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                value += entry.Length;
            };
            return new PieDataPoint(value, dir.FullName.Substring(rootPath.Length));
        }

#endif
    }
}
