using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Collections;

namespace Healpix
{

    class HealpixBorder
    {
        private short n;
        private ArrayList BorderLines;
        private double[] BorderThickness;

        // sampling interval on the unit sphere
        private double interval = Math.PI / 90;

        public HealpixBorder()
        {
            n = 0;
            BorderLines = null;
            BorderThickness = null;
        }

        public short Resolution
        {
            set
            {
                if (n != value)
                {
                    n = value;
                    GenerateBorders();
                }
            }
            get
            {
                return n;
            }
        }

        public double SamplingInterval
        {
            set { interval = value; }
            get { return interval; }
        }

        public int GetNumberOfBorderLines()
        {
            return BorderLines.Count;
        }

        public PointCollection GetAsSphericalCoord(int n)
        {
            if (n >= GetNumberOfBorderLines())
            {
                return null;
            }
            else
            {
                return BorderLines[n] as PointCollection;
            }
        }

        public Point3DCollection GetAsCartesianCoord(int n)
        {
            if (n >= GetNumberOfBorderLines())
            {
                return null;
            }
            else
            {
                PointCollection sph = BorderLines[n] as PointCollection;
                Point3DCollection cart = new Point3DCollection(sph.Count);
                IEnumerator ienum = sph.GetEnumerator();
                while (ienum.MoveNext())
                {
                    Point point = (Point)ienum.Current;
                    cart.Add(GetCartesianCoord(point.X, point.Y));
                }
                return cart;
            }
        }

        public static Point3D GetCartesianCoord(double theta, double phi)
        {
            double r = Math.Sin(theta);
            return new Point3D()
            {
                X = r * Math.Cos(phi),
                Y = r * Math.Sin(phi),
                Z = Math.Cos(theta)
            };
        }

        public static void GetSphericalCoord(Point3D cart, out double theta, out double phi)
        {
            phi = Math.Atan2(cart.Y, cart.X);
            theta = Math.Acos(cart.Z);
        }

        public double GetBorderThickness(int n)
        {
            if (n >= GetNumberOfBorderLines())
            {
                return 0.0;
            }
            else
            {
                return BorderThickness[n];
            }
        }

        const double BorderThicknessNormal = 1.0;
        const double BorderThicknessThick = 2.0;

        private void GenerateBorders()
        {
            int NumberOfBorders = 4 * (2 * n + 2 * n + 2 + 2 * n);
            BorderLines = new ArrayList(NumberOfBorders);
            BorderThickness = new double[NumberOfBorders];
            int BorderIndex = 0;

            // ------------------------------
            // polar cap area
            // ------------------------------

            for (short k = 1; k <= n; k++)
            {
                double start_phi = Math.PI * k / (2 * n);
                double end_phi = Math.PI / 2;
                PointCollection template = HealpixBorderLinePC(n, k, interval, start_phi, end_phi, false);
                PointCollection pts;
                for (int m = 0; m < 4; m++)
                {
                    // Northern Hemisphere
                    pts = new PointCollection(template.Count);
                    BorderLines.Add(pts);
                    BorderThickness[BorderIndex++] = BorderThicknessNormal;
                    foreach (Point temp in template)
                    {
                        pts.Add(new Point(temp.X, temp.Y + Math.PI * m / 2));
                    }
                    // Southern Hemisphere
                    pts = new PointCollection(template.Count);
                    BorderLines.Add(pts);
                    BorderThickness[BorderIndex++] = BorderThicknessNormal;
                    foreach (Point temp in template)
                    {
                        pts.Add(new Point(Math.PI - temp.X, temp.Y + Math.PI * m / 2));
                    }
                }
                
                start_phi = 0;
                end_phi = Math.PI / 2 - Math.PI * k / (2 * n);
                template = HealpixBorderLinePC(n, k, interval, start_phi, end_phi, true);
                for (int m = 0; m < 4; m++)
                {
                    // Northern Hemisphere
                    pts = new PointCollection(template.Count);
                    BorderLines.Add(pts);
                    BorderThickness[BorderIndex++] = BorderThicknessNormal;
                    foreach (Point temp in template)
                    {
                        pts.Add(new Point(temp.X, temp.Y + Math.PI * m / 2));
                    }
                    // Southern Hemisphere
                    pts = new PointCollection(template.Count);
                    BorderLines.Add(pts);
                    BorderThickness[BorderIndex++] = BorderThicknessNormal;
                    foreach (Point temp in template)
                    {
                        pts.Add(new Point(Math.PI - temp.X, temp.Y + Math.PI * m / 2));
                    }
                }
            }

            for (short k = 0; k < 4; k++)
            {
                double theta = (double)k * Math.PI / 2;
                double phi;

                // Northern Hemisphere
                PointCollection points = new PointCollection();
                BorderLines.Add(points);
                BorderThickness[BorderIndex++] = BorderThicknessThick;

                double phi_end = Math.Acos(2.0 / 3);
                for (phi = 0.0; phi < phi_end; phi += interval)
                {
                    points.Add(new Point(phi, theta));
                }
                if (phi != phi_end)
                {  // if the end edge point was not included
                    points.Add(new Point(phi_end, theta));    // add the end edge
                }


                // Southern Hemisphere
                points = new PointCollection();
                BorderLines.Add(points);
                BorderThickness[BorderIndex++] = BorderThicknessThick;

                double phi_begin = Math.Acos(-2.0 / 3);
                phi_end = Math.PI;
                for (phi = phi_begin; phi < phi_end; phi += interval)
                {
                    points.Add(new Point(phi, theta));
                }
                if (phi != phi_end)
                {  // if the end edge point was not included
                    points.Add(new Point(phi_end, theta));    // add the end edge
                }
            }

            // ------------------------------
            // equatorial belt area
            // ------------------------------

            for (short k = (short)(-3 * n); k < n; k++)
            {
                double start_theta = Math.Acos(-2.0 / 3);
                double start_phi = (-4.0 / 3 + 4.0 * k / (3 * n)) * 3 * Math.PI / 8;
                double end_phi = 4.0 * k / (3 * n) * 3 * Math.PI / 8;
                PointCollection line = HealpixBorderLineEB(n, k, interval, start_phi, end_phi, start_theta);
                BorderLines.Add(line);
                BorderThickness[BorderIndex++] = (k % n == 0) ? BorderThicknessThick : BorderThicknessNormal;

                start_theta = Math.Acos(2.0 / 3);
                double temp = -start_phi;
                start_phi = -end_phi;
                end_phi = temp;
                line = HealpixBorderLineEB(n, k, interval, start_phi, end_phi, start_theta);
                BorderLines.Add(line);
                BorderThickness[BorderIndex++] = (k % n == 0) ? BorderThicknessThick : BorderThicknessNormal;
            }

        }

