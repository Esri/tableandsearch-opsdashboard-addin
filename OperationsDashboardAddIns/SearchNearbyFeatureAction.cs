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
using System.ComponentModel.Composition;
using System.Windows.Media;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using System.Runtime.Serialization;
using OperationsDashboardAddIns.Config;
using System.Windows.Data;

namespace OperationsDashboardAddIns
{
  /// <summary>
  /// This application queries for the features of a selectable data source 
  /// based on a buffer circle specified by user.
  /// 
  /// To know more, read:
  /// http://resources.arcgis.com/en/help/runtime-wpf/concepts/index.html#/How_to_use_the_Geometry_task/017000000039000000/
  /// http://resources.arcgis.com/en/help/runtime-wpf/concepts/index.html#/Selecting_features_in_a_map_widget/0170000000ns000000/ 
  /// </summary>
  [Export("ESRI.ArcGIS.OperationsDashboard.FeatureAction")]
  [ExportMetadata("DisplayName", "Search Nearby Features")]
  [ExportMetadata("Description", "Search Nearby Features")]
  [ExportMetadata("ImagePath", "/OperationsDashboardAddIns;component/Images/nearby.png")]
  public class SearchNearbyFeatureAction : IFeatureAction
  {
    private client.Tasks.GeometryService _geometryTask;

    #region Properties
    public string TargetDataSourceId { get; set; }
    public int BufferDistance{get; set;}
    public client.Tasks.LinearUnit BufferUnit { get;  set; }
    #endregion

    #region Constructor
    public SearchNearbyFeatureAction():this(null,1,client.Tasks.LinearUnit.Kilometer){}

    public SearchNearbyFeatureAction(string targetDataSourceId, int bufferDistance, client.Tasks.LinearUnit bufferUnit)
    {
      _geometryTask = new client.Tasks.GeometryService(
        "http://tasks.arcgisonline.com/ArcGIS/rest/services/Geometry/GeometryServer");
      _geometryTask.BufferCompleted += GeometryTask_BufferCompleted;
      _geometryTask.Failed += GeometryServiceFailed;
      TargetDataSourceId = targetDataSourceId;
      BufferDistance = bufferDistance;
      BufferUnit = bufferUnit;
    }
    #endregion

    #region IFeatureAction
    public bool CanConfigure
    {
      get { return true; }
    }

    public bool Configure(System.Windows.Window owner)
    {
      //Pass the data source names to the config dialog so users can pick
      SearchNearbyFeatureActionDialog searchDiag = new SearchNearbyFeatureActionDialog(TargetDataSourceId, BufferDistance, BufferUnit);

      //Show the configuration dialog
      if (searchDiag.ShowDialog() != true)
        return false;

      //Retrive the values from the configuration dialog
      TargetDataSourceId = searchDiag.TargetDataSourceId;      

      BufferDistance = searchDiag.Distance;
      BufferUnit = searchDiag.SelectedUnit;

      return (BufferDistance > 0 && TargetDataSourceId != null);
    }

    /// <summary>
    /// This function determines if the feature action will be enabled or disabled
    /// In order for the FeatureAction to execute:
    /// - The map widget that contains the dataSource from which the buffer area is generated must not be null
    /// - The Target DataSource that will be queried must not be null
    /// - The radius (Distance) of the buffer area must be >0 
    /// </summary>
    /// <param name="BufferDS">BufferDS is the data source containing the BufferFeature</param>
    /// <param name="BufferFeature">BufferFeature is the feature used to generate the buffer area</param>
    public bool CanExecute(ESRI.ArcGIS.OperationsDashboard.DataSource BufferDS, client.Graphic BufferFeature)
    {
        return (MapWidget.FindMapWidget(BufferDS) != null && TargetDataSourceId != null && BufferDistance > 0);
    }

