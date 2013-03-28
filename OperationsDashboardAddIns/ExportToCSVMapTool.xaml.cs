//Copyright 2012 Esri
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.​

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using System.Windows.Forms;
using System.IO;
using OperationsDashboardAddIns.Helpers;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;

namespace OperationsDashboardAddIns
{
  [Export("ESRI.ArcGIS.OperationsDashboard.MapTool")]
  [ExportMetadata("DisplayName", "Export Point Features To CSV")]
  [ExportMetadata("Description", "Exports selected point features to CSV format")]
  [ExportMetadata("ImagePath", "/OperationsDashboardAddIns;component/Images/export.png")]
  [DataContract]
  public partial class ExportToCSVMapTool : System.Windows.Controls.UserControl, IMapTool
  {
    //set initial path to my documents 
    private string _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    private HashSet<FeatureLayer> _featureLayerSet = new HashSet<FeatureLayer>();

    public ExportToCSVMapTool()
    {
      InitializeComponent();

    }

    #region IMapTool


    public MapWidget MapWidget { get; set; }


    public void OnActivated()
    {
      getAllFeatureLayers();
    }

    public void OnDeactivated()
    {

    }


    public bool CanConfigure
    {
      get { return true; }
    }


    public bool Configure(System.Windows.Window owner)
    {

      //open the windows folder browser dialog
      System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
      folderDialog.Description = "Select the directory to save the exported files";
      folderDialog.SelectedPath = _folderPath;
      DialogResult result = folderDialog.ShowDialog();
      if (result == DialogResult.OK)
        _folderPath = folderDialog.SelectedPath;

      return true;
    }

    #endregion

    #region Private methods
    private async Task writeToFile(SelectedFeatures featureSelectionDataSource)
    {
      Task t = new Task(exportFeatures, featureSelectionDataSource);
      t.Start();
      await t;
    }
    private void exportFeatures(object parameters)
    {
      SelectedFeatures featureSelectionDataSource = (SelectedFeatures)parameters;

      string separator = ",";

      StreamWriter sw = new StreamWriter(_folderPath + "\\" + featureSelectionDataSource.Name + ".csv");

      // now add the header in csv file suffix with "," delimeter except last one

      for (int i = 0; i < featureSelectionDataSource.Fields.Count(); i++)
      {
        sw.Write(featureSelectionDataSource.Fields[i].Name);
        if (i != featureSelectionDataSource.Fields.Count() - 1)
          sw.Write(",");
      }
      // add new line
      sw.Write(sw.NewLine);

      foreach (Graphic graphic in featureSelectionDataSource.SelectedGraphics)
      {
        StringBuilder sb = new StringBuilder();
        string fieldValue = string.Empty;
        for (int i = 0; i < featureSelectionDataSource.Fields.Count(); i++)
        {
          if (i != featureSelectionDataSource.Fields.Count() - 1)
            sb.Append(graphic.Attributes[featureSelectionDataSource.Fields[i].Name] != null ? graphic.Attributes[featureSelectionDataSource.Fields[i].Name] + separator : " " + separator);
          else
            sb.Append(graphic.Attributes[featureSelectionDataSource.Fields[i].Name] != null ? graphic.Attributes[featureSelectionDataSource.Fields[i].Name] + "" : " ");
        }
        sw.WriteLine(sb.ToString());
      }

      sw.Flush();
      sw.Close();
    }


    #endregion

    #region Presentation
    private void getAllFeatureLayers()
    {

      //loop through each data source in the dashboard
      foreach (ESRI.ArcGIS.OperationsDashboard.DataSource ds in OperationsDashboard.Instance.DataSources)
      {
        //get the feature layer associated with the data source
        FeatureLayer featureLayer = this.MapWidget.FindFeatureLayer(ds);

        //check if the feature layer is null. This can be null if users add standalone data source
        if (featureLayer != null)
        {
          //add the feature layer to the set if the geometry is map point and has some features in it
          if (featureLayer.Graphics.Count > 0 && featureLayer.Graphics[0].Geometry != null && featureLayer.Graphics[0].Geometry is MapPoint)
            _featureLayerSet.Add(featureLayer);
        }

      }
    }

    private void OnExportMapToolClick(object sender, RoutedEventArgs e)
    {
      try
      {
        if (!_featureLayerSet.Any(f => f.SelectedGraphics.Count() > 0))
        {
          System.Windows.MessageBox.Show("Please select features to export", "No Selection");
          return;
        }
        //create a list of all the export tasks
        List<Task> exportTasks = new List<Task>();

        //loop through each feature layer in the set
        foreach (FeatureLayer featureLayer in _featureLayerSet)
        {
          //if there are any selected features in the layer
          if (featureLayer.SelectedGraphics.Count() > 0)
          {
            //if the selected feature is a poitn feature
            if (featureLayer.SelectedGraphics.First().Geometry is MapPoint)
            {
              //check if the file already exists
              if (File.Exists(_folderPath + "\\" + featureLayer.LayerInfo.Name + ".csv"))
              {
                MessageBoxResult result = System.Windows.MessageBox.Show("File with similar name already exists at this location. Do you want to overwrite the existing file?", "File Already Exists", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                  //add the task to list
                  exportTasks.Add(writeToFile(new SelectedFeatures(featureLayer.LayerInfo.Name, featureLayer.LayerInfo.Fields, featureLayer.SelectedGraphics)));
              }
              else
                exportTasks.Add(writeToFile(new SelectedFeatures(featureLayer.LayerInfo.Name, featureLayer.LayerInfo.Fields, featureLayer.SelectedGraphics)));
            }
          }
        }

        //chekc if there are any export tasks
        if (exportTasks.Count() > 0)
        {
          Task[] exportTasksArray = exportTasks.ToArray();
          //show the completed message box only when all the tasks in the list are completed
          Task.WhenAll(exportTasksArray);
          MessageBoxResult result = System.Windows.MessageBox.Show("Selected features have been successfully exported to " + _folderPath, "Export to CSV Successful!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

      }
      catch (Exception ex)
      {
        System.Windows.MessageBox.Show(ex.Message, "Error exporting features", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      ToggleButton.IsChecked = false;
    }


    #endregion

  }
}
