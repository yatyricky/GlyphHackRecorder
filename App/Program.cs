using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace App
{
    internal class Program
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern int BitBlt(IntPtr srchDC, int srcX, int srcY, int srcW, int srcH, IntPtr desthDC, int destX, int destY, int op);

        private const int CutOff = (int)(0.78f * 255);
        private static double prevBrightness;
        private const double BrightnessThreshold = 0.2;
        private const int RunningFrames = 120;

        private const int SRCCOPY = 13369376;

        private const int X = 1030;
        private const int Y = 180;
        private const int W = 500;
        private const int H = 1080;

        private const int BytesLength = W * H * 4;

        private static byte[] rgbValues;

        private static int frame;
        private static int recordFrame;

        private static int Update()
        {
            var bmp = new Bitmap(W, H);
            var g = Graphics.FromImage(bmp);
            var destDc = g.GetHdc();
            var hdc = GetDC(IntPtr.Zero);
            BitBlt(destDc, 0, 0, W, H, hdc, X, Y, SRCCOPY);
            ReleaseDC(IntPtr.Zero, hdc);
            g.ReleaseHdc(destDc);

            var bmpData = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly, bmp.PixelFormat);

            Marshal.Copy(bmpData.Scan0, rgbValues, 0, BytesLength);

            long blackCount = 0;
            long whiteCount = 0;

            for (int ri = 0; ri < BytesLength; ri += 4)
            {
                var gi = ri + 1;
                var bi = ri + 2;
                var grayScale = (int)(rgbValues[ri] * 0.3f + rgbValues[gi] * 0.59f + rgbValues[bi] * 0.11f);
                if (grayScale < CutOff)
                {
                    blackCount++;
                }
                else
                {
                    whiteCount++;
                }
            }

            var brightness = (double)whiteCount * 100 / (blackCount + whiteCount);
            Console.WriteLine(brightness);
            if (brightness - prevBrightness > BrightnessThreshold)
            {
                bmp.Save($"{recordFrame++}_{brightness:0.000}_.png");
            }

            prevBrightness = brightness;

            bmp.Dispose();

            return frame > RunningFrames ? 0 : 1;
        }

        public static void Main(string[] args)
        {
            foreach (var file in Directory.GetFiles("."))
            {
                if (file.EndsWith(".png"))
                {
                    File.Delete(file);
                }
            }

            rgbValues = new byte[BytesLength];
            frame = 0;
            recordFrame = 0;
            while (true)
            {
                var flag = Update();
                frame++;
                if (flag == 0)
                {
                    break;
                }

                Thread.Sleep(50);
            }
        }
    }
}
