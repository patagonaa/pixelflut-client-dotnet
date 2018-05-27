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
        private Tuple<Image<Rgba32>, byte[]> currentImg;
        private readonly Random r;
        private readonly int cardCount;
        private List<(double speedX, double speedY, double offsetX, double offsetY)> states;
        private uint _i;


        public DrawImageSolitaire(IList<string> filePaths, int cardCount)
        {
            images = filePaths.Select(x => GetImageData(x)).ToList();
            currentImg = images[0];
            this.r = new Random();
            states = new List<(double speedX, double speedY, double offsetX, double offsetY)>();
            this.cardCount = cardCount;
            //states.Add((5, 0, r.Next(250, 1000), 0));
        }

        public override void Init(Size canvasSize)
        {
            base.Init(canvasSize);
            states.Clear();
            var image = currentImg.Item1;
            for (int i = 0; i < this.cardCount; i++)
            {
                states.Add((initialSpeedX, 0, r.Next(0, canvasSize.Width - image.Width), r.Next(0, canvasSize.Height - image.Height)));
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

            var image = currentImg.Item1;

            if (state.offsetY + image.Height > CanvasSize.Height)
            {
                state.speedY = -state.speedY;
            }
            else
            {
                state.speedY += 1;
                if (state.speedY > 0)
                    state.speedY /= 1.04;
            }

            if (state.offsetX + image.Width > CanvasSize.Width || state.offsetX < 0)
            {
                state.speedX = -state.speedX;
            }

            if (Math.Abs(state.speedY) < 0.4 && (CanvasSize.Height - (state.offsetY + image.Height) < 10))
            {
                state.speedX = r.Next(100) > 50 ? r.Next(initialSpeedX / 2, initialSpeedX) : -r.Next(initialSpeedX / 2, initialSpeedX);
                state.speedY = 0d;

                state.offsetX = r.Next(0, CanvasSize.Width - image.Width);
                state.offsetY = 0d;
                this.currentImg = this.images[r.Next(0, this.images.Count)];
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

            return new OutputFrame(offsetX, offsetY, DrawImage(currentImg, Point.Empty).ToArray());
        }
    }
}