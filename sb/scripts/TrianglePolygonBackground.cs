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

        [Configurable]
        public int ShockwaveStepTime = 100;

        [Configurable]
        public int ShockwaveDelayTime = 50;

        [Configurable]
        public int ShockwavePointX = 320;

        [Configurable]
        public int ShockwavePointY = 240;

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
                        (grid[x,y]).FlipV(StartTime, StartTime+Duration*2); // Need to do the contrast to hug against each triangle
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

        public void ShockwaveColor(float startTime, float stepTime, float delayTime, Vector2 target, CommandColor newColor) {
            // Creates a shockwave color point beginning at startTime.
            // The triangle closest to target becomes the first-point to flash from newColor -> baseColor.
            // Remaining triangles will flash as well after delayTime passes for each subsequent hit.
            // The triangles' order is based on flood-fill from that point. (ie. this is the base method)

            // Initialize the flags array, all as unmarked triangles.
            var flags = new bool [GridWidth, GridHeight];

            // What triangle in the grid would be closest to the target vector's coordinates?
            var closestX = 0;
            var closestY = 0;
            var closestD = float.MaxValue;

            for(int x = 0; x < GridWidth; x++) {
                for(int y = 0; y < GridHeight; y++) {
                    var curD = Distance(grid[x,y].PositionAt(startTime), target);
                    if(curD < closestD) {
                        closestX = x; // New minimum found, so update the high score.
                        closestY = y;
                        closestD = curD;
                    }
                }
            }

            // After this point, we should have found the best point to begin the shockwave. So let's do it!
            var startPoint = new Vector2(closestX, closestY);
            ShockwaveFill(startTime, stepTime, delayTime, startPoint, newColor, startPoint, flags);

        }

        public void ShockwaveFill(float startTime, float stepTime, float delayTime, Vector2 slot, CommandColor newColor, Vector2 startPoint, bool [,] flags) {
            // Flood-fill method. This method actually executes the color command to a single triangle on a shockwave.
            // If a triangle has already been filled, it is ignored. Otherwise, execute the color flash, mark the triangle,
            // and move to its orthogonal neighbors.
            // Also, we have the startPoint, so we can keep track of how far we are from the shockwve.
            // This helps us determine how much delaytime is needed for the flash to occur.
            // To make the distinction that target is pure coordinates and slot is based on array indices, the vector2 is named differently.

            // Base case  
            if (slot.X >= GridWidth ||
                slot.Y >= GridHeight ||
                slot.X < 0 ||
                slot.Y < 0 ||
                flags[(int)slot.X, (int)slot.Y] ) {
                return;
            }

            // We're here, so that means it's time to flash and mark.
            var s = grid[(int)slot.X, (int)slot.Y];
            flags[(int)slot.X, (int)slot.Y] = true;
            var newStartTime = startTime + delayTime*ManhattanDistance(slot, startPoint);
            s.Color(0, newStartTime, newStartTime+stepTime, newColor, s.ColorAt(startTime));

            // Use recursion to flash the other neighbors.
            ShockwaveFill(startTime, stepTime, delayTime, Vector2.Add(slot, new Vector2(0, -1)), newColor, startPoint, flags); // N
            ShockwaveFill(startTime, stepTime, delayTime, Vector2.Add(slot, new Vector2(-1, 0)), newColor, startPoint, flags); // W
            ShockwaveFill(startTime, stepTime, delayTime, Vector2.Add(slot, new Vector2(0, 1)), newColor, startPoint, flags); // S
            ShockwaveFill(startTime, stepTime, delayTime, Vector2.Add(slot, new Vector2(1, 0)), newColor, startPoint, flags); // E

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

        public float ManhattanDistance(Vector2 a, Vector2 b) {
            // Calculates the distance between two vectors through manhattan distance.
            // i.e. tiles, not straightforward "as the crow flies" kind.
            return (float) ( Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) );
         }

        public float Distance(Vector2 a, Vector2 b) {
            // Calculates the distance between two vectors.
            return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        public Vector3 ToHSB(CommandColor c) {
            // Converts RGB color from c to HSB
            // Assumes rgb are 0..1
            // Probably will be obsolete once an actual
            // conversion or data struct is added for HSB

            var R = (float) c.R / 255;
            var G = (float) c.G / 255;
            var B = (float) c.B / 255;

            var cMax = Math.Max(R, G);
            cMax = Math.Max(cMax, B);
            var cMin = Math.Min(R, G);
            cMin = Math.Min(cMin, G);

            var delta = cMax - cMin;

            float h, s, b;

            // huehuehue
            if(delta == 0) {
                h = 0;
            }
            else if (cMax == R) {
                h = 60 * ( ( (G - B) / delta ) % 6);
            }
            else if (cMax == G) {
                h = 60 * ( ( (B - R) / delta ) + 2);
            }
            else {
                h = 60 * ( ( (R - G) / delta ) + 4);
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
            //RotateGrid(StartTime, StartTime+Duration, Angle2Radians(AngleRotation));
            //ScaleGrid(StartTime+Duration, StartTime+Duration*2, (float)0.5);
            //Glitter(StartTime+1, (float)Duration/10, GlitterCount);
            ShockwaveColor(StartTime+1, ShockwaveStepTime, ShockwaveDelayTime, new Vector2( ShockwavePointX, ShockwavePointY ), new CommandColor(255, 0, 0));
            Glitter(StartTime+Duration, (float)Duration/10, GlitterCount);
        }
    }
}
