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
    public class Baz : StoryboardObjectGenerator
    {
        [Configurable]
        public int StartTime = 5000;
        [Configurable]
        public int Duration = 1000;
        [Configurable]
        public string SpritePath = "SB/yui.png";

        private Qux nico; 
        public override void Generate()
        {
		    var bg = GetLayer("Wow");	
            var sprite = bg.CreateSprite(SpritePath, OsbOrigin.Centre, new Vector2( 320, 240) ) ;

            nico = new Qux(sprite);

            for(int i = 0; i < Duration*10; i += Duration) {
                nico.Dance(StartTime+i, Duration);
            }

            
        }
    }

    public class Qux {
        // Testing.
        public OsbSprite quux;

        public Qux(OsbSprite s) {
            quux = s;
        }

        public void Dance(int s, int d) {
            // DO A LITTLE DANCE
            // MAKE A LITTLE LOVE
            // GET DOWN TONIGHT
            quux.Scale(OsbEasing.OutBounce, s, s+d, 1, 1.5);
        }

    }
}
