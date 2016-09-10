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
    public class Foo : StoryboardObjectGenerator
    {
        [Configurable]
        public string SpritePath = "SB/yui.png";

        [Configurable]
        public int StartTime = 10000;

        [Configurable]
        public int EndTime = 20000;

        public override void Generate()
        {
            var layer = GetLayer("foo");
            var spr = layer.CreateSprite(SpritePath, OsbOrigin.Centre);
            spr.Scale(StartTime, EndTime, 1.0, 5.0);
            spr.MoveX(StartTime, EndTime, 50, 400);
            spr.MoveY(StartTime, EndTime - 5000, 240, 250);
            spr.MoveY(EndTime - 5000, EndTime, 250, 480);

            // var newSpriteTest = new Baz();
            // newSpriteTest.Generate(); // The error occurs here. A Storyboard Object can't call another storyboard object. (newSpriteTest is null)
        }
    }

    public class Baz : StoryboardObjectGenerator {

        public override void Generate() {
            var layer = GetLayer("foo");
            var spr = layer.CreateSprite("SB/yui.png", OsbOrigin.Centre);
            spr.Scale(0, 50000, 1.0, 5.0);
        }

    }
}
