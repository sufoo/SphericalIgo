using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Healpix
{
    public struct HealpixIndex
    {
        public double i { get; set; }
        public double j { get; set; }
    }

    public struct HealpixIndexInt
    {
        public int i { get; set; }
        public int j { get; set; }
    }

    public struct HealpixPolarCoord
    {
        public double theta { get; set; }
        public double phi { get; set; }
    }

    public class Healpix
    {
        public Healpix()
        {
            n = 0;
            Points = null;
        }
        public short Resolution
        {
            set
            {
                if (n != value)
                {
                    n = value;
                    n2 = n * n;
                    n_total = 12 * n * n;
                    GenerateSampling();
                }
            }
            get
            {
                return n;
            }
        }
        public Point3DCollection Points { get; set; }
        public HealpixPolarCoord[] PolarCoords { get; set; }
        private short n;
        private int n2;
        private int n_total;

        // Generate sampling points on HEALPix
        //
        // OUT = GenerateSampling(n, mode)
        // Parameters
        // n : resolution of the grid (N_side)
        void GenerateSampling()
        {
            Points = new Point3DCollection(n_total);
            PolarCoords = new HealpixPolarCoord[n_total];
            for (int pn = 0; pn < n_total; pn++)
            {
                HealpixIndex index = GetNestedIndex(pn);
                HealpixPolarCoord polar = GetSphCoord(index);
                PolarCoords[pn] = polar;
                Point3D point = GetCartesianCoord(polar);
                Points.Add(point);
            }
        }
        // I = GetNestedIndex(n, pn)
        // Parameters
        // n  : resolution of the grid (N_side)
        // pn : index number
        HealpixIndex GetNestedIndex(int pn)
        {
            int f = pn / n2;
            int pnd = pn % n2;

            int x = 0;
            int y = 0;

            for (int b = 0; b < 15; b++)
            {
                if ((pnd & (1 << (2 * b))) != 0)
                {
                    x += (1 << b);
                }
                if ((pnd & (1 << (2 * b + 1))) != 0)
                {
                    y += (1 << b);
                }
            }

            int[] f1l = { 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4 };
            int[] f2l = { 1, 3, 5, 7, 0, 2, 4, 6, 1, 3, 5, 7 };
            int f1 = f1l[f];
            int f2 = f2l[f];

            int v = x + y;
            int h = x - y;

            int i = f1 * n - v - 1;

            int s, nr;
            if (i < n)
            {
                s = 1;
                nr = i;
            }
            else if (3 * n < i)
            {
                s = 1;
                nr = 4 * n - i;
            }
            else
            {
                s = (i - n + 1) % 2;
                nr = n;
            }

            int j = (f2 * nr + h + s) / 2;

            if (j > 4 * n)
            {
                j = j - 4 * n;
            }
            if (j < 1)
            {
                j = j + 4 * n;
            }

            return new HealpixIndex()
            {
                i = i,
                j = j
            };
        }
        // Convert spherical polar coordhinate to cartesian coordinate
        public HealpixPolarCoord GetSphCoord(HealpixIndex index)
        {
            if (index.i <= 2 * n)
            {
                // Northern Hemisphere
                return GetNorthernHemisphere(index);
            }
            else
            {
                // Southern Hemisphere
                index.i = 4.0 * n - index.i;
                HealpixPolarCoord temp = GetNorthernHemisphere(index);  // mirror symmetry of the north
                temp.theta = Math.PI - temp.theta;
                return temp;
            }
        }
        public Point3D GetCartesianCoord(HealpixPolarCoord polar)
        {
            double r = Math.Sin(polar.theta);
            return new Point3D()
            {
                X = r * Math.Cos(polar.phi),
                Y = r * Math.Sin(polar.phi),
                Z = Math.Cos(polar.theta)
            };
        }
        // [theta, phi] = NorthernHemisphere(n, i, j)
        // Parameters
        // n : resolution of the grid (N_side)
        // i : ring index
        // j : pixel in ring index
        public HealpixPolarCoord GetNorthernHemisphere(HealpixIndex index)
        {
            if (index.i < n)
            {
                // North polar cap
                return new HealpixPolarCoord()
                {
                    theta = Math.Acos(1.0 - index.i * index.i / (3 * n2)),
                    phi = Math.PI * (index.j - 0.5) / (2 * index.i)
                };
            }
            else
            {
                // North equatorial belt
                double s = (index.i - n + 1) % 2;
                return new HealpixPolarCoord()
                {
                    theta = Math.Acos(4.0 / 3 - 2.0 * index.i / (3 * n)),
                    phi = Math.PI * (index.j - s / 2) / (2 * n)
                };
            }
        }

        public HealpixIndex GetHealpixIndex(HealpixPolarCoord polar)
        {
            if (polar.phi < 0) polar.phi += (2.0 * Math.PI);
            double i = Math.Sqrt((1.0 - Math.Cos(polar.theta)) * 3 * n2);
            if (Math.Cos(polar.theta) > 0)
            {
                // Northern Hemisphere
                return GetHealpixIndexForNorthernHemisphere(polar);
            }
            else
            {
                // Southern Hemisphere
                HealpixPolarCoord polar_north = new HealpixPolarCoord()
                {
                    theta = Math.PI - polar.theta,
                    phi = polar.phi
                };
                HealpixIndex temp = GetHealpixIndexForNorthernHemisphere(polar_north);  // mirror symmetry of the north
                temp.i = 4.0 * n - temp.i;
                return temp;
            }

        }

        public HealpixIndex GetHealpixIndexForNorthernHemisphere(HealpixPolarCoord polar)
        {
            double temp_i = Math.Sqrt((1.0 - Math.Cos(polar.theta)) * 3 * n2);

            if (temp_i < n)
            {
                double i = Math.Sqrt((1.0 - Math.Cos(polar.theta)) * 3 * n2);
                return new HealpixIndex()
                {
                    i = i,
                    j = polar.phi * 2.0 * i / Math.PI + 0.5
                };
            }
            else
            {
                double i = (4.0 / 3 - Math.Cos(polar.theta)) * (3 * n) / 2;
                double s = (i - n + 1) % 2;
                return new HealpixIndex()
                {
                    i = i,
                    j = polar.phi * 2.0 * n / Math.PI + s / 2
                };
            }
        }

#if false
        // HEALPix projection onto the plane
        //
        // [i, j] = HealpixProjectionOntoPlane(n, x, y)
        //
        // Parameters
        // n : resolution of the grid (N_side)
        // x : coordinate in plane (corresponding to azimuth)
        // y : coordinate in plane (corresponding to elevation)
        // i : ring index in decimal
        // j : intra-ring index in decimal
        public void HealpixProjectionOntoPlane(double x, double y, out double i, out double j)
        {
            i = 2.0 * n + y * 4 * n / Math.PI;   // 0.0 <= i <= 4 * n

            // clip to the valid range
            if(i <= 0){
                i = 0;
            }
            if(i >= 4 * n){
                i = 4 * n;
            }

            double s;
            double n_j;

            // pole
            if((i < 1) || (i > 4 * n - 1)){
                s = 1;
                n_j = 2;
            }else if(i < n){
            // north polar cap area
                s = 1;
                n_j = 2 * i;
            }else if(i > 3 * n){
            // south polar cap area
                s = 1;
                n_j = 2 * (4 * n - i);
            }else{
            // equatorial belt area
                double di_d = i - n;
                int di_i = (int)di_d;
                if((di_i % 2) == 0)
                {
                    s = 1 - (di_d - di_i);
                }
                else
                {
                    s = di_d - di_i;
                }
                n_j = 2 * n;
            }

            j = x * n_j / Math.PI + s / 2;   // 0.0 < j <= 2 * n_j

            // clip to the valid range
            if(j <= 0){
                j = j + 2 * n_j;
            }
            if(j > 2 * n_j){
                j = j - 2 * n_j;
            }
        }
#endif

        enum HealpixPatchClass
        {
            PATCH_CLASS_O,
            PATCH_CLASS_R,
            PATCH_CLASS_T,
            PATCH_CLASS_P,
            PATCH_CLASS_E,
            PATCH_CLASS_B,
            PATCH_CLASS_V
        }

        struct HealpixPointInfo
        {
            public HealpixPatchClass patch_class;
            public bool is_south_pole;
            public bool is_polar_cap;
            public int polar_part;

            public double i_n;
            public int int_i_n;
            public double decimal_i_n;

            public double polar_intra_part;
            public int int_polar_intra_part;
            public double decimal_polar_intra_part;

        }

        // Get the class of patch that the specified point belongs 
        // on the HEALPix tessellation
        //
        // Patch classes
        // [class-id] [shape]    [location]
        // 'o'        rectangle  pole
        // 'r'        rectangle  polar cap
        // 't'        triangle   equatorial belt
        // 'p'        rhombus    polar cap
        // 'e'        rhombus    equatorial belt
        // 'b'        rhombus    border between polar cap and equatorial belt
        // 'v'        invalid
        HealpixPointInfo HealpixSelectPatchClass(int n, double i, double j)
        {
            HealpixPointInfo INFO = new HealpixPointInfo();

            if (i <= 2 * n)
            {
                // Northern Hemisphere
                INFO.i_n = i;
                INFO.is_south_pole = false;
            }
            else
            {
                // Southern Hemisphere
                INFO.i_n = 4 * n - i;
                INFO.is_south_pole = true;
            }

            if (INFO.i_n < n)
            {
                // polar cap
                INFO.is_polar_cap = true;
            }
            else
            {
                // equatorial belt
                INFO.is_polar_cap = false;
            }

            // obviously belongs to the equatorial belt
            if (INFO.i_n >= n + 1)
            {
                INFO.patch_class = HealpixPatchClass.PATCH_CLASS_E;
                return INFO;
            }

            // belongs to the pole rectangle
            if (INFO.i_n <= 1)
            {
                INFO.patch_class = HealpixPatchClass.PATCH_CLASS_O;
                return INFO;
            }

            double part_width;
            // width of the partition
            if (INFO.i_n <= n)
            {
                part_width = INFO.i_n;
            }
            else
            {
                part_width = n;
            }

            // partition number within the porlar cap
            INFO.polar_part = (int)(j / part_width);
            // intra-partition index
            INFO.polar_intra_part = j - part_width * INFO.polar_part;

            if (INFO.polar_part >= 4 && INFO.polar_intra_part != 0)
            {
                INFO.patch_class = HealpixPatchClass.PATCH_CLASS_V;
                return INFO;
            }

            // integer part of the intra-partition index
            INFO.int_polar_intra_part = (int)(INFO.polar_intra_part);
            // decimal part of the intra-partition index
            INFO.decimal_polar_intra_part = INFO.polar_intra_part - INFO.int_polar_intra_part;
            // integer part of the i_n
            INFO.int_i_n = (int)INFO.i_n;
            // decimal part of the i_n
            INFO.decimal_i_n = INFO.i_n - INFO.int_i_n;

            // belongs to the equatorial belt
            if (INFO.i_n >= n)
            {
                if (INFO.decimal_polar_intra_part < 1 - INFO.decimal_i_n)
                {
                    if (INFO.polar_intra_part < 1)
                    {
                        INFO.patch_class = HealpixPatchClass.PATCH_CLASS_T;
                    }
                    else
                    {
                        INFO.patch_class = HealpixPatchClass.PATCH_CLASS_B;
                    }
                }
                else
                {
                    INFO.patch_class = HealpixPatchClass.PATCH_CLASS_E;
                }
                return INFO;

            }

            // gradient of the border
            double grad = INFO.polar_part;
            // beginning position of the partition
            double part_begin = grad * (INFO.i_n - 1) + grad;

            // belongs to the border between the partitions in polar cap
            if (part_begin <= j && j <= part_begin + 1)
            {
                INFO.patch_class = HealpixPatchClass.PATCH_CLASS_R;
                return INFO;
            }
            else
            {
                INFO.patch_class = HealpixPatchClass.PATCH_CLASS_P;
            }

            if (INFO.i_n > n - 1)
            {
                grad = INFO.polar_part;
                int begin_org_j = (int)(j - grad * INFO.decimal_i_n);
                double begin_j = grad * INFO.decimal_i_n + begin_org_j;
                double end_j = (grad + 1) * INFO.decimal_i_n + begin_org_j;
                if (begin_j <= j && j < end_j)
                {
                    INFO.patch_class = HealpixPatchClass.PATCH_CLASS_B;
                }
            }

            return INFO;
        }

        // Get vertex coordinates for the patch type 'B'
        // [C, N, S, W, E] = HealpixGetPatchVertexCoordsB(n, i, j, INFO)
        //
        // Parameters
        // n : grid resolution
        // i : ring index
        // j : intra-ring index
        // INFO : intermediate information (output of HealpixSelectPatchClass())
        // C : intra-patch coordinates
        // N : coordinates for north vertex
        // S : coordinates for south vertex
        // W : coordinates for west vertex
        // E : coordinates for east vertex
        void HealpixGetPatchVertexCoordsB(int n, double i, double j, HealpixPointInfo INFO,
            out HealpixIndex C, out HealpixIndexInt N, out HealpixIndexInt S, out HealpixIndexInt W, out HealpixIndexInt E)
        {
            C = new HealpixIndex();
            N = new HealpixIndexInt();
            S = new HealpixIndexInt();
            W = new HealpixIndexInt();
            E = new HealpixIndexInt();

            // gradient of the border
            int grad = INFO.polar_part;

            double decimal_i = INFO.decimal_i_n;
            double decimal_j = INFO.decimal_polar_intra_part;

            int int_i = INFO.int_i_n;
            int int_j;

            if (int_i == n)
            {
                int_j = (int)j;
                if (int_j == 0)
                {
                    int_j = 4 * INFO.int_i_n;
                }

                int north_int_j = ((int_j - grad - 1) % (4 * (n - 1))) + 1;
                int east_int_j = ((int_j + 1 - 1) % (4 * n)) + 1;

                N.i = int_i - 1;
                N.j = north_int_j;
                S.i = int_i + 1;
                S.j = int_j;
                W.i = int_i;
                W.j = int_j;
                E.i = int_i;
                E.j = east_int_j;

                double offset_j = 0.5 * decimal_i;
                C.i = decimal_i;
                C.j = decimal_j + offset_j - 0.5;
            }
            else
            {
                int_j = (int)(j - grad * decimal_i);
                if (int_j == 0)
                {
                    int_j = 4 * n;
                }

                int west_int_j = ((int_j + grad - 1) % (4 * n)) + 1;
                int east_int_j = ((west_int_j + 1 - 1) % (4 * n)) + 1;

                N.i = int_i;
                N.j = int_j;
                S.i = int_i + 2;
                S.j = west_int_j;
                W.i = int_i + 1;
                W.j = west_int_j;
                E.i = int_i + 1;
                E.j = east_int_j;

                double offset_j = 0.5 * (1 - decimal_i);
                C.i = decimal_i - 1;
                C.j = decimal_j + offset_j - 0.5;
            }

            if (INFO.is_south_pole)
            {
                HealpixIndexInt TMP = N;
                N = S;
                S = TMP;
                N.i = 4 * n - N.i;
                S.i = 4 * n - S.i;
                W.i = 4 * n - W.i;
                E.i = 4 * n - E.i;
                C.i = -C.i;
            }
        }

        // Get vertex coordinates for the patch type 'E'
        // [C, N, S, W, E] = HealpixGetPatchVertexCoordsE(n, i, j)
        //
        // Parameters
        // n : grid resolution
        // i : ring index
        // j : intra-ring index
        // C : intra-patch coordinates
        // N : coordinates for north vertex
        // S : coordinates for south vertex
        // W : coordinates for west vertex
        // E : coordinates for east vertex
        void HealpixGetPatchVertexCoordsE(int n, double i, double j, HealpixPointInfo INFO,
            out HealpixIndex C, out HealpixIndexInt N, out HealpixIndexInt S, out HealpixIndexInt W, out HealpixIndexInt E)
        {
            C = new HealpixIndex();
            N = new HealpixIndexInt();
            S = new HealpixIndexInt();
            W = new HealpixIndexInt();
            E = new HealpixIndexInt();

            int int_i = (int)i;
            int int_j = (int)j;
            double decimal_i = i - int_i;
            double decimal_j = j - int_j;

            if (int_j == 0)
            {
                int_j = 4 * n;
            }

            int east_int_j = ((int_j + 1 - 1) % (4 * n)) + 1;

            if ((int_i - n) % 2 == 0)
            {
                if (decimal_i < 1.0 - decimal_j)
                {
                    N.i = int_i - 1;
                    N.j = int_j;
                    S.i = int_i + 1;
                    S.j = int_j;
                    W.i = int_i;
                    W.j = int_j;
                    E.i = int_i;
                    E.j = east_int_j;

                    double offset_j = 0.5 * decimal_i;
                    C.i = decimal_i;
                    C.j = decimal_j + offset_j - 0.5;
                }
                else
                {
                    N.i = int_i;
                    N.j = east_int_j;
                    S.i = int_i + 2;
                    S.j = east_int_j;
                    W.i = int_i + 1;
                    W.j = int_j;
                    E.i = int_i + 1;
                    E.j = east_int_j;

                    double offset_j = 0.5 * (1.0 - decimal_i);
                    C.i = decimal_i - 1;
                    C.j = decimal_j - offset_j - 0.5;
                }
            }
            else
            {
                if (decimal_i < decimal_j)
                {
                    N.i = int_i - 1;
                    N.j = east_int_j;
                    S.i = int_i + 1;
                    S.j = east_int_j;
                    W.i = int_i;
                    W.j = int_j;
                    E.i = int_i;
                    E.j = east_int_j;

                    double offset_j = 0.5 * decimal_i;
                    C.i = decimal_i;
                    C.j = decimal_j - offset_j - 0.5;
                }
                else
                {
                    N.i = int_i;
                    N.j = int_j;
                    S.i = int_i + 2;
                    S.j = int_j;
                    W.i = int_i + 1;
                    W.j = int_j;
                    E.i = int_i + 1;
                    E.j = east_int_j;

                    double offset_j = 0.5 * (1.0 - decimal_i);
                    C.i = decimal_i - 1;
                    C.j = decimal_j + offset_j - 0.5;
                }
            }
        }


        // Get vertex coordinates for the patch type 'O'
        // [C, N, S, W, E] = HealpixGetPatchVertexCoordsO(n, i, j, INFO)
        //
        // Parameters
        // n : grid resolution
        // i : ring index
        // j : intra-ring index
        // INFO : intermediate information (output of HealpixSelectPatchClass())
        // C : intra-patch coordinates
        // P0 : (i, j) = (1, 1)
        // P1 : (i, j) = (1, 2)
        // P2 : (i, j) = (1, 3)
        // P3 : (i, j) = (1, 4)
        void HealpixGetPatchVertexCoordsO(int n, double i, double j, HealpixPointInfo INFO,
            out HealpixIndex C, out HealpixIndexInt P0, out HealpixIndexInt P1, out HealpixIndexInt P2, out HealpixIndexInt P3)
        {
            C = new HealpixIndex();
            P0 = new HealpixIndexInt();
            P1 = new HealpixIndexInt();
            P2 = new HealpixIndexInt();
            P3 = new HealpixIndexInt();

            if (INFO.is_south_pole == false)
            {
                P0.i = 1;
                P0.j = 1;
                P1.i = 1;
                P1.j = 2;
                P2.i = 1;
                P2.j = 4;
                P3.i = 1;
                P3.j = 3;
            }
            else
            {
                P0.i = 4 * n - 1;
                P0.j = 1;
                P1.i = 4 * n - 1;
                P1.j = 2;
                P2.i = 4 * n - 1;
                P2.j = 4;
                P3.i = 4 * n - 1;
                P3.j = 3;
            }

            int int_j = (int)j;
            double decimal_j = j - int_j;
            if (int_j == 0)
            {
                int_j = 4;
            }

            double decimal_i_n = INFO.i_n;
            double delta_decimal_i = decimal_i_n / 2;
            double delta_decimal_j = decimal_j * decimal_i_n;
            double offset_j = 0.5 - delta_decimal_i;

            switch (int_j)
            {
                case 1:
                    C.i = 0.5 - delta_decimal_i;
                    C.j = offset_j + delta_decimal_j;
                    break;
                case 2:
                    C.i = offset_j + delta_decimal_j;
                    C.j = 0.5 + delta_decimal_i;
                    break;
                case 3:
                    C.i = 0.5 + delta_decimal_i;
                    C.j = 1.0 - (offset_j + delta_decimal_j);
                    break;
                case 4:
                    C.i = 1.0 - (offset_j + delta_decimal_j);
                    C.j = 0.5 - delta_decimal_i;
                    break;
            }

            C.i -= 0.5;
            C.j -= 0.5;
        }


        // Get vertex coordinates for the patch type 'P'
        // [C, N, S, W, E] = HealpixGetPatchVertexCoordsP(n, i, j, INFO)
        //
        // Parameters
        // n : grid resolution
        // i : ring index
        // j : intra-ring index
        // INFO : intermediate information (output of HealpixSelectPatchClass())
        // C : intra-patch coordinates
        // N : coordinates for north vertex
        // S : coordinates for south vertex
        // W : coordinates for west vertex
        // E : coordinates for east vertex
        void HealpixGetPatchVertexCoordsP(int n, double i, double j, HealpixPointInfo INFO,
            out HealpixIndex C, out HealpixIndexInt N, out HealpixIndexInt S, out HealpixIndexInt W, out HealpixIndexInt E)
        {
            C = new HealpixIndex();
            N = new HealpixIndexInt();
            S = new HealpixIndexInt();
            W = new HealpixIndexInt();
            E = new HealpixIndexInt();

            // gradient of the border
            int grad = INFO.polar_part;

            double decimal_i = INFO.decimal_i_n;
            int int_i = INFO.int_i_n;

            int int_j = (int)(j - grad * decimal_i);
            double decimal_j = j - int_j;
            if (int_j == 0)
            {
                int_j = 4 * int_i;
            }

            if (decimal_j > (grad + 1) * decimal_i)
            {
                int north_i = int_i - 1;
                int south_i = int_i + 1;
                int north_int_j = ((int_j - grad - 1) % (4 * north_i)) + 1;
                int south_int_j = ((int_j + (grad + 1) - 1) % (4 * south_i)) + 1;
                int east_int_j = ((int_j + 1 - 1) % (4 * int_i)) + 1;

                N.i = north_i;
                N.j = north_int_j;
                S.i = south_i;
                S.j = south_int_j;
                W.i = int_i;
                W.j = int_j;
                E.i = int_i;
                E.j = east_int_j;

                double offset_j = (grad + 0.5) * decimal_i;
                C.i = decimal_i;
                C.j = decimal_j - offset_j - 0.5;
            }
            else
            {
                int curnt_i = int_i + 1;
                int south_i = int_i + 2;
                if (south_i > n)
                {
                    south_i = n;
                }
                int west_int_j = ((int_j + grad - 1) % (4 * curnt_i)) + 1;
                int east_int_j = ((int_j + grad + 1 - 1) % (4 * curnt_i)) + 1;
                int south_int_j = ((west_int_j + (grad + 1) - 1) % (4 * south_i)) + 1;

                N.i = int_i;
                N.j = int_j;
                S.i = int_i + 2;
                S.j = south_int_j;
                W.i = int_i + 1;
                W.j = west_int_j;
                E.i = int_i + 1;
                E.j = east_int_j;

                double offset_j = (grad + 0.5) * (1.0 - decimal_i);
                C.i = decimal_i - 1;
                C.j = decimal_j + offset_j - (grad + 0.5);
            }

            if (INFO.is_south_pole)
            {
                HealpixIndexInt TMP = N;
                N = S;
                S = TMP;
                N.i = 4 * n - N.i;
                S.i = 4 * n - S.i;
                W.i = 4 * n - W.i;
                E.i = 4 * n - E.i;
                C.i = -C.i;
            }
        }

        // Get vertex coordinates for the patch type 'R'
        // [C, NW, NE, SW, SE] = HealpixGetPatchVertexCoordsR(n, i, j, INFO)
        //
        // Parameters
        // n : grid resolution
        // i : ring index
        // j : intra-ring index
        // INFO : intermediate information (output of HealpixSelectPatchClass())
        // C : intra-patch coordinates
        // NW : coordinates for north-east vertex
        // NE : coordinates for north-west vertex
        // SW : coordinates for south-west vertex
        // SE : coordinates for south-east vertex
        void HealpixGetPatchVertexCoordsR(int n, double i, double j, HealpixPointInfo INFO,
            out HealpixIndex C, out HealpixIndexInt NW, out HealpixIndexInt NE, out HealpixIndexInt SW, out HealpixIndexInt SE)
        {
            C = new HealpixIndex();
            NW = new HealpixIndexInt();
            NE = new HealpixIndexInt();
            SW = new HealpixIndexInt();
            SE = new HealpixIndexInt();

            // gradient of the border
            int grad = INFO.polar_part;

            int delta_int_i_n = INFO.int_i_n - 1;
            int north_west_j = grad + grad * delta_int_i_n;
            int south_west_j = north_west_j + grad;

            int north_i_n = INFO.int_i_n;
            int south_i_n = INFO.int_i_n + 1;

            north_west_j = ((north_west_j - 1) % (4 * north_i_n)) + 1;
            int north_east_j = ((north_west_j + 1 - 1) % (4 * north_i_n)) + 1;

            south_west_j = ((south_west_j - 1) % (4 * south_i_n)) + 1;
            int south_east_j = ((south_west_j + 1 - 1) % (4 * south_i_n)) + 1;

            if (INFO.is_south_pole == false)
            {
                NW.i = north_i_n;
                NW.j = north_west_j;
                NE.i = north_i_n;
                NE.j = north_east_j;
                SW.i = south_i_n;
                SW.j = south_west_j;
                SE.i = south_i_n;
                SE.j = south_east_j;
                C.i = INFO.decimal_i_n - 0.5;
                C.j = INFO.polar_intra_part - 0.5;
            }
            else
            {
                north_i_n = 4 * n - north_i_n;
                south_i_n = 4 * n - south_i_n;
                NW.i = south_i_n;
                NW.j = south_west_j;
                NE.i = south_i_n;
                NE.j = south_east_j;
                SW.i = north_i_n;
                SW.j = north_west_j;
                SE.i = north_i_n;
                SE.j = north_east_j;
                C.i = 1.0 - INFO.decimal_i_n - 0.5;
                C.j = INFO.polar_intra_part - 0.5;
            }
        }

        // Get vertex coordinates for the patch type 'T'
        // [C, NW, NE, SW, SE] = HealpixGetPatchVertexCoordsT(n, i, j, INFO)
        //
        // Parameters
        // n : grid resolution
        // i : ring index
        // j : intra-ring index
        // INFO : intermediate information (output of HealpixSelectPatchClass())
        // C : intra-patch coordinates
        // NW : coordinates for north-east vertex
        // NE : coordinates for north-west vertex
        // SW, SE : coordinates for south vertex (always SW = SE since the patch is triangular shape)
        void HealpixGetPatchVertexCoordsT(int n, double i, double j, HealpixPointInfo INFO,
            out HealpixIndex C, out HealpixIndexInt NW, out HealpixIndexInt NE, out HealpixIndexInt SW, out HealpixIndexInt SE)
        {
            C = new HealpixIndex();
            NW = new HealpixIndexInt();
            NE = new HealpixIndexInt();
            SW = new HealpixIndexInt();
            SE = new HealpixIndexInt();

            int int_j = (int)j;
            if (int_j == 0)
            {
                int_j = 4 * n;
            }
            int east_int_j = ((int_j + 1 - 1) % (4 * INFO.int_i_n)) + 1;

            double decimal_i;
            if (INFO.is_south_pole == false)
            {
                NW.i = n;
                NW.j = int_j;
                NE.i = n;
                NE.j = east_int_j;
                SW.i = n + 1;
                SW.j = int_j;
                SE.i = SW.i;
                SE.j = SW.j;

                decimal_i = INFO.decimal_i_n;
            }
            else
            {
                SW.i = 3 * n;
                SW.j = int_j;
                SE.i = 3 * n;
                SE.j = east_int_j;
                NW.i = 3 * n - 1;
                NW.j = int_j;
                NE.i = NW.i;
                NE.j = NW.j;

                decimal_i = 1.0 - INFO.decimal_i_n;
            }

            double decimal_j = INFO.decimal_polar_intra_part;

            if (1 > INFO.decimal_i_n)
            {
                C.i = decimal_i - 0.5;
                C.j = decimal_j / (1.0 - INFO.decimal_i_n) - 0.5;
            }
            else
            {
                C.i = decimal_i - 0.5;
                C.j = decimal_j - 0.5;
            }
        }

        enum HealpixDirection{
            DIR_VERTICAL,
            DIR_HORIZONTAL,
            DIR_OBLIQUE_P,
            DIR_OBLIQUE_N
        }

        // Get adjacent pixels coordinates for NOT integer position
        void HealpixGetAdjacentVertex(int n, HealpixIndex POS,
            out HealpixIndexInt P0, out HealpixIndexInt P1, out HealpixIndexInt P2, out HealpixIndexInt P3)
        {
            HealpixPointInfo info = HealpixSelectPatchClass(n, POS.i, POS.j);
            HealpixIndex C;
            switch (info.patch_class)
            {
                case HealpixPatchClass.PATCH_CLASS_O:
                    HealpixGetPatchVertexCoordsO(n, POS.i, POS.j, info, out C, out P0, out P1, out P2, out P3);
                    break;
                case HealpixPatchClass.PATCH_CLASS_R:
                    HealpixGetPatchVertexCoordsR(n, POS.i, POS.j, info, out C, out P0, out P1, out P2, out P3);
                    break;
                case HealpixPatchClass.PATCH_CLASS_T:
                    HealpixGetPatchVertexCoordsT(n, POS.i, POS.j, info, out C, out P0, out P1, out P2, out P3);
                    break;
                case HealpixPatchClass.PATCH_CLASS_P:
                    HealpixGetPatchVertexCoordsP(n, POS.i, POS.j, info, out C, out P0, out P1, out P2, out P3);
                    break;
                case HealpixPatchClass.PATCH_CLASS_E:
                    HealpixGetPatchVertexCoordsE(n, POS.i, POS.j, info, out C, out P0, out P1, out P2, out P3);
                    break;
                case HealpixPatchClass.PATCH_CLASS_B:
                    HealpixGetPatchVertexCoordsB(n, POS.i, POS.j, info, out C, out P0, out P1, out P2, out P3);
                    break;
                case HealpixPatchClass.PATCH_CLASS_V:
                default:
                    throw new System.NotImplementedException();
            }
        }

        // Get adjacent pixels coordinates for integer position
        void HealpixGetAdjacentCoords(int n, HealpixIndexInt POS, HealpixDirection dir,
            out HealpixIndexInt P0, out HealpixIndexInt P1)
        {
            // ここで、上下左右に座標を少しづつ動かしてHealpixGetAdjacentVertexを呼んで、
            // ４方向のペアを求める
            P0 = new HealpixIndexInt();
            P1 = new HealpixIndexInt();

            switch (dir)
            {
                case HealpixDirection.DIR_VERTICAL:
                    break;
                case HealpixDirection.DIR_HORIZONTAL:
                    break;
                case HealpixDirection.DIR_OBLIQUE_P:
                    break;
                case HealpixDirection.DIR_OBLIQUE_N:
                    break;

            }
        }

    }
}
