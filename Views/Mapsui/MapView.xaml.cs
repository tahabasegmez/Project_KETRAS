using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Mapsui.UI.Wpf;

namespace SARTEK_WPF.Views.Mapsui;
/// <summary>
/// Interaction logic for MapView.xaml
/// </summary>
public partial class MapView : UserControl
{
    public MapView()
    {
        InitializeComponent();
    }

    public MapControl mapControl => this.MapControl; // MapControl'ü dışarıdan erişilebilir yapıyoruz
}
