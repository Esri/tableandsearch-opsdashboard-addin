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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client;
using System.Windows.Data;

namespace OperationsDashboardAddIns
{
  public enum FeatureActionType
  {
    Highlight,
    ZoomTo,
    PanTo,
    SearchNearby
  }

  [Export("ESRI.ArcGIS.OperationsDashboard.Widget")]
  [ExportMetadata("DisplayName", "Table Widget Sample")]
  [ExportMetadata("Description", "Table Widget Sample")]
  [ExportMetadata("ImagePath", "/OperationsDashboardAddIns;component/Images/table_widget.png")]
  [ExportMetadata("DataSourceRequired", true)]
  [DataContract]
  public partial class TableWidget : UserControl, IWidget, IDataSourceConsumer
  {


    #region Properties

    [DataMember(Name = "dataSourceId")]
    public string DataSourceId { get; set; }


    [DataMember(Name = "field")]
    public string Field { get; set; }

    //data source configured for using data grid
    public ESRI.ArcGIS.OperationsDashboard.DataSource DataSource { get; private set; }
    //set of feature actions selected in configuration
    public IEnumerable<IFeatureAction> FeatureActions { get; private set; }

    //serialize the feature actions which are configured 
    [DataMember(Name = "featureActions")]
    public FeatureActionType[] PersistedFeatureActions { get; set; }

    //serialize the highlight update type to be used by the highlight feature action
    [DataMember(Name = "highlightUpdateType")]
    public UpdateExtentType HighlightUpdateType { get; set; }

    [DataMember(Name = "bufferDistance")]
    public int BufferDistance { get; set; }

    [DataMember(Name = "BufferDistanceUnit")]
    public client.Tasks.LinearUnit BufferUnit { get; set; }

    [DataMember(Name = "TargetDataSourceId")]
    public string TargetDataSourceId { get; set; }

    #endregion

    public TableWidget()
    {
      InitializeComponent();
      this.FeatureGrid.FontSize = 14;
    }



    #region IWidget Members

    private string _caption = "New Table Widget";

    [DataMember(Name = "caption")]
    public string Caption
    {
      get
      {
        return _caption;
      }

      set
      {
        if (value != _caption)
        {
          _caption = value;
        }
      }
    }


    [DataMember(Name = "id")]
    public string Id { get; set; }


    public void OnActivated()
    {
      //listen to text size mode changed event and responsd to the same in your widget
      OperationsDashboard.Instance.TextSizeModeChanged += (e, a) =>
      {
        //here we have hard coded the font size. However you can also apply style
        //to the control that derives from the styles defined for ops dashboard
        //see http://resources.arcgis.com/en/help/runtime-wpf/concepts/index.html#/Application_resource_reference/0170000000mt000000/

        switch (OperationsDashboard.Instance.TextSizeMode)
        {
          case TextSizeMode.Large:
            this.FeatureGrid.FontSize = 18;
            break;
          case TextSizeMode.Medium:
            this.FeatureGrid.FontSize = 16;
            break;
          case TextSizeMode.Small:
            this.FeatureGrid.FontSize = 14;
            break;

        }
      };

      //check if there is a persisted information about the feature actions
      InitializeFeatureActions();
      FeatureActionContextMenu.FeatureActions = FeatureActions;

    }


    public void OnDeactivated()
    {


    }


    public bool CanConfigure
    {
      get { return true; }
    }


    public bool Configure(Window owner, IList<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources)
    {
      // Show the configuration dialog.

      Config.TableWidgetDialog dialog = new Config.TableWidgetDialog(DataSource, FeatureActions, Caption) { Owner = owner };
      if (dialog.ShowDialog() != true)
        return false;
      //set the data source id
      DataSourceId = dialog.DataSource.Id;
      //get the selected feature actions from the configuration dialog
      FeatureActions = dialog.SelectedFeatureActions;
      //point to context menu's feature actions to the selected feature actions
      FeatureActionContextMenu.FeatureActions = FeatureActions;
      Caption = dialog.Caption;
      InitializePersistedFeatureActions();
      return true;

    }

    #endregion

    #region IDataSourceConsumer Members


    public string[] DataSourceIds
    {
      get { return new string[] { DataSourceId }; }
    }


    public void OnRemove(ESRI.ArcGIS.OperationsDashboard.DataSource dataSource)
    {
      // Respond to data source being removed.
      DataSourceId = null;
    }




    #endregion

    #region Private Methods

