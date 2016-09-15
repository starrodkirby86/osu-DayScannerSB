using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Util;
using StorybrewCommon.Mapset;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace StorybrewScripts {
    /// <summary>
    /// ParticleGlider.cs
    /// Randomly gliding particles will spawn and eventually assemble to form
    /// a shape. The shape is based on an imported image.
    /// 
    /// Idea implementation:
    ///     - Load the image.
    ///     - Convert the image into a data structure of coordinates called pixels.
    ///     - Load the appropriate particles necessary to occupy each pixel. Use MVC method.
    ///     - Particle initially spawns and moves wayward in space, random trajectory motion
    ///     - At call-time, all subscribed particles assemble together to form the shape (duration parameter?)
    /// 
    /// </summary>

    public class ParticleGlider : StoryboardObjectGenerator {
        #region Congifurables
        [Configurable]
        public int StartTime = 0;

        [Configurable]
        public int TriggerTime = 5000;

        [Configurable]
        public float GlideVelocity = (float)0.5;

        [Configurable]
        public float XVariance = 50;

        [Configurable]
        public float YVariance = 50;

        [Configurable]
        public int GlideDuration = 500;

        [Configurable]
        public OsbEasing PreEasingEffect;

        [Configurable]
        public OsbEasing EasingEffect;

        [Configurable]
        public string TargetSpriteImagePath = "img/lol.png";

        [Configurable]
        public string Particle1SpritePath = "SB/yui.png";
        #endregion

        #region Privates
        private Bitmap targetImage;
        
        private List<ParticleGlideObject> particles;

        #endregion

        #region Image Methods
        
        public void LoadTargetImage(string filename) {
            // Loads the image specified by filename into targetImage.
            // This will create the image file containing the pixels for the particles
            // to eventually glide to.
            try {
                targetImage = GetProjectBitmap(TargetSpriteImagePath);
            }
            catch {
                Log("Cannot find target image from file location: " + filename);
            }
        }
        
        public void AddPixelsToList() {
            // With the loaded image, locate all pixels that are fully black
            // and add their location to the particle glide object list.

            // I don't know if these asserts actually work man lol
            // Spoiler: They don't LOL
            Debug.Assert(targetImage != null);

            // Scan through the matrix for black pixels
            // TODO: Different criteria?

            for(int x = 0; x < targetImage.Width; x++) {
                for(int y = 0; y < targetImage.Height; y++) {
                    if(targetImage.GetPixel(x,y).ToArgb() == Color.Black.ToArgb()) {
                        Vector2 randomCoord = new Vector2(Random(0,936)-168, Random(0, 480));
                        Vector2 targetCoord = new Vector2(x,y);
                        particles.Add(new ParticleGlideObject(randomCoord, targetCoord));
                    }
                }
            }


        }
        
        #endregion


        #region Particle Methods
        public void GenerateParticle(ParticleGlideObject p) {
            // The money shot #ohbaby
            // This method will create the particle, then take the particle and have it glide over to target coordinates
            // TODO: Have some nice random slow glide movement PRIOR to moving to the target spot
            // and some fade effects too

            //  Create sprite
            var layer = GetLayer("ParticleGliders");
            var sprite = layer.CreateSprite(Particle1SpritePath, OsbOrigin.Centre, p.initV);

            GlideRandomly(sprite);
            GlideToTarget(sprite, p);

        }

        public void GlideRandomly(OsbSprite s) {
            // From StartTime to TriggerTime

            // Establish random gliding motions
            var preDuration = TriggerTime - StartTime;

            // Create move effects
             s.MoveX(PreEasingEffect, StartTime, TriggerTime, s.PositionAt(0).X, s.PositionAt(StartTime).X + (GlideVelocity * preDuration / 1000) + Random(0, XVariance*2) - XVariance);
             s.MoveY(PreEasingEffect, StartTime, TriggerTime, s.PositionAt(0).Y, s.PositionAt(StartTime).Y + (GlideVelocity * preDuration / 1000) + Random(0, YVariance*2) - YVariance);
        }

        public void GlideToTarget(OsbSprite s, ParticleGlideObject p) {
            // From TriggerTime to Assembled Point
            // Good, now move to the target spot
            var endTime = TriggerTime + GlideDuration;

            s.MoveX(EasingEffect, TriggerTime, endTime, s.PositionAt(TriggerTime).X, p.targetV.X);
            s.MoveY(EasingEffect, TriggerTime, endTime, s.PositionAt(TriggerTime).Y, p.targetV.Y);

            // Alright, let's let it stay there for demonstrating the effect worked HOORAY
            s.Fade(endTime, endTime + 1000, 1, 0);
        }
        #endregion

        #region Main Methods

        public override void Generate() {
            // Main Method!!! This is the moneymaker.
            particles = new List<ParticleGlideObject>();

            // DEBUG
            //LoadFallbackTarget(); // To populate the list.

            // Let's try this debugging too!
            LoadTargetImage(Particle1SpritePath);
            AddPixelsToList();

            Debug.Assert(particles.Count != 0);

            foreach (var p in particles) {
                GenerateParticle(p);
            }

        }

        public void LoadFallbackTarget() {
            // This will load a sample layout, so we don't need to worry about a failed image or something.
            // This will generate a list of particles so we can just run and go.
            int diagSplit = 50;
            Vector2 origin = new Vector2(320, 240);
            for (int i = -2; i < 3; i++) {
                for (int j = -2; j < 3; j++) {
                    Vector2 randomCoord = new Vector2(Random(0,640), Random(0, 480));
                    Vector2 targetCoord = new Vector2(diagSplit * i + origin.X, diagSplit * j + origin.Y);
                    particles.Add(new ParticleGlideObject(randomCoord, targetCoord));
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// ParticleGlideObject:
    /// Object contains a pixel coordinate it anchors to.
    /// Let it float around until you signal that it needs to move to the anchor point.
    /// At the moment, it only has the intiial location and the target location.
    /// 
    /// TODO: More stuff in here? LMAO
    /// </summary>
    public class ParticleGlideObject {

        // Here's the initial coordinates upon spawn
        public Vector2 initV { get; set; }

        // Pixel coordinates
        public Vector2 targetV { get; set; }

        // Constructor
        public ParticleGlideObject(Vector2 i, Vector2 t) {
            initV = i;
            targetV = t;
        }

        #region Methods

        // Woosh.

        #endregion
    }
}