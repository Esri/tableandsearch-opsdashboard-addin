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

using ESRI.ArcGIS.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperationsDashboardAddIns.Helpers
{
    internal class SelectedFeatures
    {
        private IList<ESRI.ArcGIS.Client.Field> _fields;

        private IEnumerable<Graphic> _selectedGraphics;

        private string _name;

        public IList<ESRI.ArcGIS.Client.Field> Fields { get { return _fields; } }

        public IEnumerable<Graphic> SelectedGraphics { get { return _selectedGraphics; } }

        public string Name { get { return _name; } }

        public SelectedFeatures(string name, IList<ESRI.ArcGIS.Client.Field> fields, IEnumerable<Graphic> selectedGraphics)
        {
            _fields = fields;
            _name = name;
            _selectedGraphics = selectedGraphics;
        }
    }
}
