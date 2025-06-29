using Parkrun_View.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parkrun_View.MVVM.Interfaces
{
    public interface IFilterableByParkrunner
    {
        ObservableCollection<ParkrunData> Data { get; set; }
    }
}
