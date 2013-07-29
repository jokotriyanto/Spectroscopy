﻿// Main form for Spectroscopy Viewer


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;           // For debugging
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;    
using ZedGraph;     // Includes ZedGraph for plotting graphs


namespace Spectroscopy_Viewer
{
    public partial class SpectroscopyViewerForm : Form
    {
        // A list of spectrum objects. List is basically just a dynamic array so we can add more objects as necessary
        public List<spectrum> mySpectrum = new List<spectrum>();
        // List to store data for plotting spectrum graph. PointPairList is the object needed for plotting with zedGraph 
        private List<PointPairList> dataPlot = new List<PointPairList>();

        // Arrays of data for histograms - separate lists for cooling period, count period & all combined
        // Plus an integer to keep track of how large the arrays are
        private int[] histogramCool;
        private int[] histogramCount;
        private int[] histogramAll;
        private int histogramSize;

        private int numberOfSpectra = new int();


        public SpectroscopyViewerForm()
        {
            InitializeComponent();
        }

        // Respond to form 'Load' event
        private void SpectroscopyViewerForm_Load(object sender, EventArgs e)
        {

            // Setup the graph
            createGraph(zedGraphSpectra);
            // Size the control to fill the form with a margin
            SetSize();

            // Disable radio buttons to select histogram display
            // If these are used before the histogram is created, program will crash
            this.histogramDisplayAll.Enabled = false;
            this.histogramDisplayCool.Enabled = false;
            this.histogramDisplayCount.Enabled = false;

            // Disable manual max bin select
            this.histogramMaxBinSelect.Enabled = false;


        }

        // Respond to the form 'Resize' event
        private void SpectroscopyViewerForm_Resize(object sender, EventArgs e)
        {
            SetSize();
        }

        // SetSize() is separate from Resize() so we can 
        // call it independently from the Form1_Load() method
        private void SetSize()
        {
            tabControl1.Location = new Point(10, 10);
            tabControl1.Size = new Size(ClientRectangle.Width - 20,
                                    ClientRectangle.Height - 20);

            zedGraphSpectra.Location = new Point(10, 60);
            // Leave a small margin around the outside of the control
            zedGraphSpectra.Size = new Size(ClientRectangle.Width - 40,
                                    ClientRectangle.Height - 120);
        }



        // Build the Chart - before any data has been added
        private void createGraph(ZedGraphControl zgcSpectrum)
        {
            // get a reference to the GraphPane
            GraphPane myPane = zgcSpectrum.GraphPane;

            // Clear the Titles
            myPane.Title.Text = "";
            myPane.XAxis.Title.Text = "";
            myPane.YAxis.Title.Text = "";
        }

        
        // Update the chart when data has been added/updated
        private void updateGraph(ZedGraphControl zgcSpectrum)
        {
            // get a reference to the GraphPane
            GraphPane myPane = zgcSpectrum.GraphPane;

            // Clear data
            zgcSpectrum.GraphPane.CurveList.Clear();

            for (int i = 0; i < mySpectrum.Count; i++)
            {
                // Generate a red curve with diamond
                // symbols
                LineItem myCurve = myPane.AddCurve("Data Plot",
                      dataPlot[i], Color.Red, SymbolType.Diamond);

                // Tell ZedGraph to refigure the
                // axes since the data have changed
                zgcSpectrum.AxisChange();
                zgcSpectrum.Invalidate();
                // Force redraw of control
            }
        }



