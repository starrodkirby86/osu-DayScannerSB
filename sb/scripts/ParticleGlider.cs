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
        public int GlideDuration = 500;

        [Configurable]
        public OsbEasing EasingEffect;

        [Configurable]
        public string TargetSpriteImagePath = "img/lol.png";

        [Configurable]
        public string Particle1SpritePath = "SB/yui.png";
        #endregion

        #region Privates
        // private Bitmap targetImage;
        private List<ParticleGlideObject> particles = new List<ParticleGlideObject>();

        #endregion

        #region Image Methods
        /*
        public void LoadTargetImage(string filename) {
            // Loads the image specified by filename into targetImage.
            // This will create the image file containing the pixels for the particles
            // to eventually glide to.
            try {
                targetImage = (Bitmap)targetImage.FromFile(filename, true);
            }
            catch {
                Console.Error.WriteLine("Cannot find target image from file location: " + filename);
            }
        }
        */

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

            // Good, now move to the target spot
            sprite.Move(EasingEffect, StartTime, StartTime + GlideDuration, sprite.PositionAt(StartTime), p.targetV);

            // Alright, let's let it stay there for SHOWING
            sprite.Fade(StartTime + GlideDuration, (StartTime + GlideDuration) + 1000, 1, 0);
        }
        #endregion

        #region Main Methods

        public override void Generate() {
            // Main Method!!! This is the moneymaker.

            // DEBUG
            LoadFallbackTarget(); // To populate the list.

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
                    Vector2 randomCoord = new Vector2(Random(-280, 861), Random(-100, 513));
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