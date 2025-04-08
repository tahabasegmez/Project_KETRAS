using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapsui.UI.Wpf;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Tiling;
using Mapsui.Styles;
using Mapsui.Rendering;

namespace SARTEK_WPF.ViewModels.Mapsui
{

    public partial class BaseMapViewModel
    {
        public Map Map
        {
            get; private set;
        }

        public BaseMapViewModel()
        {
            Map = new Map();
            Map.Layers.Add(OpenStreetMap.CreateTileLayer()); // OpenStreetMap katmanı
            Map.Navigator.CenterOnAndZoomTo(new MPoint(4.895168, 52.370216), 10); // Amsterdam başlangıç noktası
        }
    }
}
