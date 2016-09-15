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
    ///
    /// One referential note here! It seems that from (-130,50) a whole screen is 20x5 triangles.
    /// So shoot for that.
    ///
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
        public TriangleBehavior BehaviorType = TriangleBehavior.Fallback; 

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

        public delegate void executeSprite(OsbSprite s, float startTime, float stepTime);   // for shockwave
        public delegate bool querySprite(OsbSprite s, float queryTime);                     // wall condition for shockwave
        public delegate void executeBehavior();                                             // For generate()

        #endregion

        #region Privates
        OsbSprite [,] grid;
        private int baseSize = 100;                        // triangle.png's pixel size (for ref)
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
                        (grid[x,y]).FlipV(StartTime, StartTime+Duration); // Need to do the contrast to hug against each triangle TODO: make this end @ correct endtime
                    }
                    //(grid[x,y]).ColorHsb(StartTime, Random(180,240), sat, Random(1,10) * 0.1);
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
            // TODO: Add param for glitter colors.
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

        public void ShockwaveColor(float startTime, float stepTime, float delayTime, Vector2 target, CommandColor newColor, CommandColor ignoreColor, bool executeAsLoop) {
            // Creates a shockwave color point beginning at startTime.
            // The triangle closest to target becomes the first-point to flash from newColor -> baseColor.
            // Remaining triangles will flash as well after delayTime passes for each subsequent hit.
            // The triangles' order is based on the manhattan distance.
            // Also, we can set an ignoreColor that acts as a hard wall that the shockwave can't penetrate through.
            // We can also execute it as a loop, where it will execute the alt. shockcolor method. (but this is hacky, don't do it other than darenimo3)

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

            executeSprite shockColor = delegate(OsbSprite s, float st, float step) { s.Color(0, st, st+step, newColor, s.ColorAt(st)); } ;
            executeSprite shockColorLoop = delegate(OsbSprite s, float st, float step) {    s.StartLoopGroup(st, 4); 
                                                                                s.Color(OsbEasing.InBack, 0, step, newColor, s.ColorAt(st));
                                                                                s.Fade(2400,1);
                                                                                s.EndGroup(); };
            querySprite shockWall = delegate(OsbSprite s, float st) { return s.ColorAt(st) == ignoreColor; } ;

            // After this point, we should have found the best point to begin the shockwave. So let's do it!
            var startPoint = new Vector2(closestX, closestY);
            ShockwaveFill(startTime, stepTime, delayTime, startPoint, startPoint, flags, ( executeAsLoop ? shockColorLoop : shockColor), shockWall);

        }

        public void ShockwaveFill(float startTime, float stepTime, float delayTime, Vector2 slot, Vector2 startPoint, bool [,] flags, executeSprite Go, querySprite Wall) {
            // Flood-fill method. This method actually executes the color command to a single triangle on a shockwave.
            // If a triangle has already been filled, it is ignored. Otherwise, execute the Go method, mark the triangle,
            // and move to its orthogonal neighbors.
            // Also, we have the startPoint, so we can keep track of how far we are from the shockwave.
            // This helps us determine how much delaytime is needed for the flash to occur.
            // To make the distinction that target is pure coordinates and slot is based on array indices, the vector2 is named differently.

            // Base case  
            if (slot.X >= GridWidth ||
                slot.Y >= GridHeight ||
                slot.X < 0 ||
                slot.Y < 0 ||
                flags[(int)slot.X, (int)slot.Y] ||
                Wall(grid[(int)slot.X,(int)slot.Y],startTime)) {
                return;
            }

            // We're here, so that means it's time to flash and mark.
            var s = grid[(int)slot.X, (int)slot.Y];
            flags[(int)slot.X, (int)slot.Y] = true;
            var newStartTime = startTime + delayTime*ManhattanDistance(slot, startPoint);
            Go(s, newStartTime, stepTime); // Change me to executing the delegate

            // Use recursion to flash the other neighbors.
            ShockwaveFill(startTime, stepTime, delayTime, Vector2.Add(slot, new Vector2(0, -1)), startPoint, flags, Go, Wall); // N
            ShockwaveFill(startTime, stepTime, delayTime, Vector2.Add(slot, new Vector2(-1, 0)), startPoint, flags, Go, Wall); // W
            ShockwaveFill(startTime, stepTime, delayTime, Vector2.Add(slot, new Vector2(0, 1)), startPoint, flags, Go, Wall); // S
            ShockwaveFill(startTime, stepTime, delayTime, Vector2.Add(slot, new Vector2(1, 0)), startPoint, flags, Go, Wall); // E

        }

        #endregion

        #region Entry/Exit Methods
        public void Fade(int startTime, int duration, bool isExit) {
            // Have these triangles fade in/out.
            // Entry of exit depending on the boolean entered. (Entry=0, Exit=1)
            for(int x = 0; x < GridWidth; x++) {
                for(int y = 0; y < GridHeight; y++) {
                    var s = grid[x,y];
                    if(isExit) {
                        s.Fade(0, startTime, startTime+duration, s.OpacityAt(startTime), 0);
                    }
                    else {
                        s.Fade(0, startTime, startTime+duration, 0, 1);
                    }
                }
            }
        }

        public void ScaleXYFlip(int startTime, int duration, bool isExit, bool isY) {
            // Those triangles can enter together! Altogether. :) Individually.
            // Entry or exit depending on the boolean entered. (Entry=0, Exit=1)
            // X or Y depending on the boolean enetered. (x=0, y=1)
            for(int x = 0; x < GridWidth; x++) {
                for(int y = 0; y < GridHeight; y++) {
                    var s = grid[x,y];
                    var initialSize = (float) TriangleSize / baseSize;
                    var initialSizeVector = new Vector2 (initialSize, initialSize);
                    var flipScale = (isY) ? new Vector2(initialSize, 0) : new Vector2(0, initialSize);
                    if (isExit) {
                        s.ScaleVec(0, startTime, startTime+duration, initialSizeVector, flipScale);
                    }
                    else {
                        s.ScaleVec(0, startTime, startTime+duration, flipScale, initialSizeVector);
                        }
                }
            }
        }
        #endregion

        #region Color Methods
        public void ColorTriangle(int startTime, int duration, Vector2 coord, CommandColor newColor) {
            // Colors a single triangle at startTime for duration.
            // Select the triangle using the coord command.
            // It'll change to newColor.
            // To instantly change color, just set duration to 0.

            // Don't color if it's OOB.
            if(coord.X >= GridWidth ||
               coord.X < 0 ||
               coord.Y >= GridHeight ||
               coord.Y < 0) {
                   return;
               }

            // But you made it here, so let's color.
            var s = grid[(int)coord.X, (int)coord.Y];
            s.Color(0, startTime, startTime+duration, s.ColorAt(startTime), newColor);
        }

        public void ColorAllTriangles(int startTime, int duration, CommandColor newColor) {
            // Colors all the triangles as... THE SAME COLOR.
            // To instantly change color, just set duration to 0.
            foreach(var s in grid) {
                s.Color(0, startTime, startTime+duration, s.ColorAt(startTime), newColor);
            }
        }

        public void ColorRowCol(int startTime, int duration, int coord, CommandColor newColor, bool isCol) {
            // Colors a row or column.
            // Select the row/col to fill with coord to newColor.
            // isCol bool: row=0, col=1
            
            // OOB check
            if(coord < 0 ||
               coord > ( (isCol) ? GridHeight : GridWidth) ) { return; }

            // Now get ready to fill that baby in.
            for(var b = 0; b < ( (isCol) ? GridWidth : GridHeight ); b++) {
                var s = (isCol) ? grid[b,coord] : grid[coord,b];
                s.Color(0, startTime, startTime+duration, s.ColorAt(startTime), newColor);
            }
            
        }

        public void ColorLinearGradient(int startTime, int duration, CommandColor colorA, CommandColor colorB, bool isVertical) {
            // Colors triangles in a gradient from colorA to colorB.
            // Decide whether to make the gradient transition vertically or horizontally
            // using the Boolean. (horizontal=0, vertical=1)

            var difference = new Vector3( ( colorB.R - colorA.R ),
                                          ( colorB.G - colorA.G ),
                                          ( colorB.B - colorA.B ) ) ; // The difference between colorA and colorB

            var colorVector = new Vector3( colorA.R, colorA.G, colorA.B );   // Ideally the goal is a linear dist from this to colorB
            
            var limiter = (isVertical) ? GridWidth : GridHeight; // How many points to split for the linear distribution
            
            var step = Vector3.Divide(difference, limiter);
            // var step = new Vector3( difference.X / limiter, difference.Y / limiter, difference.Z / limiter ); // The step...
            
            // Now comes the loop:
            for(int a = 0; a < limiter; a++) {
                var decColorVector = Vector3.Divide(colorVector, 255);
                ColorRowCol(startTime, duration, a, new CommandColor(decColorVector), !isVertical);
                colorVector += step;
            }
        }

        public void ColorTopography(int startTime, int duration, CommandColor colorA, CommandColor colorB, int steps, Vector2 [] hotspot) {
            // Colors the hotspot areas with colorA,
            // and for every tile that isn't in the hotspot array,
            // color them towards colorB depending on the minimum Manhattan Distance
            // (i.e. the closest point)
            // Set duration to 0 for instant colorization
            // Ignore entries that are OOB. (Because they aren't in the grid when we are iterating anyway!)
            // However, we can do cool things like have partial colorations, so all is not lost.

            var difference = new Vector3( ( colorB.R - colorA.R ),
                                          ( colorB.G - colorA.G ),
                                          ( colorB.B - colorA.B ) ) ;   // The difference between colorA and colorB
            var step = Vector3.Divide(difference, steps);               // Gives us the linear distribution per step   

            for(int x = 0; x < GridWidth; x++) {
                for(int y = 0; y < GridHeight; y++) {
                    var curSpot = new Vector2(x,y);
                    if(Array.IndexOf(hotspot, curSpot) != -1) {
                        // Is found in the hotspot array
                        // So that means we color that with colorA!
                        ColorTriangle(startTime, duration, curSpot, colorA);
                    }
                    else {
                        // So we could not find it. Therefore, it'll be in some limbo or to colorB.
                        // Find the difference and have a vector that represents one step (similar to gradient)
                        // So first we need to figure out the minimum manhattan distance.
                        int minManhattan = int.MaxValue;

                        foreach(var point in hotspot) {
                            var candidate = ManhattanDistance(curSpot, point);
                            if(candidate < minManhattan) {
                                minManhattan = (int) candidate;
                            }
                        }

                        // Next up, calculate the color based on how many steps away...
                        var limboColor = new Vector3();
                        if(minManhattan >= steps) {
                            limboColor = new Vector3( colorB.R, colorB.G, colorB.B ); // so far that it's pretty much colorB
                        }
                        else {
                            limboColor = new Vector3(colorA.R, colorA.G, colorA.B) + Vector3.Multiply(step, minManhattan);
                        }

                        limboColor = Vector3.Divide(limboColor, 255);

                        // Finally, we can issue the color.
                        ColorTriangle(startTime, duration, curSpot, new CommandColor(limboColor)); 
                    }
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

        #region Implementations

        public enum TriangleBehavior {Fallback, TestGradient, VerseBackground, GuitarSolo, DareNiMo3};
        
        public void Fallback() {
            // This is where the initial generate code went.
            ScaleXYFlip(StartTime, 500, false, true);
            ColorLinearGradient(StartTime, 1, new CommandColor(0.5,0.6,0.1), new CommandColor(1.0, 0.0, 0), true);
            ColorRowCol(StartTime+2,1, GridHeight/2, new CommandColor(0.5, 0.5, 0.5), false);
            ColorRowCol(StartTime+2,1, (int) (GridHeight/1.3), new CommandColor(0.5, 0.5, 0.5), true);            
            //ColorLinearGradient(StartTime, 1, new CommandColor(0.1,0.1,0.1), new CommandColor(0.4, 0.9, 1.0), true);
            ColorLinearGradient(StartTime+Duration/2, 1000, new CommandColor(0.5,0.6,0.1), new CommandColor(0.1, 0.3, 0.5), false);
            RotateGrid(StartTime+500, StartTime+Duration+500, Angle2Radians(AngleRotation));
            //ScaleGrid(StartTime+Duration, StartTime+Duration*2, (float)0.5);
            //Glitter(StartTime+1, (float)Duration/10, GlitterCount);
            ShockwaveColor(StartTime+500, ShockwaveStepTime, ShockwaveDelayTime, new Vector2( ShockwavePointX, ShockwavePointY ), new CommandColor(1.0, 1.0, 1.0), new CommandColor(0.5, 0.5, 0.5), false);
            //Glitter(StartTime+Duration, (float)Duration/10, GlitterCount);
            ScaleXYFlip(StartTime+Duration+500, 500, true, false);
        }

        public void TestGradient() {
            // A gradient test. Linear (close to) black to white.
            ScaleXYFlip(StartTime, 100, false, true);
            ColorLinearGradient(StartTime,0, new CommandColor(0.1, 0.1, 0.1), new CommandColor(1.0, 1.0, 1.0), true);
            Fade(StartTime+Duration,StartTime+Duration+500,true);
        }

        public void VerseBackground() {
            // Goes for any of the verse sections.
            // Expects a triangle grid the size of... 40x10.
            // There are 8 measures of the verse, so every other measure we change colors.
            // Color change is random.
            // In the meantime, maybe a light light glitter.

            // Coolors are COOOOOL.
            var colorList = new CommandColor[5] {   new CommandColor(39.0/255,40.0/255,56.0/255),
                                                    new CommandColor(93.0/255,83.0/255,107.0/255),
                                                    new CommandColor(125.0/255,107.0/255,145.0/255),
                                                    new CommandColor(165.0/255,148.0/255,249.0/255),
                                                    new CommandColor(52.0/255,127.0/255,196.0/255)};

            var hotSpots = new List<Vector2>();
            
            // Time to assign the hotspots that will actually be killed off.
            for(int i = GridWidth/2 - GridWidth/10; i < (GridWidth/2+GridWidth/10) - 1; i++) {
                for(int j = GridHeight/5; j < (GridHeight/5)*3; j++) {
                    hotSpots.Add(new Vector2(i-2,j));
                }
            }

            var colorMarker = Random(0,4);
            var topographicPoints = 8;
            var greyMarker = new Vector3((float)0.4,(float)0.4,(float)0.4);

            // Shockwave to kill off the hotspots.
            executeSprite killMe = delegate(OsbSprite s, float st, float d) { s.ScaleVec(OsbEasing.OutBounce, st, st+d, s.ScaleAt(st), new CommandScale(0,(float) TriangleSize / baseSize)); };
            querySprite isntHotSpot = delegate(OsbSprite s, float st) { return s.ColorAt(st) != colorList[colorMarker]; };

            // OK so let's make the initial topography.
            ColorTopography(StartTime, 0, colorList[colorMarker], new CommandColor(greyMarker), topographicPoints, hotSpots.ToArray());
            Glitter(StartTime+1,600,3);
            ScaleXYFlip(StartTime,600,false,false);

            // Till death do you part.
            // TODO: Maybe get this to work or something.
            ShockwaveFill(StartTime+600,600,75,hotSpots[0],new Vector2(320,240),new bool[GridWidth,GridHeight], killMe, isntHotSpot);

            // Assign different colors.
            for(int i = 1; i < 4; i++) {
                // Update stuff
                colorMarker = ( colorMarker + Random(1,4) ) % 5;
                greyMarker -= new Vector3((float)0.1,(float)0.1,(float)0.1);
                topographicPoints++;
                // And execute!
                ColorTopography(StartTime + (Duration/4)*i-600, 600, colorList[colorMarker], new CommandColor(greyMarker), topographicPoints, hotSpots.ToArray());
                Glitter(StartTime + (Duration/4)*i, 600, 3);
            }


            // Bye bye.
            ScaleXYFlip(StartTime+Duration-600,600,true,false);

            
        }
        public void GuitarSolo() {
            // This is the triangle behavior for the guitar solo portion.
            // The idea is to colorize the entire triangle area as motherf'in evil, and then the ones that aren't go through some cool flip effects.
            var bigPoints = new Vector2[16] {   new Vector2 (8,0),
                                                new Vector2 (9,0),
                                                new Vector2 (10,0),
                                                new Vector2 (7,1),
                                                new Vector2 (11,1),
                                                new Vector2 (4,2),
                                                new Vector2 (5,2),
                                                new Vector2 (6,2),
                                                new Vector2 (12,2),
                                                new Vector2 (13,2),
                                                new Vector2 (14,2),
                                                new Vector2 (5,3),
                                                new Vector2 (9,3),
                                                new Vector2 (13,3),
                                                new Vector2 (4,4),
                                                new Vector2 (14,4)
                                            };

            // Now let's create a topography color based off that.
            CommandColor hotColor = new CommandColor(0.8,0.1,0.1);
            CommandColor coldColor = new CommandColor(0.1,0.1,0.1);
            ColorTopography(StartTime, 0, hotColor, coldColor, 3, bigPoints);

            var beatDuration = 4*(60000)/Beatmap.GetTimingPointAt((int)StartTime).Bpm;

            // The goal is to have the central non-hotspot flip on and off with backgrounds.
            executeSprite flipOff = delegate(OsbSprite s, float startTime, float duration) { s.StartLoopGroup(startTime, 4); 
                                                                                                s.ScaleVec(OsbEasing.OutCubic, 0, duration, new CommandScale(1,1), new CommandScale(1,0));
                                                                                                s.ScaleVec(OsbEasing.InCubic, beatDuration-duration, beatDuration, new CommandScale(1,0), new CommandScale(1,1));
                                                                                                s.Fade(beatDuration*2,1); 
                                                                                             s.EndGroup(); };
                                                                                    
            executeSprite shockColor = delegate(OsbSprite s, float st, float step) {    s.StartLoopGroup(st, 8); 
                                                                                            s.Color(OsbEasing.OutQuint, 0, step, new CommandColor(1,0,0), s.ColorAt(st));
                                                                                            s.Fade(beatDuration,1);
                                                                                        s.EndGroup(); };

            // Query for the wall being the hotColor
            querySprite colorWall = delegate(OsbSprite s, float queryTime) { return s.ColorAt(queryTime) == hotColor; };

            // Wonder if this works. Let's try having a loop with the shockwaves.
            ShockwaveFill(StartTime, 600, 150, bigPoints[12] - new Vector2(0,1), bigPoints[12] , new bool[GridWidth, GridHeight], flipOff, colorWall );

            // Left/right shockwave flashes?
            ShockwaveFill(StartTime, 300, 75/2, new Vector2(0,0), bigPoints[12], new bool[GridWidth, GridHeight], shockColor, colorWall );
            ShockwaveFill(StartTime, 300, 75/2, new Vector2(GridWidth-1,0), bigPoints[12], new bool[GridWidth, GridHeight], shockColor, colorWall );
            

            // HARD VALUES BAD
            ScaleXYFlip(120645-600, 600, true, false);
            
        }

        public void DareNiMo3() {
            // This portion plays during the last dare ni mo part before the DAY SCANNER mania.
            // It expects a BIG-ass set of triangles.

            ColorAllTriangles(StartTime,0,new CommandColor(0.09,0.09,0.09));
            ShockwaveColor(StartTime,300,75/4,new Vector2(320,240),new CommandColor(1,1,1),new CommandColor(1,0,0),true);
            ScaleXYFlip(StartTime+Duration-500,500,true, false);


        }

        #endregion

        public override void Generate()
        {
            // Set up the list of possible behaviors the triangles can do...
            executeBehavior[] implementations = {Fallback, TestGradient, VerseBackground, GuitarSolo, DareNiMo3};
            
            // And go!
            InitializeGrid();
            implementations[(int)BehaviorType]();

        }
    }
}
