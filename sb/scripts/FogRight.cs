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
    public class FogRight : StoryboardObjectGenerator
    {
        public override void Generate()
        {
		var bgLayer = GetLayer("Background");   
        
        var startTime = 43846;
        var endTime = 53446;	
        var beat = 2400;
        var x = Random(-200, 400);
        var y = 400;
        var x2 = x+Random(100,300);
        var y2 = y;
        var sc = Random(1.00, 2.00);
        
        var fog = bgLayer.CreateSprite("SB/fog.png", OsbOrigin.Centre);
        fog.Move(startTime, endTime, 200, 200, 400,200);
        fog.Scale(startTime, endTime, 1,1 );
        fog.Fade(startTime, startTime+beat, 0,1);
        fog.Fade(startTime+beat, 52846, 1,1);
        fog.Fade(52846, endTime, 1,0 );
        
        
        var light = bgLayer.CreateSprite("SB/blurrydot.png", OsbOrigin.Centre);  	
            
        }
    }
}
