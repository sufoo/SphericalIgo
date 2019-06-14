using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace SphericalIgo
{
    interface IConsole
    {
        void SetOnConfigurationsChanged(EventHandler handle);
        void SetGameRule(GameRule _rule);
        void UpdateDispCoordinate();
        void UpdateDispScore();
        OperationState GetOperationState();
    }
}
