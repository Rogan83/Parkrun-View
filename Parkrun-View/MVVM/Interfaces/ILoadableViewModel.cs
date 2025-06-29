using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parkrun_View.MVVM.Interfaces
{
    public interface ILoadableViewModel
    {
        bool IsLoading { get; set; } // Wenn true, dann wird der Ladespinner angezeigt, wenn die Seite verlassen wird
    }
}
