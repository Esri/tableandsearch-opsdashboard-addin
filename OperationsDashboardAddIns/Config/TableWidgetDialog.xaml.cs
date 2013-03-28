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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace OperationsDashboardAddIns.Config
{
  public partial class TableWidgetDialog : Window
  {
    private DataSource _dataSource = null;

    public DataSource DataSource
    {
      get { return _dataSource; }
      set
      {
        _dataSource = value;
        DataSourceSelector.SelectedDataSource = _dataSource;
      }
    }

    private string _caption = "New Table Widget";
    public string Caption
    {
      get { return _caption; }
      private set
      {
        _caption = value;
        CaptionTextBox.Text = _caption;
      }
    }

    private IFeatureAction[] _configFeatureActions = { new PanToFeatureAction(), new HighlightFeatureAction() { UpdateExtent = UpdateExtentType.Pan }, new ZoomToFeatureAction(), new SearchNearbyFeatureAction() };
    private IEnumerable<IFeatureAction> _selectedFeatureActions = null;

    public TableWidgetDialog()
      : this(null, null, null)
    {

    }

    public TableWidgetDialog(DataSource dataSource, IEnumerable<IFeatureAction> selectedFeatureActions, string caption)
    {
      InitializeComponent();
      DataSource = dataSource == null ? OperationsDashboard.Instance.DataSources.First() : dataSource;
      SelectedFeatureActions = selectedFeatureActions == null ? AllFeatureActions : selectedFeatureActions;
      Caption = caption;
      FeatureActionList.FeatureActions = AllFeatureActions;
      FeatureActionList.SelectedFeatureActions = SelectedFeatureActions;

    }

    public IEnumerable<IFeatureAction> SelectedFeatureActions
    {
      get { return _selectedFeatureActions; }
      set
      {
        _selectedFeatureActions = value;
      }

    }

    public IEnumerable<IFeatureAction> AllFeatureActions
    {
      get { return new List<IFeatureAction>(_configFeatureActions); }
    }


    private void OnOkButtonClick(object sender, RoutedEventArgs e)
    {
      //DataSource = DataSourceSelector.SelectedDataSource;
      SelectedFeatureActions = FeatureActionList.SelectedFeatureActions;
        //if(SelectedFeatureActions.Any(fa=>fa is SearchNearbyFeatureAction))
        //{
        //    SearchNearbyFeatureAction searchNearbyFeatureAction = SelectedFeatureActions.Where(fa => fa is SearchNearbyFeatureAction).FirstOrDefault() as SearchNearbyFeatureAction;

        //}
            
      Caption = CaptionTextBox.Text;
      DialogResult = true;
    }

    private void OnSelectionChanged(object sender, EventArgs e)
    {
      DataSource = DataSourceSelector.SelectedDataSource;

    }


  }
}
