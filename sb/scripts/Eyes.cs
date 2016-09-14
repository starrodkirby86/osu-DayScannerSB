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
    public class Eyes : StoryboardObjectGenerator
    {
        public override void Generate()
        {
		  var bgLayer = GetLayer("Background");    
          var eyes = bgLayer.CreateSprite("SB/eyes.png", OsbOrigin.Centre);	
          var startTime = 43846;
          var middleTime = 48646;
          var endtimefirst = 51046;
         var endTime = 53446;	
         var beat = 2400;
          eyes.Fade(startTime, middleTime, 0,1);
          eyes.Fade(endtimefirst, endTime, 1,0);
          eyes.ScaleVec(OsbEasing.OutCirc,startTime,middleTime, 0.8 ,0, 0.8,0.8);
          eyes.ScaleVec(OsbEasing.InCirc, endtimefirst, endTime, 0.8 ,0.8, 0.8,0);
          eyes.Move(startTime,endTime, 320,200,320,200);
            
        }
    }
}
