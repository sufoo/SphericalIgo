using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace SphericalIgo
{
    public enum PieceKind{
        PIECE_EMPTY = 0,
        PIECE_WHITE,
        PIECE_BLACK
    }

    public abstract class GameRule
    {
        protected struct PiecePositionsType{
            public Healpix.HealpixIndex index;
            public Healpix.HealpixPolarCoord position_polar;
            public Vector3D position_cart;
        };

        protected Healpix.Healpix pixelization;
        protected PieceKind[] PieceArrangement= null;
        protected int cursor_idx_i = 0;
        protected int cursor_idx_j = 0;
        public delegate bool PieceManipulator(PieceOperations op);
        protected PieceManipulator ManipulatePiece;
        
        protected PiecePositionsType[] PiecePositions = null;

        public GameRule(Healpix.Healpix pix)
        {
            pixelization = pix;
            
            int n = GetMaxIndex();
            PieceArrangement = new PieceKind[n];
            for(int i = 0; i < n; i++){
                PieceArrangement[i] = PieceKind.PIECE_EMPTY;
            }

            CreatePiecePositionTable();
        }
        public void SetPieceManipulator(PieceManipulator handle)
        {
            ManipulatePiece = handle;
        }
        protected Healpix.HealpixIndex GetNearestPiece(Vector3D vec)
        {
            double max_val = 0.0;
            Healpix.HealpixIndex? best_idx = null;
            foreach (PiecePositionsType element in PiecePositions)
            {
                double val = Vector3D.DotProduct(vec, element.position_cart);
                if (val > max_val)
                {
                    max_val = val;
                    best_idx = element.index;
                }
            }
            if (best_idx == null)
            {
                return new Healpix.HealpixIndex() { i = 1, j = 1 };
            }
            else
            {
                return (Healpix.HealpixIndex)best_idx;
            }
        }
        protected Healpix.HealpixIndex GetNearestPiece(Vector3D vec, int begin_i, int end_i)
        {
            int begin_n = GetIndexAt(begin_i, 1);
            int end_n = GetIndexAt(end_i, GetMaxIntraRingIndexAt(end_i));
            double max_val = 0.0;
            Healpix.HealpixIndex? best_idx = null;
            for(int i = begin_n; i <= end_n; i++){
                PiecePositionsType element = PiecePositions[i];
                double val = Vector3D.DotProduct(vec, element.position_cart);
                if (val > max_val)
                {
                    max_val = val;
                    best_idx = element.index;
                }
            }
            if (best_idx == null)
            {
                return new Healpix.HealpixIndex() { i = 1, j = 1 };
            }
            else
            {
                return (Healpix.HealpixIndex)best_idx;
            }
        }
#if false
        public virtual void SetCursorCoordinates(Healpix.HealpixIndex idx)
        {
            cursor_idx_i = (int)idx.i;
            cursor_idx_j = (int)idx.j;
        }
#endif
        public virtual void SetCursorCoordinates(double theta, double phi)  // slow version (all search)
        {
            Point3D point = pixelization.GetCartesianCoord(new Healpix.HealpixPolarCoord()
            {
                theta = theta,
                phi = phi
            });
            Healpix.HealpixIndex idx = GetNearestPiece(new Vector3D() { X = point.X, Y = point.Y, Z = point.Z });
            cursor_idx_i = (int)idx.i;
            cursor_idx_j = (int)idx.j;
        }
        public Healpix.HealpixPolarCoord GetRegularizedCursorCoordinates()
        {
            return PiecePositions[GetCursorIndex()].position_polar;
        }
        public String GetCursorCoordinatesText()
        {
            return "i = " + cursor_idx_i + ", j = " + cursor_idx_j + ", n = " + GetCursorIndex();
        }
        public bool PutPiece(PieceKind kind)
        {
            int idx = GetCursorIndex();
            if (kind != PieceKind.PIECE_EMPTY)
            {
                if (PieceArrangement[idx] != PieceKind.PIECE_EMPTY)
                {
                    return false;
                }
            }
            PieceArrangement[idx] = kind;
            return true;
        }
        public PieceKind GetPiece()
        {
            return PieceArrangement[GetCursorIndex()];
        }
        public int GetMaxIndex()
        {
            int n = pixelization.Resolution;
            int max_ring = GetMaxRingIndex();
            int idx = 0;
            for (int i = 1; i <= max_ring; i++)
            {
                idx += GetMaxIntraRingIndexAt(i);
            }

            return idx;
        }
        public int GetIndexAt(int idx_i, int idx_j) // input idx_i, idx_j start with 1. returned 1D-index starts with 0.
        {
            int n = pixelization.Resolution;
            int idx = 0;
            for (int i = 1; i < idx_i; i++)
            {
                idx += GetMaxIntraRingIndexAt(i);
            }

            return idx + idx_j - 1;
        }
        public int GetCursorIndex()
        {
            return GetIndexAt(cursor_idx_i, cursor_idx_j);
        }
        public void SetCursorIndex(int n)
        {
            int idx_i = 0, idx_j = 0;
            int max_ring = GetMaxRingIndex();
            int sum = 0;
            for (idx_i = 1; idx_i <= max_ring; idx_i++)
            {
                idx_j = n - sum + 1;
                int intra_ring_idx = GetMaxIntraRingIndexAt(idx_i);
                if(idx_j <= intra_ring_idx){
                    break;
                }
                sum += intra_ring_idx;
            }

            if ((idx_i <= max_ring) && (idx_j > 0))
            {
                cursor_idx_i = idx_i;
                cursor_idx_j = idx_j;
            }
        }

        public abstract String GetName();
        public abstract String GetStatus();
        public abstract PieceOperations GetNextOperation(PieceOperations state);
        protected abstract void CreatePiecePositionTable();
        protected abstract int GetMaxIntraRingIndexAt(int i);
        protected abstract int GetMaxRingIndex();

        public virtual void SetInitialPieces() { }
        public virtual bool FlipPiece() { return false; }
        public virtual bool RetrievePiece() { return false; }
        public virtual int RetrievePiece(PieceKind kind, int val) { return 0; }
        public virtual bool PutTerritory(PieceKind kind) { return false; }
    }
}
