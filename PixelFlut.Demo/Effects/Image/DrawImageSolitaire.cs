using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using SixLabors.ImageSharp;

namespace PixelFlut.Infrastructure.Effects.Image
{
    public class DrawImageSolitaire : DrawImageBase
    {
        const int initialSpeedX = 5;

        private readonly List<Tuple<Image<Rgba32>, byte[]>> images;
        private readonly List<OutputPixel[]> imagesCache = new List<OutputPixel[]>();
        private readonly Random r;
        private readonly int cardCount;
        private List<(double speedX, double speedY, double offsetX, double offsetY, int imgIdx)> states;
        private uint _i;


        public DrawImageSolitaire(IList<string> filePaths, int cardCount)
        {
            images = filePaths.Select(x => GetImageData(x)).ToList();
            this.r = new Random();
            states = new List<(double speedX, double speedY, double offsetX, double offsetY, int imgIdx)>();
            this.cardCount = cardCount;
            //states.Add((5, 0, r.Next(250, 1000), 0));
        }

        public override void Init(Size canvasSize)
        {
            base.Init(canvasSize);
            states.Clear();
            var currentImage = images[0].Item1;
            for (int i = 0; i < this.cardCount; i++)
            {
                states.Add((initialSpeedX, 0, r.Next(0, canvasSize.Width - currentImage.Width), r.Next(0, canvasSize.Height - currentImage.Height), 0));
            }

            foreach (var image in images)
            {
                imagesCache.Add(DrawImage(image, Point.Empty).ToArray());
            }
        }

        protected override OutputFrame TickInternal()
        {
            int i;
            unchecked
            {
                i = (int)(_i++ % states.Count);
            }

            var state = states[i];

            state.offsetX += state.speedX;
            state.offsetY += state.speedY;

            var imageIdx = state.imgIdx;
            var image = images[imageIdx];

            if (state.offsetY + image.Item1.Height > CanvasSize.Height)
            {
                state.speedY = -state.speedY;
            }
            else
            {
                state.speedY += 1;
                if (state.speedY > 0)
                    state.speedY /= 1.04;
            }

            if (state.offsetX + image.Item1.Width > CanvasSize.Width || state.offsetX < 0)
            {
                state.speedX = -state.speedX;
            }

            if (Math.Abs(state.speedY) < 0.4 && (CanvasSize.Height - (state.offsetY + image.Item1.Height) < 10))
            {
                state.speedX = r.Next(100) > 50 ? r.Next(initialSpeedX / 2, initialSpeedX) : -r.Next(initialSpeedX / 2, initialSpeedX);
                state.speedY = 0d;

                state.offsetX = r.Next(0, CanvasSize.Width - image.Item1.Width);
                state.offsetY = 0d;
                state.imgIdx = r.Next(0, this.images.Count);
            }

            states[i] = state;

            int offsetX;
            int offsetY;

            if (state.offsetX < 0)
            {
                offsetX = 0;
            }
            else if (state.offsetX >= CanvasSize.Width)
            {
                offsetX = CanvasSize.Width - 1;
            }
            else
            {
                offsetX = (int)state.offsetX;
            }

            if (state.offsetY < 0)
            {
                offsetY = 0;
            }
            else if (state.offsetY >= CanvasSize.Height)
            {
                offsetY = CanvasSize.Height - 1;
            }
            else
            {
                offsetY = (int)state.offsetY;
            }

            var cachedImage = imagesCache[imageIdx];

            return new OutputFrame(offsetX, offsetY, cachedImage, imageIdx, false);
        }
    }
}