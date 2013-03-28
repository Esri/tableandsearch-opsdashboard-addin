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

using System.ComponentModel;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Controls;
using ESRI.ArcGIS.OperationsDashboard;
using ESRI.ArcGIS.Client.Tasks;
using ESRI.ArcGIS.Client;

namespace OperationsDashboardAddIns.Config
{
    /// <summary>
    /// Configuration dialog for the search nearby tool
    /// </summary>
    public partial class SearchNearbyFeatureActionDialog : Window, IDataErrorInfo
    {
        private bool dataNoError = true;
        private int _distance = 1;
        private List<LinearUnit> _units;
        private LinearUnit _selectedUnit;

        #region Properties
        //id of the data source from which the nearby features are to be selected
        public string TargetDataSourceId { get; private set; }
        //list of all the data sources which are selectable
        public IEnumerable<string> SelectableDataSourceNames { get; private set; }

        //buffer radius
        public int Distance
        {
            get { return _distance; }
            set
            { _distance = value; }
        }

        //list of radius units
        public List<LinearUnit> Units
        {
            get { return _units; }
            set { _units = value; }
        }

        //selected buffer radius unit
        public LinearUnit SelectedUnit
        {
            get { return _selectedUnit; }
            set { _selectedUnit = value; }
        }
        #endregion

        #region Constructors
        public SearchNearbyFeatureActionDialog()
        {
            InitializeComponent();

            base.DataContext = this;
        }

        public SearchNearbyFeatureActionDialog(string targetDataSourceId, int BufferDistance, LinearUnit BufferUnit)
            : this()
        {
            //Set the available data sources and the selected data source
            SelectableDataSourceNames = GetSelectableMapDataSources();
            if (SelectableDataSourceNames != null && SelectableDataSourceNames.Count() > 0)
                TargetDataSourceId = string.IsNullOrEmpty(targetDataSourceId) ? OperationsDashboard.Instance.DataSources.Where(d => d.IsSelectable).FirstOrDefault().Id : targetDataSourceId;
            string selectedName = string.Empty;
            foreach (ESRI.ArcGIS.OperationsDashboard.DataSource ds in OperationsDashboard.Instance.DataSources)
            {
                if (ds.Id == TargetDataSourceId)
                    selectedName = ds.Name;
            }

            if (!string.IsNullOrEmpty(selectedName))
                cmbLayer.SelectedValue = selectedName; //set the selected value in the layer combo to the selectedname

            //Set buffer distance
            if (BufferDistance > 0)
                Distance = BufferDistance;

            //Set the available units 
            List<LinearUnit> units = new List<LinearUnit>() { LinearUnit.Kilometer, LinearUnit.Meter, LinearUnit.SurveyMile, LinearUnit.SurveyYard };
            Units = units;

            //Set the unit of the buffer radius
            if (BufferUnit == 0)
                SelectedUnit = Units[0];
            else
                SelectedUnit = BufferUnit;
        }
        #endregion

        #region Validate distance text box input

        public string Error
        {
            get { return ""; }
        }

        public string this[string columnName]
        {
            get
            {
                string result = null;
                double pDistance;
                if (columnName == "Distance")
                {
                    if (!double.TryParse(Distance.ToString(), out pDistance) || Distance <= 0)
                        result = "Distance is not > 0";
                }
                return result;
            }
        }

        /// <summary>
        /// Handle when data in the distance text box has error 
        /// </summary>
        private void txtDistance_DataError(object sender, System.Windows.Controls.ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
                dataNoError = false;
            else
                dataNoError = true;
        }
        #endregion

        #region OK button command
        /// <summary>
        /// Check if the OK button's command can be executed
        /// </summary>
        private void OK_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = dataNoError;
        }

        /// <summary>
        /// Handle when OK button's command has been executed
        /// </summary>
        private void OK_HasExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            DialogResult = true;
        }
        #endregion

        private List<string> GetSelectableMapDataSources()
        {
            List<string> selectableLayerNames = new List<string>();

            IEnumerable<IWidget> mapWidgets = OperationsDashboard.Instance.Widgets.Where(w => w is MapWidget);
            if (mapWidgets != null && mapWidgets.Count() > 0)
            {
                foreach (ESRI.ArcGIS.OperationsDashboard.DataSource dataSource in OperationsDashboard.Instance.DataSources)
                {
                    if (dataSource.IsSelectable)
                        if (!selectableLayerNames.Contains(getDataSourceName(dataSource.Name)))
                            selectableLayerNames.Add(getDataSourceName(dataSource.Name));
                }
            }

            return selectableLayerNames;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TargetDataSourceId = OperationsDashboard.Instance.DataSources.Where(d => string.Compare(d.Name, cmbLayer.SelectedValue.ToString().Trim()) == 0).FirstOrDefault().Id;
        }

        private string getDataSourceName(string selectionName)
        {
            return selectionName.Substring(0, selectionName.IndexOf(" Selection"));
        }
    }
}
