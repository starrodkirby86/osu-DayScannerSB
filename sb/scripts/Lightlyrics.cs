using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Mapset;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Util;
using StorybrewCommon.Subtitles;
using StorybrewCommon.Mapset;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;

namespace StorybrewScripts
{
    public class Lightlyrics : StoryboardObjectGenerator
    {
        public override void Generate()
        {
		  var bgLayer = GetLayer("Background");  	
          
          
          var startTime = 43846;
          var beat = 2400;
          var dare = startTime;
          var x = 30;
          var y = Random(280,320);
          
          int[] line1 = new int[9] {43546, 45656, 46246, 48046,48346,48646, 50446, 50746, 51046}; 
           
            var beep = 1;
               foreach (var a in line1){
              
            var light = bgLayer.CreateSprite("SB/"+beep+".png", OsbOrigin.Centre);
            var dot = bgLayer.CreateSprite("SB/blurrydot.png", OsbOrigin.Centre);
          /*  if (a == line1[0])
            { 
                
            }   */
            light.Move(OsbEasing.In,a, a+beat, x, y, x, y+100 );
            light.Scale(a, a+beat, 0.12,0);
            light.Fade(a, a+beat/4, 0,1);
            light.Fade(a+beat/4, a+beat/2, 1,1);
             light.Fade(a+beat/2, a+beat,1,0);
           light.Additive(a, a+beat);
            light.Color(a,a+beat, 0.5,0.1,0.3,1,1,1);
            dot.Move(OsbEasing.In,a, a+beat, x, y, x, y+100 );
            dot.Scale(a, a+beat, 0.5,0.05);
            dot.Fade(a, a+beat/4, 0,1);
            dot.Fade(a+beat/4, a+beat/2, 1,1);
            dot.Fade(a+beat/2, a+beat,1,0);
            dot.Additive(a, a+beat);
            dot.Color(a,a+beat, 0.8,0.8,0,1,1,1);
            x=x+80;
            y = Random(280,300);
            beep++;
           
                 
          }
          
        }
    }
}
