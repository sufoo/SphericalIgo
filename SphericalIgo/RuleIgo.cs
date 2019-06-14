using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace SphericalIgo
{
    public class RuleIgo : GameRule
    {
        int retrieve_count_black = 0;
        int retrieve_count_white = 0;
        protected PieceKind[] TerritoryArrangement = null;

        public RuleIgo(Healpix.Healpix pix)
            : base(pix)
        {
            int n = GetMaxIndex();
            TerritoryArrangement = new PieceKind[n];
            for (int i = 0; i < n; i++)
            {
                TerritoryArrangement[i] = PieceKind.PIECE_EMPTY;
            }
        }
        protected override void CreatePiecePositionTable()
        {
            PiecePositions = new PiecePositionsType[GetMaxIndex()];
            int max_ring = GetMaxRingIndex();
            for (int i = 1, m = 0; i <= max_ring; i++)
            {
                int max_intra_ring = GetMaxIntraRingIndexAt(i);
                if (max_intra_ring == 1)
                {
                    PiecePositions[m++] = new PiecePositionsType()
                    {
                        index = new Healpix.HealpixIndex() { i = i, j = 1 },
                        position_polar = new Healpix.HealpixPolarCoord() { phi = 0, theta = (i == 1) ? 0 : Math.PI },
                        position_cart = new Vector3D() { X = 0, Y = 0, Z = (i == 1) ? 1 : -1 }
                    };
                    continue;
                }
                for (int j = 1; j <= max_intra_ring; j++, m++)
                {
                    Healpix.HealpixIndex idx = new Healpix.HealpixIndex() { i = i, j = j };
                    Healpix.HealpixIndex idx_act = new Healpix.HealpixIndex() { i = i - 1, j = j - 0.5 };
                    Healpix.HealpixPolarCoord polar = pixelization.GetSphCoord(idx_act);
                    Point3D point = pixelization.GetCartesianCoord(polar);
                    PiecePositions[m] = new PiecePositionsType()
                    {
                        index = idx,
                        position_polar = polar,
                        position_cart = new Vector3D() { X = point.X, Y = point.Y, Z = point.Z }
                    };
                }
            }
        }
        public override String GetName()
        {
            return "Spherical Igo";
        }
        public override void SetCursorCoordinates(double theta, double phi)
        {
            Healpix.HealpixPolarCoord polar = new Healpix.HealpixPolarCoord()
            {
                theta = theta,
                phi = phi
            };
            Healpix.HealpixIndex index = pixelization.GetHealpixIndex(polar);
            int base_idx_i = (int)(index.i + 1 + 0.5);
            int begin_idx_i = (base_idx_i <= 1) ? 1 : base_idx_i - 1;
            int end_idx_i = (base_idx_i >= GetMaxRingIndex()) ? GetMaxRingIndex() : base_idx_i + 1;
            Point3D point = pixelization.GetCartesianCoord(polar);
            Healpix.HealpixIndex idx = GetNearestPiece(new Vector3D() { X = point.X, Y = point.Y, Z = point.Z }, begin_idx_i, end_idx_i);
            cursor_idx_i = (int)idx.i;
            cursor_idx_j = (int)idx.j;
        }
        protected override int GetMaxIntraRingIndexAt(int i)
        {
            int n = pixelization.Resolution;
            if (i == 1)
            {
                return 1;
            }
            if (i == (4 * n + 1))
            {
                return 1;
            }
            if (i < n)
            {
                return 4 * (i - 1);
            }
            if (i > (3 * n))
            {
                return 4 * (4 * n - i + 1);
            }
            else
            {
                return 4 * n;
            }
        }
        protected override int GetMaxRingIndex()
        {
            int n = pixelization.Resolution;
            return 4 * n + 1;
        }
        public override String GetStatus()
        {
            int num_white = 0;
            int num_black = 0;
            foreach (PieceKind kind in TerritoryArrangement)
            {
                if (kind == PieceKind.PIECE_WHITE)
                {
                    num_white++;
                }
                else if (kind == PieceKind.PIECE_BLACK)
                {
                    num_black++;
                }
            }

            return "Black = " + num_black + "-" + retrieve_count_black + " = " + (num_black - retrieve_count_black)
                + ", White = " + num_white + "-" + retrieve_count_white + " = " + (num_white - retrieve_count_white);
        }
        public override PieceOperations GetNextOperation(PieceOperations state)
        {
            switch (state)
            {
                case PieceOperations.OP_PUT_PIECE_BLACK:
                    return PieceOperations.OP_PUT_PIECE_WHITE;
                case PieceOperations.OP_PUT_PIECE_WHITE:
                    return PieceOperations.OP_PUT_PIECE_BLACK;
                default:
                    return state;
            }

        }
        public override bool RetrievePiece()
        {
            int idx = GetCursorIndex();
            switch(PieceArrangement[idx]){
                case PieceKind.PIECE_EMPTY:
                    return false;
                case PieceKind.PIECE_BLACK:
                    retrieve_count_black++;
                    PieceArrangement[idx] = PieceKind.PIECE_EMPTY;
                    break;
                case PieceKind.PIECE_WHITE:
                    retrieve_count_white++;
                    PieceArrangement[idx] = PieceKind.PIECE_EMPTY;
                    break;
            }
            return true;
        }
        public override int RetrievePiece(PieceKind kind, int val) 
        {
            switch (kind)
            {
                case PieceKind.PIECE_BLACK:
                    retrieve_count_black += val;
                    if (retrieve_count_black < 0) retrieve_count_black = 0;
                    return retrieve_count_black;
                case PieceKind.PIECE_WHITE:
                    retrieve_count_white += val;
                    if (retrieve_count_white < 0) retrieve_count_white = 0;
                    return retrieve_count_white;
            }
            return 0;
        }
        public override bool PutTerritory(PieceKind kind) 
        {
            int idx = GetCursorIndex();
            switch (PieceArrangement[idx])
            {
                case PieceKind.PIECE_BLACK:
                case PieceKind.PIECE_WHITE:
                    return false;
                case PieceKind.PIECE_EMPTY:
                    if (kind != PieceKind.PIECE_EMPTY)
                    {
                        if (TerritoryArrangement[idx] != PieceKind.PIECE_EMPTY)
                        {
                            return false;
                        }
                    }
                    TerritoryArrangement[idx] = kind;
                    break;
            }
            return true;
        }
    }
}
