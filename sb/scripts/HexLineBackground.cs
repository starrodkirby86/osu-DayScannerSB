using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Util;
using StorybrewCommon.Mapset;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;

namespace StorybrewScripts
{
    /// <summary>
    /// HexLineBackground.cs
    /// Generates a whole pattern of hexagon patterns on the background through lines.
    /// The lines are customizable. Their coloration changes and there are (ideally) many
    /// effects you can try out. Also transition stuff is available too.
    /// The lines are also arranged in a manipulatable 2D Matrix that can be
    /// used for coloration purposes.
    /// </summary>
    public class HexLineBackground : StoryboardObjectGenerator
    {
        #region Configurables
        [Configurable]
                public string LineImagePath = "SB/line.png";
        #endregion


        #region Hexagon Generation Methods
        public void DrawHexagon(Vector2 centre) {
            // Let's make a hexagon.

        }
        #endregion
        public override void Generate()
        {
		    	
            
        }
    }

    public class Hexagon {
     /// <summary>
     /// Hexagon
     /// A data structure that holds a hexagon shape. The shape itself is basically...
     /// -> The 12 lines of the hexagon   
     
    }
}
