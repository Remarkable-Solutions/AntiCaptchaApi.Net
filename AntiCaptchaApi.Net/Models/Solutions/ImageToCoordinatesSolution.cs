using System.Collections.Generic;
using System.Linq;

namespace AntiCaptchaApi.Net.Models.Solutions
{
    public class ImageToCoordinatesSolution : BaseSolution
    {
        public IReadOnlyList<IReadOnlyList<int>> Coordinates { get; set; }
    
        public override bool IsValid() =>
            Coordinates.Any();
    }
}