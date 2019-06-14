using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using Healpix;
using Petzold.Media3D;
using System.ComponentModel;

namespace SphericalIgo
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Load settings and initialize
            //Setting.Load(true);
            Setting.Load(false);
            if (Setting.param.WindowWidth != 0)
            {
                Width = Setting.param.WindowWidth;
                Height = Setting.param.WindowHeight;
            }

            UpdateDisplay();
        }

        private int current_resolution = 0;
        private int current_quality = 0;
        private GameKind current_game = GameKind.GAMEKIND_IGO;

        private void UpdateDisplay()
        {
            if (current_game != Setting.param.Kind
                || current_quality != Setting.param.Quality
                || current_resolution != Setting.param.Resolution)
            {
                // ------------------------------
                // Change mesh resolution
                // ------------------------------
                int div;
                switch (Setting.param.Quality)
                {
                    case 1:
                        div = 30;
                        break;
                    case 2:
                        div = 60;
                        break;
                    case 3:
                    default:
                        div = 90;
                        break;
                }
                double SamplingInterval = Math.PI / div;
                UpdateMeshResolution((short)(1 << Setting.param.Resolution), SamplingInterval);

                // ------------------------------
                // Create game rule
                // ------------------------------
                if (Setting.param.Kind == GameKind.GAMEKIND_OTHELLO)
                {
                    rule = new RuleOthello(pixelization);
                }
                else
                {
                    rule = new RuleIgo(pixelization);
                }
                Title = rule.GetName();

                // ------------------------------
                // Remove all pieces
                // ------------------------------
                PieceArray2D = new UIElement[rule.GetMaxIndex()];
                PieceArray3D = new Visual3D[rule.GetMaxIndex()];
                canvas_projection_piece.Children.Clear();
                PieceContainer.Children.Clear();

                TerritoryArray2D = new UIElement[rule.GetMaxIndex()];
                TerritoryArray3D = new Visual3D[rule.GetMaxIndex()];
                canvas_projection_territory.Children.Clear();
                TerritoryContainer.Children.Clear();

                // ------------------------------
                // Select UI panel
                // ------------------------------
#if false
                UserInterfaceArea.Children.Clear();
                if (Setting.param.Kind == GameKind.GAMEKIND_IGO)
                {
                    ConsoleIgo control = new ConsoleIgo();
                    UserInterfaceArea.Children.Add(control);
                    UserInterface = control;
                }
                else
                {
                    ConsoleOthello control = new ConsoleOthello();
                    UserInterfaceArea.Children.Add(control);
                    UserInterface = control;
                }
#else
                if (Setting.param.Kind == GameKind.GAMEKIND_IGO)
                {
                    UserInterface = PanelIgo;
                    TabItemIgo.IsSelected = true;
                }
                else
                {
                    UserInterface = PanelOthello;
                    TabItemOthello.IsSelected = true;
                }
#endif
                UserInterface.SetOnConfigurationsChanged(delegate(object sender, System.EventArgs e) { UpdateDisplay(); });
                UserInterface.SetGameRule(rule);

                // ------------------------------
                // Prepare initial piece arrangements
                // ------------------------------
                rule.SetPieceManipulator(ManipulatePiece);
                rule.SetInitialPieces();

                // ------------------------------
                // Remember current settings
                // ------------------------------
                current_resolution = Setting.param.Resolution;
                current_quality = Setting.param.Quality;
                current_game = Setting.param.Kind;
            }

            SphereMaterial.Brush = new SolidColorBrush(Setting.param.ForeColor);
            MapBase.Fill = SphereMaterial.Brush;
        }

        private void CanvasSizeChanged(
            Object sender,
            SizeChangedEventArgs e)
        {
            double Width = canvas_projection.RenderSize.Width;
            double Height = canvas_projection.RenderSize.Height;
            double x_ratio = Width / map_org_fig_size_x;
            double y_ratio = Height / map_org_fig_size_y;
            Matrix mat = new Matrix();
            if (y_ratio > x_ratio)
            {
                mat.Scale(x_ratio, x_ratio);
                switch (VerticalContentAlignment)
                {
                    case VerticalAlignment.Top:
                        mat.Translate(0, 0);
                        break;
                    case VerticalAlignment.Bottom:
                        mat.Translate(0, Height - x_ratio * map_org_fig_size_y);
                        break;
                    case VerticalAlignment.Center:
                    default:
                        mat.Translate(0, (Height - x_ratio * map_org_fig_size_y) / 2);
                        break;
                }
            }
            else
            {
                mat.Scale(y_ratio, y_ratio);
                switch (HorizontalContentAlignment)
                {
                    case HorizontalAlignment.Left:
                        mat.Translate(0, 0);
                        break;
                    case HorizontalAlignment.Right:
                        mat.Translate(Width - y_ratio * map_org_fig_size_x, 0);
                        break;
                    case HorizontalAlignment.Center:
                    default:
                        mat.Translate((Width - y_ratio * map_org_fig_size_x) / 2, 0);
                        break;
                }
            }
            canvas_transform.Matrix = mat;
        }

        IConsole UserInterface = null;
        GameRule rule = null;
        Healpix.Healpix pixelization = null;
        Healpix.HealpixBorder border = null;
        static ProjectionMapCoordsTrans prjmap_coords = new ProjectionMapCoordsTrans();
        static ProjectionMapCoordsTrans prjmap_coords_saved = new ProjectionMapCoordsTrans();
        const int map_org_fig_size_x = 360;
        const int map_org_fig_size_y = 180;
        const double map_pole_enlarge_scale = 0.7;

        void UpdateMeshResolution(short resolution, double sampling_interval)
        {
            // ------------------------------
            // Adjust cursor size
            // ------------------------------
            CursorScaleB.ScaleX = CursorScaleB.ScaleY = CursorScaleB.ScaleZ = Math.PI / 8 / resolution;

            // ------------------------------
            // Create pixelization scheme
            // ------------------------------
            pixelization = new Healpix.Healpix();
            pixelization.Resolution = resolution;

            // ------------------------------
            // Create border lines
            // ------------------------------
            border = new HealpixBorder();
            try
            {
                border.SamplingInterval = sampling_interval;
                border.Resolution = resolution;
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
            }

            Visual3DCollection elements = BorderContainer.Children;
            elements.Clear();
            int n = border.GetNumberOfBorderLines();

            for (int i = 0; i < n; i++)
            {
                WirePolyline line = new WirePolyline();
                line.Points = border.GetAsCartesianCoord(i);
                line.Color = Colors.Black;
                line.Thickness = border.GetBorderThickness(i);
                elements.Add(line);
            }

            UpdateMapMesh();
        }

        private void UpdateMapMesh()
        {
            int n = border.GetNumberOfBorderLines();

            Brush map_brush = new SolidColorBrush(Colors.Black);
            UIElementCollection canvas_children = canvas_projection_mesh.Children;
            canvas_children.Clear();

#if true
            for (int i = 0; i < n; i++)
            {
                double StrokeThickness = border.GetBorderThickness(i) * 0.5;
                PointCollection points = border.GetAsSphericalCoord(i);
                PointCollection pointDeg = new PointCollection(points.Count);
                foreach (Point element in points)
                {
                    double azimuth, elevation;
                    SphToMapTransform(element.X, element.Y, out azimuth, out elevation);
                    pointDeg.Add(new Point()
                    {
                        X = azimuth,
                        Y = elevation
                    });
                }

                int num_begin = 0;
                int num_end = pointDeg.Count;
                for (int j = 0; j < num_end; j++)
                {
                    if ((j == (num_end - 1))
                        || (Math.Abs(pointDeg[j].X - pointDeg[j + 1].X) > 100.0)
                        || (Math.Abs(pointDeg[j].Y - pointDeg[j + 1].Y) > 90.0))
                    {
                        int length = j - num_begin + 1;
                        if (length == num_end)
                        {
                            Polyline segment = new Polyline();
                            segment.Points = pointDeg;
                            segment.Stroke = map_brush;
                            segment.StrokeThickness = StrokeThickness;

                            canvas_children.Add(segment);
                        }
                        else if (length >= 2)
                        {
                            PointCollection pointDegPart = new PointCollection(length);
                            for (int k = num_begin; k <= j; k++)
                            {
                                pointDegPart.Add(pointDeg[k]);
                            }
                            Polyline segment = new Polyline();
                            segment.Points = pointDegPart;
                            segment.Stroke = map_brush;
                            segment.StrokeThickness = StrokeThickness;

                            canvas_children.Add(segment);
                        }

                        num_begin = j + 1;
                    }
                }
            }
#else
            for (int i = 0; i < n; i++)
            {
                double StrokeThickness = border.GetBorderThickness(i) * 0.5;
                PointCollection points = border.GetAsSphericalCoord(i);
                PointCollection pointDeg = new PointCollection(points.Count);
                foreach (Point element in points)
                {
                    double azimuth, elevation;
                    SphToMapTransform(element.X, element.Y, out azimuth, out elevation);
                    pointDeg.Add(new Point()
                    {
                        X = azimuth,
                        Y = elevation
                    });
                }

                int discontinuous_idx = -1;
                int last_num = pointDeg.Count;
                for (int j = 0; j < last_num - 1; j++)
                {
                    if ((Math.Abs(pointDeg[j].X - pointDeg[j + 1].X) > 100.0)
                        || (Math.Abs(pointDeg[j].Y - pointDeg[j + 1].Y) > 90.0))
                    {
                        discontinuous_idx = j;
                        break;
                    }
                }

                if (discontinuous_idx < 0)
                {
                    Polyline segment = new Polyline();
                    segment.Points = pointDeg;
                    segment.Stroke = map_brush;
                    segment.StrokeThickness = StrokeThickness;

                    canvas_children.Add(segment);
                }
                else
                {
                    PointCollection pointDeg1 = new PointCollection(discontinuous_idx + 1);
                    PointCollection pointDeg2 = new PointCollection(pointDeg.Count - (discontinuous_idx + 1));
                    for (int j = 0; j <= discontinuous_idx; j++)
                    {
                        pointDeg1.Add(pointDeg[j]);
                    }
                    for (int j = discontinuous_idx + 1; j < last_num; j++)
                    {
                        pointDeg2.Add(pointDeg[j]);
                    }

                    Polyline segment = new Polyline();
                    segment.Points = pointDeg1;
                    segment.Stroke = map_brush;
                    segment.StrokeThickness = StrokeThickness;

                    canvas_children.Add(segment);

                    segment = new Polyline();
                    segment.Points = pointDeg2;
                    segment.Stroke = map_brush;
                    segment.StrokeThickness = StrokeThickness;

                    canvas_children.Add(segment);
                }
            }
#endif
        }

        private void RedrawAllPieces2D()
        {
            // ------------------------------
            // Remove all pieces
            // ------------------------------
            canvas_projection_piece.Children.Clear();
            int n = rule.GetMaxIndex();
            for (int i = 0; i < n; i++)
            {
                PieceArray2D[i] = null;
            }

            // ------------------------------
            // Redraw all pieces
            // ------------------------------
            for (int i = 0; i < n; i++)
            {
                rule.SetCursorIndex(i);
                PieceKind piece = rule.GetPiece();
                if (piece == PieceKind.PIECE_EMPTY)
                {
                    continue;
                }

                // ------------------------------
                // 2D map cursor
                // ------------------------------
                Healpix.HealpixPolarCoord PolarCoord = rule.GetRegularizedCursorCoordinates();
                double azimuth, elevation;
                SphToMapTransform(PolarCoord.theta, PolarCoord.phi, out azimuth, out elevation);
                MapCursorTranslateB.X = azimuth;
                MapCursorTranslateB.Y = elevation;
                MapCursorB.Visibility = Visibility.Visible;

                switch (piece)
                {
                    case PieceKind.PIECE_BLACK:
                        SetPiece2D(Colors.Black);
                        break;
                    case PieceKind.PIECE_WHITE:
                        SetPiece2D(Colors.White);
                        break;
                }
            }
        }

        private void UpdatePiecesPositions2D()
        {
            int n = rule.GetMaxIndex();
            for (int i = 0; i < n; i++)
            {
                if (PieceArray2D[i] != null)
                {
                    rule.SetCursorIndex(i);

                    Healpix.HealpixPolarCoord PolarCoord = rule.GetRegularizedCursorCoordinates();
                    double azimuth, elevation;
                    SphToMapTransform(PolarCoord.theta, PolarCoord.phi, out azimuth, out elevation);

                    Ellipse piece = (Ellipse)PieceArray2D[i];
                    TranslateTransform trans = (TranslateTransform)piece.RenderTransform;
                    trans.X = azimuth;
                    trans.Y = elevation;
                    trans.X -= piece.Width / 2;
                    trans.Y -= piece.Height / 2;
                }
                if (TerritoryArray2D[i] != null)
                {
                    rule.SetCursorIndex(i);

                    Healpix.HealpixPolarCoord PolarCoord = rule.GetRegularizedCursorCoordinates();
                    double azimuth, elevation;
                    SphToMapTransform(PolarCoord.theta, PolarCoord.phi, out azimuth, out elevation);

                    Rectangle piece = (Rectangle)TerritoryArray2D[i];
                    TranslateTransform trans = (TranslateTransform)piece.RenderTransform;
                    trans.X = azimuth;
                    trans.Y = elevation;
                    trans.X -= piece.Width / 2;
                    trans.Y -= piece.Height / 2;
                }
            }
        }

        public void ViewPortOnMouseMove(Object sender, MouseEventArgs e)
        {
            Point PositionOnScreen = e.GetPosition(viewport);
            double HalfX = viewport.ActualWidth / 2;
            double HalfY = viewport.ActualHeight / 2;
            PositionOnScreen.X = (PositionOnScreen.X - HalfX) / HalfX;
            PositionOnScreen.Y = (PositionOnScreen.Y - HalfY) / HalfX;

            PerspectiveCamera camera = viewport.Camera as PerspectiveCamera;
            double FieldOfView = camera.FieldOfView * Math.PI / 180;
            Point3D origin = camera.Position;
            Vector3D direction = camera.LookDirection;
            Vector3D screen_x_dir = Vector3D.CrossProduct(direction, camera.UpDirection);
            Vector3D screen_y_dir = Vector3D.CrossProduct(direction, screen_x_dir);

            direction.Normalize();
            screen_x_dir.Normalize();
            screen_y_dir.Normalize();

            Vector3D MouseDirection = direction + Math.Tan(FieldOfView / 2) *
                (PositionOnScreen.X * screen_x_dir + PositionOnScreen.Y * screen_y_dir);

            Point3D pos1 = trackball.Transform.Transform(origin);
            Point3D pos2 = trackball.Transform.Transform(origin + MouseDirection);
            Point3D? intersection = GetSphereIntersection(pos1, pos2 - pos1, new Point3D(0, 0, 0), 1.0);
            DisplayCursor(intersection);
        }

        bool IsMapDraging = false;
        double MapDragBeginTheta;
        double MapDragBeginPhi;
        Cursor SavedCursor = Cursors.Arrow;

        private void viewport_MouseRightButtonDown(Object sender, MouseButtonEventArgs e)
        {
            IsMapDraging = true;
            SavedCursor = Cursor;
            Cursor = Cursors.ScrollAll;

            Point PositionOnScreen = e.GetPosition(canvas_projection);
            MapToSphTransform(PositionOnScreen.X, PositionOnScreen.Y, out MapDragBeginTheta, out MapDragBeginPhi, false);
            prjmap_coords_saved.Copy(prjmap_coords);    // save current coordinate transform
        }

        private void viewport_MouseRightButtonUp(Object sender, MouseButtonEventArgs e)
        {
            IsMapDraging = false;
            Cursor = SavedCursor;

            Point PositionOnScreen = e.GetPosition(canvas_projection);
            double theta, phi;
            MapToSphTransform(PositionOnScreen.X, PositionOnScreen.Y, out theta, out phi, false);
            if ((MapDragBeginTheta == theta) && (MapDragBeginPhi == phi))
            {
                prjmap_coords.SetPolePositionAbsolute(theta, phi);
            }
            UpdateMapMesh();
            UpdatePiecesPositions2D();
        }

        public void MapOnMouseMove(Object sender, MouseEventArgs e)
        {
            Point PositionOnScreen = e.GetPosition(canvas_projection);
            double theta, phi;
            MapToSphTransform(PositionOnScreen.X, PositionOnScreen.Y, out theta, out phi);
            Point3D pos = HealpixBorder.GetCartesianCoord(theta, phi);
            DisplayCursor(pos);

            if (IsMapDraging)
            {
                MapToSphTransform(PositionOnScreen.X, PositionOnScreen.Y, out theta, out phi, false);
                prjmap_coords.Copy(prjmap_coords_saved);
                prjmap_coords.SetPolePositionRelative(MapDragBeginTheta, MapDragBeginPhi, theta, phi);
                UpdatePiecesPositions2D();
            }
        }

        private void DisplayCursor(Point3D? CursorPos)
        {
            if (CursorPos != null)
            {
                // ------------------------------
                // Discretization
                // ------------------------------
                Point3D CursorAPos = (Point3D)CursorPos;
                double theta, phi;
                HealpixBorder.GetSphericalCoord(CursorAPos, out theta, out phi);
                rule.SetCursorCoordinates(theta, phi);

                // ------------------------------
                // Spherical cursor
                // ------------------------------
                CursorRotateA.Angle = Math.Acos(CursorAPos.Z) * 180 / Math.PI;
                CursorRotateA.Axis = new Vector3D()
                {
                    X = -CursorAPos.Y,
                    Y = CursorAPos.X,
                    Z = 0
                };

                // ------------------------------
                // 2D map cursor
                // ------------------------------
                HealpixBorder.GetSphericalCoord((Point3D)CursorPos, out theta, out phi);
                double azimuth, elevation;
                SphToMapTransform(theta, phi, out azimuth, out elevation);
                MapCursorTranslateA.X = azimuth;
                MapCursorTranslateA.Y = elevation;
                MapCursorA.Visibility = Visibility.Visible;
            }
            else
            {
                //SphCursor.IsSealed = true;
                MapCursorA.Visibility = Visibility.Hidden;
                MapCursorB.Visibility = Visibility.Hidden;
            }

            {
                HealpixPolarCoord polar = rule.GetRegularizedCursorCoordinates();
                Point3D CursorBPos = pixelization.GetCartesianCoord(polar);

                // ------------------------------
                // Spherical cursor
                // ------------------------------
                CursorRotateB.Angle = Math.Acos(CursorBPos.Z) * 180 / Math.PI;
                CursorRotateB.Axis = new Vector3D()
                {
                    X = -CursorBPos.Y,
                    Y = CursorBPos.X,
                    Z = 0
                };

                // ------------------------------
                // 2D map cursor
                // ------------------------------
                double theta, phi;
                double azimuth, elevation;
                theta = polar.theta;
                phi = polar.phi;
                if (theta < 0) theta += 2.0 * Math.PI;
                if (phi < 0) phi += 2.0 * Math.PI;
                SphToMapTransform(theta, phi, out azimuth, out elevation);
                MapCursorTranslateB.X = azimuth;
                MapCursorTranslateB.Y = elevation;
                MapCursorB.Visibility = Visibility.Visible;

                // ------------------------------
                // Show coordinates
                // ------------------------------
                UserInterface.UpdateDispCoordinate();
            }
        }

        private static void SphToMapTransform(double theta, double phi, out double azimuth, out double elevation, bool enable_move_pole = true)
        {
            if (enable_move_pole)
            {
                prjmap_coords.Transform(ref theta, ref phi);
            }

            azimuth = phi * 180.0 / Math.PI;
#if false
            elevation = theta * 180.0 / Math.PI;
#else
            double angle = (theta - Math.PI / 2) * map_pole_enlarge_scale + Math.PI / 2;
            double scale = Math.Cos(Math.PI / 2 * (1.0 - map_pole_enlarge_scale));
            elevation = (1.0 - (Math.Cos(angle) / scale)) * 90.0;
#endif

            if (elevation < 0) elevation += 360;
            if (azimuth < 0) azimuth += 360;
            if (elevation > 360) elevation -= 360;
            if (azimuth > 360) azimuth -= 360;
        }

        private static void MapToSphTransform(double azimuth, double elevation, out double theta, out double phi, bool enable_move_pole = true)
        {
            phi = azimuth * Math.PI / 180;
#if false
            theta = elevation * Math.PI / 180
#else
            double scale = Math.Cos(Math.PI / 2 * (1.0 - map_pole_enlarge_scale));
            double angle = Math.Acos((1.0 - elevation / 90.0) * scale);
            theta = (angle - Math.PI / 2) / map_pole_enlarge_scale + Math.PI / 2;
#endif
            if (enable_move_pole)
            {
                prjmap_coords.InvTransform(ref theta, ref phi);
            }
        }

        protected static Point3D? GetSphereIntersection(
            Point3D Origin,
            Vector3D Direction,
            Point3D SphereCenter,
            double SphereRadius)
        {
            Vector3D po = SphereCenter - Origin;
            double length = Direction.Length;
            if (length == 0.0) return null;
            Direction /= length;

            double v = Vector3D.DotProduct(po, Direction);
            double popo = Vector3D.DotProduct(po, po);

            double disc = SphereRadius * SphereRadius - (popo - v * v);
            if (disc < 0.0) return null;

            double d = Math.Sqrt(disc);
            return Origin + Direction * (v - d);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.Up:
                    trackball.Zoom(1.1);
                    break;
                case Key.Down:
                    trackball.Zoom(1.0/1.1);
                    break;
                default:
                    break;
            }
        }

        private UIElement[] PieceArray2D = null;
        private Visual3D[] PieceArray3D = null;
        private UIElement[] TerritoryArray2D = null;
        private Visual3D[] TerritoryArray3D = null;

        private void SetPiece2D(Color col)
        {
            Ellipse piece = new Ellipse();
            piece.Width = 90.0 / pixelization.Resolution * 0.5;
            piece.Height = 90.0 / pixelization.Resolution * 0.5;
            piece.Fill = new SolidColorBrush(col);
            piece.Stroke = new SolidColorBrush(Colors.Black);
            piece.RenderTransform = MapCursorB.RenderTransform.Clone();
            TranslateTransform trans = (TranslateTransform)piece.RenderTransform;
            trans.X -= piece.Width / 2;
            trans.Y -= piece.Height / 2;

            canvas_projection_piece.Children.Add(piece);
            PieceArray2D[rule.GetCursorIndex()] = piece;
        }

        private void SetTerritory2D(Color col)
        {
            Rectangle territory = new Rectangle();
            territory.Width = 90.0 / pixelization.Resolution * 0.7;
            territory.Height = 90.0 / pixelization.Resolution * 0.7;
            territory.Fill = new SolidColorBrush(col);
            territory.Stroke = new SolidColorBrush(Colors.Transparent);
            territory.RenderTransform = MapCursorB.RenderTransform.Clone();
            TranslateTransform trans = (TranslateTransform)territory.RenderTransform;
            trans.X -= territory.Width / 2;
            trans.Y -= territory.Height / 2;

            canvas_projection_territory.Children.Add(territory);
            TerritoryArray2D[rule.GetCursorIndex()] = territory;
        }

        private void SetPiece3D(bool IsBlack)
        {
            ModelVisual3D piece = new ModelVisual3D();
            if (IsBlack)
            {
                piece.Content = viewport.Resources["BlackPieceGeom"] as GeometryModel3D;
            }
            else
            {
                ModelVisual3D child1 = new ModelVisual3D();
                ModelVisual3D child2 = new ModelVisual3D();
                child1.Content = viewport.Resources["WhitePieceEdgeGeom"] as GeometryModel3D;
                child2.Content = viewport.Resources["WhitePieceGeom"] as GeometryModel3D;
                piece.Children.Add(child1);
                piece.Children.Add(child2);
            }
            piece.Transform = CursorBModel.Transform.Clone();

            PieceContainer.Children.Add(piece);
            PieceArray3D[rule.GetCursorIndex()] = piece;
        }

        private void SetTerritory3D(bool IsBlack)
        {
            ModelVisual3D territory = new ModelVisual3D();
            if (IsBlack)
            {
                territory.Content = viewport.Resources["BlackTerritoryGeom"] as GeometryModel3D;
            }
            else
            {
                territory.Content = viewport.Resources["WhiteTerritoryGeom"] as GeometryModel3D;
            }
            territory.Transform = CursorBModel.Transform.Clone();

            TerritoryContainer.Children.Add(territory);
            TerritoryArray3D[rule.GetCursorIndex()] = territory;
        }

        private void RemovePiece()
        {
            int n = rule.GetCursorIndex();
            if (PieceArray2D[n] != null)
            {
                canvas_projection_piece.Children.Remove(PieceArray2D[n]);
            }
            if (PieceArray3D[n] != null)
            {
                PieceContainer.Children.Remove(PieceArray3D[n]);
            }
            PieceArray2D[n] = null;
            PieceArray3D[n] = null;
        }

        private void RemoveTerritory()
        {
            int n = rule.GetCursorIndex();
            if (TerritoryArray2D[n] != null)
            {
                canvas_projection_territory.Children.Remove(TerritoryArray2D[n]);
            }
            if (TerritoryArray3D[n] != null)
            {
                TerritoryContainer.Children.Remove(TerritoryArray3D[n]);
            }
            TerritoryArray2D[n] = null;
            TerritoryArray3D[n] = null;
        }

        private void viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OperationState data = UserInterface.GetOperationState();
            if (ManipulatePiece(data.CurrentOption))
            {
                data.CurrentOption = rule.GetNextOperation(data.CurrentOption);
            }
        }

        private bool ManipulatePiece(PieceOperations op)
        {
            bool success = false;
            switch (op)
            {
                case PieceOperations.OP_PUT_PIECE_BLACK:
                    if (rule.PutPiece(PieceKind.PIECE_BLACK))
                    {
                        RemovePiece();
                        DisplayCursor(null);    // update cursor position (it might be changed not via mouse)
                        SetPiece2D(Colors.Black);
                        SetPiece3D(true);
                        success = true;
                    }
                    break;
                case PieceOperations.OP_PUT_PIECE_WHITE:
                    if (rule.PutPiece(PieceKind.PIECE_WHITE))
                    {
                        RemovePiece();
                        DisplayCursor(null);    // update cursor position (it might be changed not via mouse)
                        SetPiece2D(Colors.White);
                        SetPiece3D(false);
                        success = true;
                    }
                    break;
                case PieceOperations.OP_REMOVE_PIECE:
                    if (rule.PutPiece(PieceKind.PIECE_EMPTY))
                    {
                        RemovePiece();
                        success = true;
                    }
                    break;
                case PieceOperations.OP_FLIP_PIECE:
                    if (rule.FlipPiece())
                    {
                        RemovePiece();
                        DisplayCursor(null);    // update cursor position (it might be changed not via mouse)
                        if (rule.GetPiece() == PieceKind.PIECE_WHITE)
                        {
                            SetPiece2D(Colors.White);
                            SetPiece3D(false);
                        }
                        else
                        {
                            SetPiece2D(Colors.Black);
                            SetPiece3D(true);
                        }
                        success = true;
                    }
                    break;
                case PieceOperations.OP_GET_PIECE:
                    if (rule.RetrievePiece())
                    {
                        RemovePiece();
                        success = true;
                    }
                    break;
                case PieceOperations.OP_PUT_TERRITORY_BLACK:
                    if (rule.PutTerritory(PieceKind.PIECE_BLACK))
                    {
                        RemoveTerritory();
                        DisplayCursor(null);    // update cursor position (it might be changed not via mouse)
                        SetTerritory2D(new Color() { R = 0x00, G = 0x00, B = 0x00, A = 0x88 });
                        SetTerritory3D(true);
                        success = true;
                    }
                    break;
                case PieceOperations.OP_PUT_TERRITORY_WHITE:
                    if (rule.PutTerritory(PieceKind.PIECE_WHITE))
                    {
                        RemoveTerritory();
                        DisplayCursor(null);    // update cursor position (it might be changed not via mouse)
                        SetTerritory2D(new Color() { R = 0xFF, G = 0xFF, B = 0xFF, A = 0x88 });
                        SetTerritory3D(false);
                        success = true;
                    }
                    break;
                case PieceOperations.OP_REMOVE_TERRITORY:
                    if (rule.PutTerritory(PieceKind.PIECE_EMPTY))
                    {
                        RemoveTerritory();
                        success = true;
                    }
                    break;
                default:
                    break;
            }

            // ------------------------------
            // Show scores
            // ------------------------------
            UserInterface.UpdateDispScore();

            return success;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Save settings
            Setting.param.WindowWidth = Width;
            Setting.param.WindowHeight = Height;
            Setting.Save();

            Close();
        }
    }

    public enum PieceOperations
    {
        OP_PUT_PIECE_BLACK,
        OP_PUT_PIECE_WHITE,
        OP_REMOVE_PIECE,
        OP_FLIP_PIECE,
        OP_GET_PIECE,
        OP_PUT_TERRITORY_BLACK,
        OP_PUT_TERRITORY_WHITE,
        OP_REMOVE_TERRITORY
    };

    public class OperationState : INotifyPropertyChanged
    {
        PieceOperations _CurrentOption;
        public PieceOperations CurrentOption
        {
            get { return _CurrentOption; }
            set { _CurrentOption = value; OnPropertyChanged("CurrentOption"); }
        }
        public OperationState() { _CurrentOption = PieceOperations.OP_PUT_PIECE_BLACK; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    };

    [ValueConversion(typeof(Point3DCollection), typeof(string))]
    public class CylindricalConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType,
          object parameter, System.Globalization.CultureInfo culture)
        {
            Point3DCollection cylindrical = (Point3DCollection)value;
            Point3DCollection cartesian = new Point3DCollection();

            foreach (Point3D element in cylindrical)
            {
                double angle = element.Y * Math.PI / 180;
                cartesian.Add(new Point3D()
                {
                    X = element.X * Math.Cos(angle),
                    Y = element.X * Math.Sin(angle),
                    Z = element.Z
                });
            }
            return cartesian;
        }

        public object ConvertBack(object value, System.Type targetType,
          object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumMatchToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string checkValue = value.ToString();
            string targetValue = parameter.ToString();
            return checkValue.Equals(targetValue,
                     StringComparison.InvariantCultureIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;

            bool useValue = (bool)value;
            string targetValue = parameter.ToString();
            if (useValue)
                return Enum.Parse(targetType, targetValue);

            return null;
        }
    }

    public class ProjectionMapCoordsTrans
    {
        private bool enabled = false;
        private Vector3D RotateAxisNorm;
        private double RotationAngleCos;
        private double RotationAngleSin;
        private double theta0;
        private double phi0;

        public void Copy(ProjectionMapCoordsTrans obj)
        {
            enabled = obj.enabled;
            RotateAxisNorm = new Vector3D(obj.RotateAxisNorm.X, obj.RotateAxisNorm.Y, obj.RotateAxisNorm.Z);
            RotationAngleCos = obj.RotationAngleCos;
            RotationAngleSin = obj.RotationAngleSin;
            theta0 = obj.theta0;
            phi0 = obj.phi0;
        }

        private void SetRotationByGreatCircle(double theta1, double phi1, double theta2, double phi2)
        {
            if ((theta1 != theta2) || (phi1 != phi2))
            {
                Point3D point = HealpixBorder.GetCartesianCoord(theta2, phi2);
                Vector3D NewOriginVec = new Vector3D(point.X, point.Y, point.Z);
                point = HealpixBorder.GetCartesianCoord(theta1, phi1);
                Vector3D OriginVec = new Vector3D(point.X, point.Y, point.Z);
                RotateAxisNorm = Vector3D.CrossProduct(OriginVec, NewOriginVec);
                RotateAxisNorm.Normalize();
                double RotationAngle = Vector3D.AngleBetween(OriginVec, NewOriginVec) * Math.PI / 180;

                RotationAngleCos = Math.Cos(RotationAngle);
                RotationAngleSin = Math.Sin(RotationAngle);
                enabled = true;
            }
            else
            {
                enabled = false;
            }
        }

        public void SetPolePositionAbsolute(double theta0, double phi0)
        {
            if ((theta0 != 0) || (phi0 != 0))
            {
#if false
                SetRotationByGreatCircle(0, 0, theta0, phi0);
                this.theta0 = theta0;
                this.phi0 = 0;
#else
                SetRotationByGreatCircle(0, 0, theta0, 0);
                this.theta0 = theta0;
                this.phi0 = phi0;
#endif
            }
        }

        public void SetPolePositionRelative(double theta1, double phi1, double theta2, double phi2)
        {
            if ((theta1 != theta2) || (phi1 != phi2))
            {
                ProjectionMapCoordsTrans trans = new ProjectionMapCoordsTrans();
                trans.SetRotationByGreatCircle(theta1, phi1, theta2, phi2);
                double theta = theta0;
                double phi = phi0;
                trans.Transform(ref theta, ref phi);
                SetPolePositionAbsolute(theta, phi);
            }
        }

        public void Transform(ref double theta, ref double phi)
        {
            if (enabled)
            {
                Point3D point = HealpixBorder.GetCartesianCoord(theta, phi);
                Vector3D ObjectiveVec = new Vector3D(point.X, point.Y, point.Z);
                double h = Vector3D.DotProduct(RotateAxisNorm, ObjectiveVec);
                Vector3D RotateAxis = Vector3D.Multiply(h, RotateAxisNorm);
                // orthogonal vectors within rotation plane
                Vector3D ObjVec0 = ObjectiveVec - RotateAxis;
                Vector3D ObjVec1 = Vector3D.CrossProduct(RotateAxisNorm, ObjectiveVec);
                Vector3D NewObjectiveVec = RotateAxis + Vector3D.Multiply(RotationAngleCos, ObjVec0) + Vector3D.Multiply(RotationAngleSin, ObjVec1);

                phi = Math.Atan2(NewObjectiveVec.Y, NewObjectiveVec.X) + phi0;
                theta = Math.Acos(NewObjectiveVec.Z);
            }
            if (phi < 0)
            {
                phi += 2.0 * Math.PI;
            }
            else if (phi >= 2.0 * Math.PI)
            {
                phi -= 2.0 * Math.PI;
            }
            if (theta < 0)
            {
                theta += 2.0 * Math.PI;
            }
            else if (theta >= 2.0 * Math.PI)
            {
                theta -= 2.0 * Math.PI;
            }
        }

        public void InvTransform(ref double theta, ref double phi)
        {
            if (enabled)
            {
                Point3D point = HealpixBorder.GetCartesianCoord(theta, phi - phi0);
                Vector3D ObjectiveVec = new Vector3D(point.X, point.Y, point.Z);
                double h = Vector3D.DotProduct(RotateAxisNorm, ObjectiveVec);
                Vector3D RotateAxis = Vector3D.Multiply(h, RotateAxisNorm);
                // orthogonal vectors within rotation plane
                Vector3D ObjVec0 = ObjectiveVec - RotateAxis;
                Vector3D ObjVec1 = Vector3D.CrossProduct(RotateAxisNorm, ObjectiveVec);
                Vector3D NewObjectiveVec = RotateAxis + Vector3D.Multiply(RotationAngleCos, ObjVec0) + Vector3D.Multiply(-RotationAngleSin, ObjVec1);

                phi = Math.Atan2(NewObjectiveVec.Y, NewObjectiveVec.X);
                theta = Math.Acos(NewObjectiveVec.Z);
            }
            if (phi < 0)
            {
                phi += 2.0 * Math.PI;
            }
            else if (phi >= 2.0 * Math.PI)
            {
                phi -= 2.0 * Math.PI;
            }
            if (theta < 0)
            {
                theta += 2.0 * Math.PI;
            }
            else if (theta >= 2.0 * Math.PI)
            {
                theta -= 2.0 * Math.PI;
            }
        }
    }
}
