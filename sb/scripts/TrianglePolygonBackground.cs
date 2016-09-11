using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Util;
using StorybrewCommon.Mapset;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;

namespace StorybrewScripts
{
    /// <summary>
    /// TrianglePolygonBackground.cs
    /// Generates a whole pattern of translucent triangle patterns on the background.
    /// The triangles are customizable. Their coloration changes and there are (ideally) many
    /// effects you can try out. Also transition stuff is available too.
    /// The triangles are also arranged in a manipulatable 2D Matrix that can be
    /// used for coloration purposes.
    /// </summary>    
    public class TrianglePolygonBackground : StoryboardObjectGenerator
    {
        #region Configurables

        [Configurable]
        public int StartTime = 10000;

        [Configurable]
        public int Duration = 5000;

        [Configurable]
        public int GridWidth = 4;

        [Configurable]
        public int GridHeight = 3;

        [Configurable]
        public int InitialLocationX = 320;

        [Configurable]
        public int InitialLocationY = 240;
        
        [Configurable]
        public int TriangleSize = 100;

        [Configurable]
        public string ImagePath = "SB/triangle.png";

        [Configurable]
        public float AngleRotation = 45;

        [Configurable]
        public int GlitterCount = 10;

        #endregion

        #region Privates
        OsbSprite [,] grid;
        private int baseSize = 100; // triangle.png's pixel size (for ref)
        #endregion

        #region Grid Methods
        public void InitializeGrid() {
            // Initialize the grid with blank OSB triangle sprites to handle for later.
            grid = new OsbSprite[GridWidth, GridHeight];
            var layer = GetLayer("TriangleBackground");

            bool isTriangleUpsideDown = false;

            // Populate each item in the 2D Array with new sprites...
            for(int x = 0; x < GridWidth; x++) {
                var sat = 0.001;
                for(int y = 0; y < GridHeight; y++) {
                    var coord = new Vector2(InitialLocationX + x*TriangleSize*0.5F, InitialLocationY + y*TriangleSize);
                    grid[x,y] = layer.CreateSprite(ImagePath, OsbOrigin.Centre,coord);
                    if(isTriangleUpsideDown) {
                        (grid[x,y]).FlipV(StartTime, StartTime+Duration); // Need to do the contrast to hug against each triangle
                    }
                    (grid[x,y]).ColorHsb(StartTime, Random(180,240), sat, Random(1,10) * 0.1);
                    (grid[x,y]).Scale(StartTime, (float) TriangleSize / baseSize);
                    isTriangleUpsideDown = !isTriangleUpsideDown;
                    sat += 0.05;
                }
            }
        }

        public void ScaleGrid(float startTime, float endTime, float scale) {
            // Scales the grid to a certain multiplier.
            // Scaling also requires the whole grid to move as well.
            // BUG: It's hard-coded to 50, or baseSize/2?
            for(int x = 0; x < GridWidth; x++) {
                for(int y = 0; y < GridHeight; y++) {
                    var s = grid[x,y];
                    var startScale = s.ScaleAt(startTime).X;
                    var startVector = s.PositionAt(startTime);
                    s.Scale(0, startTime, endTime, s.ScaleAt(startTime).X, scale); // We're not doing vector scaling so we can pick one
                    s.Move(0, startTime, endTime, startVector, new Vector2(startVector.X*scale/startScale, startVector.Y*scale/startScale) );
                }
            }
        }

        public void RotateGrid(float startTime, float endTime, float angle) {
            // Rotates the grid to a certain angle.
            // At this current point, higher angles do not work well at all (the rate of rotation to the movement
            // is incredibly off), but at smaller moments, it works relatively well
            for(int x = 0; x < GridWidth; x++) {
                for(int y = 0; y < GridHeight; y++) {
                    var s = grid[x,y];
                    s.Rotate(0, startTime, endTime, s.RotationAt(startTime), angle);
                    s.Move(0, startTime, endTime, s.PositionAt(startTime), PolarRotation(s.PositionAt(startTime), angle));
                }
            }
        }

        public void Glitter(float startTime, float duration, int loopCount) {
            // Individual triangles glitter a random color.
            // Original color -> glitter color -> original color
            // This loops for some time, specified by loopCount.
            for(int x = 0; x < GridWidth; x++) {
                for(int y = 0; y < GridHeight; y++) {
                    var s = grid[x,y];
                    OsbEasing randomEnum = (OsbEasing) (Random(0, 32));
                    var baseColor = s.ColorAt(startTime);
                    var baseHSV = ToHSB(baseColor);
                    var glitterColor = new Vector3 (Random(180,240), (float) (Random(40,100) * 0.01), (float) ( Random(1,10) * 0.1) );
                    s.StartLoopGroup(startTime, loopCount);
                        s.ColorHsb(randomEnum, 0, duration/2, baseHSV.X, baseHSV.Y, baseHSV.Z, glitterColor.X, glitterColor.Y, glitterColor.Z);
                        s.ColorHsb(randomEnum, duration/2, duration, glitterColor.X, glitterColor.Y, glitterColor.Z, baseHSV.X, baseHSV.Y, baseHSV.Z);
                    s.EndGroup();
                }
            }
        }

        #endregion

        #region Util Methods
        public Vector2 PolarRotation(Vector2 old, float t) {
            // Note: In rads
            double theta = (double) t;
            return new Vector2((float) ( old.X * Math.Cos(theta) - old.Y * Math.Sin(theta) ),
                               (float) ( old.X * Math.Sin(theta) + old.Y * Math.Cos(theta) ) );
        }

        public float Angle2Radians(float angle) {
            return ((float) Math.PI / 180 * angle);
        }

        public Vector3 ToHSB(CommandColor c) {
            // Converts RGB color from c to HSB
            // Assumes rgb are 0..1
            // Probably will be obsolete once an actual
            // conversion or data struct is added for HSB
            // TODO: Bug. Colors goto 255 instead. Not successful. :(
            var cMax = Math.Max(c.R, c.G);
            cMax = Math.Max(cMax, c.B);
            var cMin = Math.Min(c.R, c.G);
            cMin = Math.Min(cMin, c.G);

            var delta = cMax - cMin;

            float h, s, b;

            // huehuehue
            if(delta == 0) {
                h = 0;
            }
            else if (cMax == c.R) {
                h = 60 * ( ( (c.G - c.B) / delta ) % 6);
            }
            else if (cMax == c.G) {
                h = 60 * ( ( (c.B - c.R) / delta ) + 2);
            }
            else {
                h = 60 * ( ( (c.R - c.G) / delta ) + 4);
            }

            // saturation
            if(cMax == 0) {
                s = 0;
            }
            else {
                s = delta / cMax;
            }

            // value
            b = cMax;

            return new Vector3(h,s,b);

        }
        #endregion

        public override void Generate()
        {
            InitializeGrid();
            RotateGrid(StartTime, StartTime+Duration, Angle2Radians(AngleRotation));
            ScaleGrid(StartTime+Duration, StartTime+Duration*2, (float)0.5);
            //Glitter(StartTime, (float)Duration/10, GlitterCount);
        }
    }
}
