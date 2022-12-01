using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FruitBowlBot_v2.Commands
{
    internal class RemotePlugin : IPluginCommand
    {
        public string PluginName => "Remote";
        public string Command => "remote";
        public IEnumerable<string> Help => new[] { "!remote uuuh it remotes" };
        public IEnumerable<string> Aliases => new string[0];
        public bool Loaded { get; set; } = true;

        public MouseMove mouse = new();
        public Random random = new();

     

        public async Task<string> Action(Message message)
        {
            string res = null;
            await Task.Run(() => { res = CAction(message); }).ConfigureAwait(false);
            return res;
        }

        public string CAction(Message message)
        {

            if (message.IsModerator && message.Username.ToLower() == "fluffykittenmika" || message.Username.ToLower()== "misternotalk")
            {

                string m = "";
                for (int i = 1; i < message.Arguments.Count; i++)
                {
                    m += message.Arguments[i] + " ";
                }

                if (message.Arguments[0] == "mouse")
                {
                    MouseMove.GetCursorPos(out Point p);
                    mouse.MoveMouse(0, 0 , random.Next(1, Screen.AllScreens.Sum(s => s.Bounds.Width)), random.Next(1, Screen.AllScreens.Max(s => s.Bounds.Height)));
                }
                if (message.Arguments[0] == "notepad")
                {
                    Notepad(message);
                }

                if (message.Arguments[0] == "error")
                {
                    MessageBox.Show(m,"IMPORTANT MESSAGE OF IMPORTANTNESS");
                }

                if (message.Arguments[0] == "write")
                {
                    SendKeys.SendWait(m);
                }

            }
            return "";
        }
            


        public static void Restart()
        {
            Process.Start("./FruitBowlBot-v2.exe");
            Process.GetCurrentProcess().Kill();
        }

        private void Notepad(Message msg)
        {
            string tempname = random.Next(0, 2000000).ToString();
            string m = "";

            for (int i = 1; i < msg.Arguments.Count; i++)
            {
                m += msg.Arguments[i] + " ";
            }
           
            File.WriteAllText($"./{tempname}.txt", $"{msg.Username} would like you to know the following: {m}");
            Thread.Sleep(2000);
            try
            {
                Process.Start("Notepad.exe",$"./{tempname}.txt");

            }
            catch (Exception e)
            {
                Console.WriteLine("ah heck " + e.Message);
            }
            //File.Delete($"./{tempname}.txt");
        }
    }

    public class MouseMove
    {

        static Random random = new();
        static int mouseSpeed = 15;

        public void Main(string[] args)
        {
            //MoveMouse(0, 0, 0, 0);
        }

        public void MoveMouse(int x, int y, int rx, int ry)
        {
            Point c = new();
            GetCursorPos(out c);

            x += random.Next(rx);
            y += random.Next(ry);

            double randomSpeed = Math.Max((random.Next(mouseSpeed) / 2.0 + mouseSpeed) / 10.0, 0.1);

            WindMouse(c.X, c.Y, x, y, 9.0, 3.0, 10.0 / randomSpeed,
                15.0 / randomSpeed, 10.0 * randomSpeed, 10.0 * randomSpeed);
        }

        public void WindMouse(double xs, double ys, double xe, double ye, 
            double gravity, double wind, double minWait, double maxWait,
            double maxStep, double targetArea)
        {

            double dist, windX = 0, windY = 0, veloX = 0, veloY = 0, randomDist, veloMag, step;
            int oldX, oldY, newX = (int)Math.Round(xs), newY = (int)Math.Round(ys);

            double waitDiff = maxWait - minWait;
            double sqrt2 = Math.Sqrt(2.0);
            double sqrt3 = Math.Sqrt(3.0);
            double sqrt5 = Math.Sqrt(5.0);

            dist = Hypot(xe - xs, ye - ys);

            while (dist > 1.0)
            {
                wind = Math.Min(wind, dist);

                if (dist >= targetArea)
                {
                    int w = random.Next((int)Math.Round(wind) * 2 + 1);
                    windX = windX / sqrt3 + (w - wind) / sqrt5;
                    windY = windY / sqrt3 + (w - wind) / sqrt5;
                }
                else
                {
                    windX = windX / sqrt2;
                    windY = windY / sqrt2;
                    if (maxStep < 3)
                        maxStep = random.Next(3) + 3.0;
                    else
                        maxStep = maxStep / sqrt5;
                }

                veloX += windX;
                veloY += windY;
                veloX = veloX + gravity * (xe - xs) / dist;
                veloY = veloY + gravity * (ye - ys) / dist;

                if (Hypot(veloX, veloY) > maxStep)
                {
                    randomDist = maxStep / 2.0 + random.Next((int)Math.Round(maxStep) / 2);
                    veloMag = Hypot(veloX, veloY);
                    veloX = (veloX / veloMag) * randomDist;
                    veloY = (veloY / veloMag) * randomDist;
                }

                oldX = (int)Math.Round(xs);
                oldY = (int)Math.Round(ys);
                xs += veloX;
                ys += veloY;
                dist = Hypot(xe - xs, ye - ys);
                newX = (int)Math.Round(xs);
                newY = (int)Math.Round(ys);

                if (oldX != newX || oldY != newY)
                    SetCursorPos(newX, newY);

                step = Hypot(xs - oldX, ys - oldY);
                int wait = (int)Math.Round(waitDiff * (step / maxStep) + minWait);
                Thread.Sleep(wait);
            }

            int endX = (int)Math.Round(xe);
            int endY = (int)Math.Round(ye);
            if (endX != newX || endY != newY)
                SetCursorPos(endX, endY);
        }

        static double Hypot(double dx, double dy)
        {
            return Math.Sqrt(dx * dx + dy * dy);
        }

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point p);
    }

}
