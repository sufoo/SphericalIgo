using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace SphericalIgo
{
    public class RuleOthello : GameRule
    {
        public RuleOthello(Healpix.Healpix pix)
            : base(pix)
        {
        }
        protected override void CreatePiecePositionTable()
        {
            PiecePositions = new PiecePositionsType[GetMaxIndex()];
            int max_ring = GetMaxRingIndex();
            for (int i = 1, m = 0; i <= max_ring; i++)
            {
                int max_intra_ring = GetMaxIntraRingIndexAt(i);
                for (int j = 1; j <= max_intra_ring; j++, m++)
                {
                    Healpix.HealpixIndex idx = new Healpix.HealpixIndex() { i = i, j = j };
                    Healpix.HealpixPolarCoord polar = pixelization.GetSphCoord(idx);
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
        public override void SetInitialPieces()
        {
            base.SetInitialPieces();

            cursor_idx_i = 1;
            cursor_idx_j = 1;
            ManipulatePiece(PieceOperations.OP_PUT_PIECE_BLACK);
            cursor_idx_j = 2;
            ManipulatePiece(PieceOperations.OP_PUT_PIECE_WHITE);
            cursor_idx_j = 3;
            ManipulatePiece(PieceOperations.OP_PUT_PIECE_BLACK);
            cursor_idx_j = 4;
            ManipulatePiece(PieceOperations.OP_PUT_PIECE_WHITE);
        }
        public override String GetName()
        {
            return "Spherical Othello";
        }
        public override void SetCursorCoordinates(double theta, double phi)
        {
            Healpix.HealpixPolarCoord polar = new Healpix.HealpixPolarCoord()
            {
                theta = theta,
                phi = phi
            };
            Healpix.HealpixIndex index = pixelization.GetHealpixIndex(polar);
            int base_idx_i = (int)(index.i + 0.5);
            int begin_idx_i = (base_idx_i <= 1) ? 1 : base_idx_i - 1;
            int end_idx_i = (base_idx_i >= GetMaxRingIndex()) ? GetMaxRingIndex() : base_idx_i + 1;
            Point3D point = pixelization.GetCartesianCoord(polar);
            Healpix.HealpixIndex idx = GetNearestPiece(new Vector3D() { X = point.X, Y = point.Y, Z = point.Z }, begin_idx_i, end_idx_i);
            cursor_idx_i = (int)idx.i;
            cursor_idx_j = (int)idx.j;
        }
        protected override int GetMaxIntraRingIndexAt(int i){
            int n = pixelization.Resolution;
            if (i < n)
            {
                return 4 * i;
            }
            if (i > (3 * n))
            {
                return 4 * (4 * n - i);
            }
            else
            {
                return 4 * n;
            }
        }
        protected override int GetMaxRingIndex()
        {
            int n = pixelization.Resolution;
            return 4 * n - 1;
        }
        public override String GetStatus()
        {
            int num_white = 0;
            int num_black = 0;
            foreach (PieceKind kind in PieceArrangement)
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

            return "Black = " + num_black + ", White = " + num_white;
        }
        public override PieceOperations GetNextOperation(PieceOperations state)
        {
            switch (state)
            {
                case PieceOperations.OP_PUT_PIECE_BLACK:
                case PieceOperations.OP_PUT_PIECE_WHITE:
                    return PieceOperations.OP_FLIP_PIECE;
                default:
                    return state;
            }

        }
        public override bool FlipPiece()
        {
            int idx = GetCursorIndex();
            switch (PieceArrangement[idx])
            {
                case PieceKind.PIECE_EMPTY:
                    return false;
                case PieceKind.PIECE_BLACK:
                    PieceArrangement[idx] = PieceKind.PIECE_WHITE;
                    break;
                case PieceKind.PIECE_WHITE:
                    PieceArrangement[idx] = PieceKind.PIECE_BLACK;
                    break;
            }
            return true;
        }

        // ------------------------------
        // Implementation of auto filp
        // ------------------------------

        enum NeighborDirection{
            PLUS_I,
            MINUS_I,
            PLUS_J,
            MINUS_J,
            PLUS_I_PLUS_J,
            PLUS_I_MINUS_J,
            MINUS_I_PLUS_J,
            MINUS_I_MINUS_J
        };

        bool GetNeighborCoord(NeighborDirection direction, int idx_i, int idx_j, ref int idx_neighbor_i, ref int idx_neighbor_j)
        {
            int n = pixelization.Resolution;
            int max_j = GetMaxIntraRingIndexAt(idx_i);

            switch(direction){
                case NeighborDirection.PLUS_I:
                case NeighborDirection.MINUS_I:
                    return false;
                case NeighborDirection.PLUS_J:
                    idx_neighbor_i = idx_i;
                    if(idx_j + 1 > max_j){
                        idx_neighbor_j = 1;
                    }else{
                        idx_neighbor_j = idx_j + 1;
                    }
                    return true;                    
                case NeighborDirection.MINUS_J:
                    idx_neighbor_i = idx_i;
                    if(idx_j == 1){
                        idx_neighbor_j = max_j;
                    }else{
                        idx_neighbor_j = idx_j - 1;
                    }
                    return true;                    
                case NeighborDirection.PLUS_I_PLUS_J:
                case NeighborDirection.PLUS_I_MINUS_J:
                case NeighborDirection.MINUS_I_PLUS_J:
                case NeighborDirection.MINUS_I_MINUS_J:
                    return false;
                default:
                    return false;
            }
        }

        bool ConfirmCanPlacePiece(PieceKind piece)
        {
            return false;
        }

        void AutoFlip(PieceKind piece)
        {
        }
    }
}