    /// <summary>
    /// This function creates the buffer area from user's selected feature
    /// </summary>
    /// <param name="BufferDS">BufferDS is the data source containing the BufferFeature</param>
    /// <param name="BufferFeature">BufferFeature is the feature used to generate the buffer area</param>
    public void Execute(ESRI.ArcGIS.OperationsDashboard.DataSource BufferDS, client.Graphic BufferFeature)
    {
      //Clear any running task
      _geometryTask.CancelAsync();

      //Get the map widget and the map that contains the feature used to generate the buffer
      MapWidget mw = MapWidget.FindMapWidget(BufferDS);
      client.Map mwMap = mw.Map;

      //Define the params to pass to the buffer operation
      client.Tasks.BufferParameters bufferParameters = CreateBufferParameters(BufferFeature, mwMap);

      //Execute the GP tool
      _geometryTask.BufferAsync(bufferParameters);
    }

    #endregion

    #region Create buffer polygon
    async void GeometryTask_BufferCompleted(object sender, client.Tasks.GraphicsEventArgs e)
    {
      //check the result (the buffer polygon)
      //then pass to the query operation
      if (e.Results != null)
      {
        //There will be only one result 
        client.Graphic buffer = e.Results[0];

        //Then query for the features that intersect with the polygon
        await QueryForFeatures(buffer.Geometry);
      }
    }
    #endregion

    #region Query for features within the specified distance that intersect with the buffer
    public async Task QueryForFeatures(client.Geometry.Geometry bufferPolygon)
    {
      //Set up the query and query result
      Query query = new Query("", bufferPolygon, true);
      DataSource ds = OperationsDashboard.Instance.DataSources.Where(d => d.Id == TargetDataSourceId).FirstOrDefault();
      if (ds != null)
      {
          //Run the query and check the result
          QueryResult result = await ds.ExecuteQueryAsync(query);
          if ((result == null) || (result.Canceled) || (result.Features == null) || (result.Features.Count < 1))
              return;

          // Get the array of OIDs from the query results.
          var resultOids = from resultFeature in result.Features select System.Convert.ToInt32(resultFeature.Attributes[ds.ObjectIdFieldName]);

          // Find the map layer in the map widget that contains the target data source.
          MapWidget mapW = MapWidget.FindMapWidget(ds);
          if (mapW != null)
          {
              // Get the feature layer in the map for the data source.
              client.FeatureLayer featureLayer = mapW.FindFeatureLayer(ds);

              // For each graphic feature in featureLayer, use its OID to find the graphic feature from the result set.
              // Note that though the featureLayer's graphic feature and the result set's feature graphic feature share the same properties,
              // they are indeed different objects
              foreach (client.Graphic feature in featureLayer.Graphics)
              {
                  int featureOid;
                  int.TryParse(feature.Attributes[ds.ObjectIdFieldName].ToString(), out featureOid);

                  //If feature has been selected in previous session, unselect it
                  if (feature.Selected)
                      feature.UnSelect();

                  //If the feature is in the query result set, select it
                  if ((resultOids.Contains(featureOid)))
                      feature.Select();
              }
          }

      }
   
    }
    #endregion

    #region helper methods
    private client.Tasks.BufferParameters CreateBufferParameters(client.Graphic feature, client.Map mwMap)
    {
      client.Tasks.BufferParameters bufferParameters = new client.Tasks.BufferParameters()
      {
        Unit = BufferUnit,
        BufferSpatialReference = mwMap.SpatialReference,
        OutSpatialReference = mwMap.SpatialReference,
        UnionResults = true,
      };
      bufferParameters.Distances.AddRange(new List<double> { BufferDistance });
      bufferParameters.Features.AddRange(new List<client.Graphic>() { feature });
      return bufferParameters;
    }

   

    void GeometryServiceFailed(object sender, client.Tasks.TaskFailedEventArgs e)
    {
      MessageBox.Show("fail to calculate buffer, error: " + e.Error);
    }
    #endregion
  }
}