        // Respond to 'Load data' button press
        private void loadDataButton_Click(object sender, EventArgs e)
        {
            // Configuring dialog to open a new data file
            openDataFile.InitialDirectory = "Z:/Data";      // Initialise to share drive
            openDataFile.RestoreDirectory = true;           // Open to last viewed directory
            openDataFile.FileName = "";                     // Set default filename to blank

            // Show dialog to open new data file
            // Do not attempt to open file if user has pressed cancel
            if (openDataFile.ShowDialog() != DialogResult.Cancel)
            {

 //               try
   //             {
                    // Create new StreamReader instance to open file
                    System.IO.StreamReader myFile = new System.IO.StreamReader(openDataFile.FileName);
                    // Create new instance of fileHandler to open & process file (pass by reference!)
                    fileHandler myFilehandler = new fileHandler(ref myFile);
                    // Clean up StreamReader instance after fileHandler has finished with it
                    myFile.Close();           // Close object & release resources

                    //*************************************************************//
                    // Pop up dialog box to select which spectrum to add data to, and act accordingly

                    // Check how many interleaved spectra there 
                    int numberInterleaved = myFilehandler.getNumberInterleaved();

                    // Get and store just the name of the file (without full path)
                    string myFileName = Path.GetFileName(openDataFile.FileName);

                    // Create spectrumSelect form, give it list of existing spectra and number of spectra in file
                    spectrumSelect mySpectrumSelectBox = new spectrumSelect(mySpectrum, numberInterleaved, myFileName);
                    mySpectrumSelectBox.ShowDialog();         // Display form & wait until it is closed to continue

                    // Get array of information about which data to add to which spectrum
                    int[] selectedSpectrum = new int[numberInterleaved];
                    selectedSpectrum = mySpectrumSelectBox.selectedSpectrum.ToArray();

                    // Make sure the user didn't press cancel or close the dialog box
                    if (mySpectrumSelectBox.DialogResult == DialogResult.OK)
                    {
                        // For each interleaved spectrum
                        for (int i = 0; i < numberInterleaved; i++)
                        {
                            // If the index >= number of existing spectra, new ones must have been added
                            // (since for a list of N items, index runs from 0 to N-1)
                            if (selectedSpectrum[i] >= numberOfSpectra)
                            {
                                // Get the list filled with data points, add to list of spectra
                                mySpectrum.Add(new spectrum(myFilehandler.getDataPoints(i)));

                                // Set number
                                mySpectrum[selectedSpectrum[i]].setNumber(selectedSpectrum[i]);
                                // Set the name of the spectrum
                                mySpectrum[selectedSpectrum[i]].setName(mySpectrumSelectBox.spectrumNames[selectedSpectrum[i]]);

                                // Add blank PointPairList for storing plot data
                                dataPlot.Add(new PointPairList());

                            }
                            else
                            {
                                // Add list of data points from file handler into existing spectrum
                                mySpectrum[selectedSpectrum[i]].addToSpectrum(myFilehandler.getDataPoints(i));
                            }
                        }
                    }

                    //**************************************************************

                    
  /*              }
                catch (Exception)   // If any general exception is thrown
                {
                    MessageBox.Show("Error");

                }*/
            }

            // Update number of spectra
            numberOfSpectra = mySpectrum.Count();
        }


        // Method to respond to 'Plot data' button press
        private void plotDataButton_Click(object sender, EventArgs e)
        {
            if (mySpectrum.Count == 0) MessageBox.Show("No data loaded");
            else
            {
                // Analyse each spectrum and get the data
                // NB if no spectra have been loaded, mySpectrum.Count will be 0 and this loop will not run
                for (int i = 0; i < mySpectrum.Count; i++)
                {
                    mySpectrum[i].analyse((int)coolingThresholdSelect.Value, (int)countThresholdSelect.Value);
                    dataPlot[i] = mySpectrum[i].getDataPlot();
                }

                // Setup the graph
                updateGraph(zedGraphSpectra);
                // Size the control to fill the form with a margin
                SetSize();
            }
        }


        // Function to output contents of spectra to file. For testing.
        private void writeToFile_test()
        {
            TextWriter[] testFile = new StreamWriter[mySpectrum.Count];


            // Write a separate file for each spectrum
            for (int i = 0; i < numberOfSpectra; i++)
            {
                testFile[i] = new StreamWriter("C:/Users/localadmin/Documents/testFile_Spectrum" + i + ".txt");
                testFile[i].WriteLine("Frequency\tDark ion prob");

                // For each point pair in the list
                for (int j = 0; j < dataPlot[i].Count; j++)
                {
                    testFile[i].WriteLine(dataPlot[i][j].X + "\t" + dataPlot[i][j].Y + "\n");
                }
                testFile[i].Flush();
                testFile[i].Close();
             }
        }