    private void InitializePersistedFeatureActions()
    {
      PersistedFeatureActions = null;
      HighlightUpdateType = UpdateExtentType.Pan;

      if (FeatureActions == null)
        return;

      List<FeatureActionType> persistedFeatureActions = new List<FeatureActionType>();
      foreach (var featureAction in FeatureActions)
      {
        if (featureAction is HighlightFeatureAction)
        {
          persistedFeatureActions.Add(FeatureActionType.Highlight);

          //persist the UpdateExtent state of the highlight feature action
          HighlightUpdateType = ((HighlightFeatureAction)featureAction).UpdateExtent;
        }
        else if (featureAction is ZoomToFeatureAction)
          persistedFeatureActions.Add(FeatureActionType.ZoomTo);
        else if (featureAction is PanToFeatureAction)
          persistedFeatureActions.Add(FeatureActionType.PanTo);
        else if (featureAction is SearchNearbyFeatureAction)
        {
            persistedFeatureActions.Add(FeatureActionType.SearchNearby);
            BufferDistance = ((SearchNearbyFeatureAction)featureAction).BufferDistance;
            BufferUnit = ((SearchNearbyFeatureAction)featureAction).BufferUnit;
            TargetDataSourceId = ((SearchNearbyFeatureAction)featureAction).TargetDataSourceId;
        }
          
        else
          throw new NotImplementedException(string.Format("Cannot persist feature action of type: {0}", featureAction.GetType().ToString()));
      }

      PersistedFeatureActions = persistedFeatureActions.ToArray();

    }


    private void InitializeFeatureActions()
    {
      FeatureActions = null;
      if (PersistedFeatureActions == null)
        return;

      List<IFeatureAction> featureActions = new List<IFeatureAction>();
      foreach (var persistedFeatureAction in PersistedFeatureActions)
      {
        switch (persistedFeatureAction)
        {
          case FeatureActionType.Highlight:
            featureActions.Add(new HighlightFeatureAction() { UpdateExtent = HighlightUpdateType });
            break;
          case FeatureActionType.PanTo:
            featureActions.Add(new PanToFeatureAction());
            break;
          case FeatureActionType.ZoomTo:
            featureActions.Add(new ZoomToFeatureAction());
            break;
          case FeatureActionType.SearchNearby:
            featureActions.Add(new SearchNearbyFeatureAction(TargetDataSourceId, BufferDistance, BufferUnit));

            break;
          default:
            throw new NotImplementedException(string.Format("Cannot create feature action of type: {0}", persistedFeatureAction.ToString()));
        }
      }
      FeatureActions = featureActions;
    }

    private bool isValidDataType(ESRI.ArcGIS.Client.Field.FieldType fieldType)
    {
      return fieldType == ESRI.ArcGIS.Client.Field.FieldType.Date
                         || fieldType == ESRI.ArcGIS.Client.Field.FieldType.Double
                         || fieldType == ESRI.ArcGIS.Client.Field.FieldType.GUID
                         || fieldType == ESRI.ArcGIS.Client.Field.FieldType.Integer
                         || fieldType == ESRI.ArcGIS.Client.Field.FieldType.OID
                         || fieldType == ESRI.ArcGIS.Client.Field.FieldType.SmallInteger
                         || fieldType == ESRI.ArcGIS.Client.Field.FieldType.String;
    }
    //async method for querying the data source and populating the collectionview source with the results


    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      //set the feature to the selected item
      FeatureActionContextMenu.Feature = (Graphic)FeatureGrid.SelectedItem;
    }

   
    #endregion

    #region Presentation

    //this method is called for data source configured for the widget
    public async void OnRefresh
        (ESRI.ArcGIS.OperationsDashboard.DataSource dataSource)
    {
      if (!String.IsNullOrEmpty(DataSourceId))
      {
        DataSource = dataSource;
        FeatureActionContextMenu.DataSource = DataSource;

        setUpFeatureGrid();
      }
      await PopulateDataGridAsync();
    }

    //list of fields to be shown in the data grid
    public IEnumerable<Field> ValidFields
    {
      get { return DataSource.Fields.Where(f => isValidDataType(f.Type)); }
    }

    //add columns and set up bindings for each column in the data grid
    private void setUpFeatureGrid()
    {
      FeatureGrid.Columns.Clear();

      if (this.DataSource != null)

        foreach (Field field in ValidFields)
          FeatureGrid.Columns.Add(new DataGridTextColumn()
          {
            Binding = new Binding("Attributes [" + field.FieldName + "]"),
            Header = field.Alias
          });

    }

    private async Task PopulateDataGridAsync()
    {
      try
      {
        //create a new query and set the where clause to return all the features
        ESRI.ArcGIS.OperationsDashboard.Query findQuery =
            new ESRI.ArcGIS.OperationsDashboard.Query()
        {
          WhereClause = "1=1",
          ReturnGeometry = true
        };

        //execute the query on the data source configured for this widget and await for it
        QueryResult result = await this.DataSource.ExecuteQueryAsync(findQuery);
        if ((result != null) && (result.Features != null))
          //bind the feature grid to the features
          FeatureGrid.ItemsSource = result.Features;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(ex.Message);
      }

    }



    #endregion
  }
}