        // HealpixBorderLine for the polar cap area
        private PointCollection HealpixBorderLinePC(short n, short k, 
            double interval, double start_phi, double end_phi, bool flag)
        {
            double delta_phi = interval;
            double phi = start_phi - delta_phi;
            
            PointCollection P = new PointCollection();
            while (phi < end_phi)
            {
                double theta, dTheta_dPhi;
                HealpixBorderGetNextPC(n, k, phi, delta_phi, flag, out phi, out theta, out dTheta_dPhi);
                P.Add(new Point(theta, phi));
                // distance in the tangent plane on the unit sphere
                double dL_dPhi = Math.Sqrt(dTheta_dPhi * dTheta_dPhi + Math.Pow(Math.Sin(theta), 2));
                // decide the next sampling point so that each distance between the points becomes same
                delta_phi = interval / dL_dPhi;
                if (phi + delta_phi > end_phi)
                {
                    delta_phi = end_phi - phi;  // add the end edge point

                }
            }
            return P;
        }

        // HealpixBorderGetNext for the north polar cap area
        // flag = false : where dTheta_dPhi >= 0
        // flag = true  : where dTheta_dPhi <= 0
        private void HealpixBorderGetNextPC(short n, short k, 
            double prev_phi, double delta_phi, bool flag,
            out double phi, out double theta, out double dTheta_dPhi)
        {

            phi = prev_phi + delta_phi;
            double a = 1;
            double b = -Math.Pow(k * Math.PI, 2) / (12 * n * n);

            if (flag == false)
            {
                theta = Math.Acos(a + b / Math.Pow(phi, 2));
                dTheta_dPhi = 2.0 * b / (Math.Pow(phi, 3) * Math.Sin(theta));
            }
            else
            {
                theta = Math.Acos(a + b / Math.Pow(phi - Math.PI / 2, 2));
                dTheta_dPhi = 2.0 * b / (Math.Pow(phi - Math.PI / 2, 3) * Math.Sin(theta));
            }
        }

        // HealpixBorderLine for the equatorial belt area
        private PointCollection HealpixBorderLineEB(short n, short k, 
            double interval, double start_phi, double end_phi, double start_theta)
        {
            double delta_phi = interval;
            double phi = start_phi - delta_phi;
            double delta_theta = interval;
            double theta = start_theta - delta_theta;

            PointCollection P = new PointCollection();
            while (phi < end_phi)
            {
                double dTheta_dPhi;
                HealpixBorderGetNextEB(n, k, phi, theta, delta_phi, delta_theta, out phi, out theta, out dTheta_dPhi);
                P.Add(new Point(theta, phi));
                // distance in the tangent plane on the unit sphere
                double dL_dPhi = Math.Sqrt(dTheta_dPhi * dTheta_dPhi + Math.Pow(Math.Sin(theta), 2));
                // decide the next sampling point so that each distance between the points becomes same
                delta_phi = interval / dL_dPhi;
                delta_theta = dTheta_dPhi * delta_phi;
                if (phi + delta_phi > end_phi)
                {
                    delta_phi = end_phi - phi;  // add the end edge point
                }
            }
            return P;
        }

        // HealpixBorderGetNext for the equatorial belt area
        private void HealpixBorderGetNextEB(short n, short k, 
            double prev_phi, double prev_theta, double delta_phi, double delta_theta, 
            out double phi, out double theta, out double dTheta_dPhi)
        {

            phi = prev_phi + delta_phi;
            double a = 2.0 / 3 - 4.0 * k / (3 * n);
            double b = 8.0 / (3.0 * Math.PI);
            double c0 = a + b * phi;
            double c1 = a - b * phi;
            if (Math.Abs(c0) > 1.0)
            {
                theta = Math.Acos(c1);
                dTheta_dPhi = +b / Math.Sin(theta);
            }
            else if (Math.Abs(c1) > 1.0)
            {
                theta = Math.Acos(c0);
                dTheta_dPhi = -b / Math.Sin(theta);
            }
            else
            {
                double theta0 = Math.Acos(c0);
                double theta1 = Math.Acos(c1);
                double exp_theta = prev_theta + delta_theta;
                if (Math.Abs(theta0 - exp_theta) < Math.Abs(theta1 - exp_theta))
                {
                    theta = theta0;
                    dTheta_dPhi = -b / Math.Sin(theta);
                }
                else
                {
                    theta = theta1;
                    dTheta_dPhi = +b / Math.Sin(theta);
                }
            }
        }
    }


}