        private void updateHistogramButton_Click(object sender, EventArgs e)
        {
            // Calculating data for histogram
            //********************************//

            // Initialise variables every time we re-create the histogram
            histogramSize = new int();

            // Local variables used within this method
            int[] tempHistogramCool;
            int[] tempHistogramCount;
            int tempHistogramSize = new int();
            

            // For each spectrum
            for (int i = 0; i < numberOfSpectra; i++)
            {
                // Temporarily store histograms for this spectrum
                tempHistogramCool = mySpectrum[i].getHistogramCool();
                tempHistogramCount = mySpectrum[i].getHistogramCount();

                // Find size of histograms for this spectrum
                tempHistogramSize = tempHistogramCool.Length;

                // For the first spectrum only
                if (i == 0)
                {
                    // Store size of lists
                    histogramSize = tempHistogramSize;

                    // Create binding lists using the histogram data
                    histogramCool = tempHistogramCool;
                    histogramCount = tempHistogramCount;
                    histogramAll = new int[histogramSize];

                    // Calculate total data and store in another list (cool + count)
                    for (int j = 0; j < histogramSize; j++)
                    {
                        histogramAll[j] = histogramCool[j] + histogramCount[j];
                    }
            
                }
                else
                {   // For subsequent spectra, go through and add the data to existing lists
                    for (int j = 0; j < histogramSize; j++)
                    {
                        // Sum the data from each spectrum into the full list
                        histogramCool[j] += tempHistogramCool[j];
                        histogramCount[j] += tempHistogramCount[j];

                        histogramAll[j] = histogramCool[j] + histogramCount[j];

                    }

                    // If the histogram for the current spectrum is larger than the existing histogram
                    if (tempHistogramSize > histogramSize)
                    {
                        // Extend the size of each array
                        histogramCool = extendArray(histogramCool, tempHistogramSize);
                        histogramCount = extendArray(histogramCount, tempHistogramSize);
                        histogramAll = extendArray(histogramAll, tempHistogramSize);

                        // Fill in the data into the new bins
                        for (int j = histogramSize; j < tempHistogramSize; j++)
                        {
                            histogramCool[j] = tempHistogramCool[j];
                            histogramCount[j] = tempHistogramCount[j];
                            histogramAll[j] = histogramCool[j] + histogramCount[j];
                        }

                        // Update size of list (could use tempHistogramSize, but recalculate just in case)
                        histogramSize = histogramCool.Count();
                    }
                }

            }

            //********************************//


            // Store the data in a table for plotting to graph
            // Try to create a data table with the lists as columns
            DataSet histogramDataSet = new DataSet();
            DataTable histogramTable = new DataTable();

            histogramDataSet.Tables.Add(histogramTable);

            // Create columns
            histogramTable.Columns.Add(new DataColumn("Bin", typeof(int) ) );
            histogramTable.Columns.Add(new DataColumn("Cool period", typeof(int)));
            histogramTable.Columns.Add(new DataColumn("Count period", typeof(int)));
            histogramTable.Columns.Add(new DataColumn("All", typeof(int)));

            for (int i = 0; i < histogramSize; i++)
            {
                DataRow myRow = histogramTable.NewRow();
                myRow["Bin"] = i;
                myRow["Cool period"] = histogramCool[i];
                myRow["Count period"] = histogramCount[i];
                myRow["All"] = histogramAll[i];
                histogramTable.Rows.Add(myRow);
            }

            //********************************//
            // Plotting histogram data on graph
            // Need to convert to an enumerable type to get it to dataBind properly

            this.histogramChart.DataBindings.Clear();
            this.histogramChart.Series.Clear();

            var enumerableTable = (histogramTable as System.ComponentModel.IListSource).GetList();
            this.histogramChart.DataBindTable(enumerableTable, "Bin");

            // This line throws an error when chart already exists & update button is pressed

            // Turn off ticks on x axis
            histogramChart.ChartAreas[0].AxisX.MajorGrid.Enabled = false;

            // Enable radio buttons to select display
            histogramDisplayAll.Enabled = true;
            histogramDisplayCool.Enabled = true;
            histogramDisplayCount.Enabled = true;

            // Set interval to 1 so that the number will be displayed for each bin
            histogramChart.ChartAreas[0].AxisX.Interval = 1;

            // Check which radio button is checked & plot correct series
            this.radioButtonDisplay_CheckedChanged(sender, e);
    
        }

        // Method to extend the size of an existing array
        private int[] extendArray(int[] array, int newSize)
        {
            // Store data in a temporary array
            int[] tempArray = array;

            // Create a new array
            array = new int[newSize];

            // Loop through elements of old array, fill in new array
            for (int i = 0; i < tempArray.Count(); i++)
            {
                array[i] = tempArray[i];
            }

            return array;
        }


