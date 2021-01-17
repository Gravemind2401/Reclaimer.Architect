using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Controls
{
    [Flags]
    public enum ManipulationFlags
    {
        None = 0,

        TranslateX = 1,
        TranslateY = 2,
        TranslateZ = 4,
        RotateX = 8,
        RotateY = 16,
        RotateZ = 32,
        ScaleX = 64,
        ScaleY = 128,
        ScaleZ = 256,

        TranslateXY = TranslateX | TranslateY,
        TranslateXZ = TranslateX | TranslateZ,
        TranslateYZ = TranslateY | TranslateZ,
        Translate = TranslateX | TranslateY | TranslateZ,

        RotateXY = RotateX | RotateY,
        RotateXZ = RotateX | RotateZ,
        RotateYZ = RotateY | RotateZ,
        Rotate = RotateX | RotateY | RotateZ,

        ScaleXY = ScaleX | ScaleY,
        ScaleXZ = ScaleX | ScaleZ,
        ScaleYZ = ScaleY | ScaleZ,
        Scale = ScaleX | ScaleY | ScaleZ,

        Default = Translate | Rotate | Scale
    }
}
