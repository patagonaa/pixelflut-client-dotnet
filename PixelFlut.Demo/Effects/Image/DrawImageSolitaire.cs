using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

namespace PixelFlut.Infrastructure.Effects.Image
{
    public class DrawImageSolitaire : DrawImageBase
    {
        private readonly Random r;
        private int i;
        private List<(double speedX, double speedY, double offsetX, double offsetY)> states;


        public DrawImageSolitaire(string filePath) : base(filePath)
        {
            this.r = new Random();
            states = new List<(double speedX, double speedY, double offsetX, double offsetY)>();
            states.Add((5, 0, r.Next(250, 1000), 0));
            states.Add((5, 0, r.Next(250, 1000), 0));
            //states.Add((5, 0, r.Next(250, 1000), 0));
        }

        protected override IEnumerable<OutputPixel> TickInternal()
        {
            var index = i++ % states.Count;

            var state = states[index];

            state.offsetX += state.speedX;
            state.offsetY += state.speedY;

            if (state.offsetY + image.Height > CanvasSize.Height)
            {
                state.speedY = -(state.speedY / 1.1);
            }
            else
            {
                state.speedY += 1;
            }

            if (state.offsetX + image.Width > CanvasSize.Width)
            {
                state.speedX = -state.speedX;
            }

            if (state.offsetX < 250)
            {
                state.speedX = -state.speedX;
            }

            if (Math.Abs(state.speedY) < 0.2 && (CanvasSize.Height - (state.offsetY + image.Height) < 10))
            {
                state.speedY = 0d;

                state.offsetX = r.Next(250, CanvasSize.Width - image.Width);
                state.offsetY = 0d;
            }

            states[index] = state;

            return DrawImage(new Point((int)state.offsetX, (int)state.offsetY));
        }
    }
}