        // Method to be called when a change is made to the radio buttons controlling the histogram display
        private void radioButtonDisplay_CheckedChanged(object sender, EventArgs e)
        {
            // For each radio button (All, Cool, Count)
            // If the button is checked, display the corresponding series
            // If the button is unchecked, hide the corresponding series

            // For "All" radio button
            if (histogramDisplayAll.Checked)
            {
                this.histogramChart.Series["All"].Enabled = true;
                this.histogramAutoScale(histogramAll);
            }
            else this.histogramChart.Series["All"].Enabled = false;

            // For "Cooling period only" radio button
            if (histogramDisplayCool.Checked)
            {
                this.histogramChart.Series["Cool period"].Enabled = true;
                this.histogramAutoScale(histogramCool);
            }
            else this.histogramChart.Series["Cool period"].Enabled = false;

            // For "Count period only" radio button
            if (histogramDisplayCount.Checked)
            {
                this.histogramChart.Series["Count period"].Enabled = true;
                this.histogramAutoScale(histogramCount);
            }

            else this.histogramChart.Series["Count period"].Enabled = false;
        }

        // Method to scale the axes based on the data being plotted (All, Cool or Counts)
        private void histogramAutoScale(int[] data)
        {
            // Specify an interval to round to based on size of data
            int maxData = data.Max();
            int interval = new int();
            if (maxData <= 100)
            {
                interval = 20;
            }
            else if (maxData <= 250)
            {
                interval = 50;
            }
            else if (maxData <= 500)
            {
                interval = 100;
            }
            else
            {
                interval = 200;
            }
            
            // Find out how many intervals fit into the data range
            // (rounded down to an integer)
            int x = data.Max() / interval;

            // Set the max to one interval greater
            histogramChart.ChartAreas[0].AxisY.Maximum = interval * (x + 1);
        }

        // Method to respond to "Auto" checkbox (under Histogram tab, Maximum bin group) changing
        private void histogramCheckBoxAuto_CheckedChanged(object sender, EventArgs e)
        {
            // If selecting auto, then disable user maxBinSelect
            if (histogramCheckBoxAuto.Checked)
            {
                histogramMaxBinSelect.Enabled = false;
                this.histogramChart.ChartAreas[0].AxisX.Maximum = histogramSize;
            }
            else
                // If not on auto, scale according to user max bin select
            {
                // Enable user select for max bin
                histogramMaxBinSelect.Enabled = true;
                // NB no code in place to create a ">= N" bin, all this does is change the display
                this.histogramChart.ChartAreas[0].AxisX.Maximum = (double)histogramMaxBinSelect.Value;
            }

        }

        // Method to respond to user changing value in the histogram max bin select
        private void histogramMaxBinSelect_ValueChanged(object sender, EventArgs e)
        {
            // NB nothing clever, we don't change the data, just the display

            // Set maximum bin to user input
            this.histogramChart.ChartAreas[0].AxisX.Maximum = (double)histogramMaxBinSelect.Value;
        }

        // Method to respond to click of "Export histogram data..." button
        // Opens a dialogue to save spectrum data independently for each displayed spectrum
        private void histogramExportData_Click(object sender, EventArgs e)
        {
            
            // Configuring dialog to save file
            saveHistogramFile.InitialDirectory = "Z:/Data";      // Initialise to share drive
            saveHistogramFile.RestoreDirectory = true;           // Open to last viewed directory
            saveHistogramFile.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            // Show new dialogue for each spectrum
            for (int i = 0; i < numberOfSpectra; i++)
            {
                saveHistogramFile.Title = "Save histogram data for spectrum" + (i + 1);
                saveHistogramFile.FileName = mySpectrum[i].getName() + " histogram data.txt";

                // Show dialog to save file
                // Check that user has not pressed cancel before continuing to save file
                if (saveHistogramFile.ShowDialog() != DialogResult.Cancel)
                {
                    // Create streamwriter object to write to file
                    // With filename given from user input
                    TextWriter histogramFile = new StreamWriter(saveHistogramFile.FileName);
                    // Write column titles
                    histogramFile.WriteLine("Bin\tTotal\tCooling period\tCountperiod");

                    // Go through each bin, write data to the file
                    for (int j = 0; j < histogramSize; j++)
                    {
                        histogramFile.WriteLine(j + "\t" + histogramAll[j] + "\t"
                                                + histogramCool[j] + "\t" + histogramCount[j]);
                    }
                    // Flush & close file when finished
                    histogramFile.Flush();
                    histogramFile.Close();
                }
            }

        }



    }
}
