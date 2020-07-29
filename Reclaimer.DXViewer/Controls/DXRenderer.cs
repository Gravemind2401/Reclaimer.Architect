using Adjutant.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

using Keys = System.Windows.Forms.Keys;
using NativeMethods = Reclaimer.Utilities.NativeMethods;

using static HelixToolkit.Wpf.SharpDX.Media3DExtension;

using Helix = HelixToolkit.Wpf.SharpDX;
using Media3D = System.Windows.Media.Media3D;

namespace Reclaimer.Controls
{
    [TemplatePart(Name = PART_Viewport, Type = typeof(Helix.Viewport3DX))]
    public class DXRenderer : Control, IDisposable
    {
        private const string PART_Viewport = "PART_Viewport";

        private const double RAD_089 = 1.5706217940;
        private const double RAD_090 = 1.5707963268;
        private const double RAD_360 = 6.2831853072;
        private const double SpeedMultipler = 0.001;

        private static readonly Helix.EffectsManager effectsManager = new Helix.DefaultEffectsManager();

        #region Dependency Properties

        public static readonly DependencyProperty CameraProperty =
            DependencyProperty.Register(nameof(Camera), typeof(Helix.PerspectiveCamera), typeof(DXRenderer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        
        public static readonly DependencyProperty CameraModeProperty =
            DependencyProperty.Register(nameof(CameraMode), typeof(Helix.CameraMode), typeof(DXRenderer), new PropertyMetadata(Helix.CameraMode.WalkAround));
        
        private static readonly DependencyPropertyKey YawPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(Yaw), typeof(double), typeof(DXRenderer), new PropertyMetadata(0.0));

        public static readonly DependencyProperty YawProperty = YawPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey PitchPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(Pitch), typeof(double), typeof(DXRenderer), new PropertyMetadata(0.0));

        public static readonly DependencyProperty PitchProperty = PitchPropertyKey.DependencyProperty;

        public Helix.PerspectiveCamera Camera
        {
            get { return (Helix.PerspectiveCamera)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        public Helix.CameraMode CameraMode
        {
            get { return (Helix.CameraMode)GetValue(CameraModeProperty); }
            set { SetValue(CameraModeProperty, value); }
        }

        public double Yaw
        {
            get { return (double)GetValue(YawProperty); }
            private set { SetValue(YawPropertyKey, value); }
        }

        public double Pitch
        {
            get { return (double)GetValue(PitchProperty); }
            private set { SetValue(PitchPropertyKey, value); }
        }

        #endregion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static DXRenderer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DXRenderer), new FrameworkPropertyMetadata(typeof(DXRenderer)));
        }

        //private readonly DispatcherTimer timer;

        private Point lastPoint;

        private Helix.Viewport3DX Viewport { get; set; }
        private readonly List<Helix.Element3D> children = new List<Helix.Element3D>();

        public DXRenderer() : base()
        {
            //NormalizeSet();
            //timer = new DispatcherTimer(DispatcherPriority.Send) { Interval = new TimeSpan(0, 0, 0, 0, 10) };
            //timer.Tick += Timer_Tick;
            //timer.Start();

            Camera = new Helix.PerspectiveCamera
            {
                Position = new Media3D.Point3D(),
                LookDirection = new Media3D.Vector3D(1, 0, 0),
                UpDirection = new Media3D.Vector3D(0, 0, 1)
            };
        }

        #region Overrides
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            OnViewportUnset();
            Viewport = Template.FindName(PART_Viewport, this) as Helix.Viewport3DX;
            OnViewportSet();
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            return;

            Focus();
            CaptureMouse();
            Cursor = Cursors.None;
            lastPoint = PointToScreen(e.GetPosition(this));
            lastPoint = new Point((int)lastPoint.X, (int)lastPoint.Y);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            return;

            ReleaseMouseCapture();
            Cursor = Cursors.Cross;
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);
            return;

            //if (e.Delta > 0) CameraSpeed = ClipValue(Math.Ceiling(CameraSpeed * 1050) / 1000, 0.001, MaxCameraSpeed);
            //else CameraSpeed = ClipValue(Math.Floor(CameraSpeed * 0950) / 1000, 0.001, MaxCameraSpeed);
        }

        private void OnViewportUnset()
        {
            if (Viewport == null) return;

            Viewport.OnRendered -= Viewport_OnRendered;

            foreach (var c in children)
                Viewport.Items.Remove(c);

            Viewport.EffectsManager = null;
        }

        private void OnViewportSet()
        {
            if (Viewport == null) return;

            foreach (var c in children)
                Viewport.Items.Add(c);

            Viewport.EffectsManager = effectsManager;
            Viewport.OnRendered += Viewport_OnRendered;
        }

        private void Viewport_OnRendered(object sender, EventArgs e)
        {
            Viewport.OnRendered -= Viewport_OnRendered;
            ScaleToContent();
        }
        #endregion

        private void GetNodeBounds(Helix.Model.Scene.SceneNode node, List<SharpDX.BoundingBox> boundsList)
        {
            if (node.HasBound)
                boundsList.Add(node.BoundsWithTransform);
            else if (node.ItemsCount > 0)
            {
                foreach (var child in node.Items)
                    GetNodeBounds(child, boundsList);
            }
        }

        public void ScaleToContent()
        {
            if (Viewport == null)
                return;

            var boundsList = new List<SharpDX.BoundingBox>();
            foreach (var element in Viewport.Items)
                GetNodeBounds(element.SceneNode, boundsList);

            var min = new SharpDX.Vector3(
                boundsList.Min(b => b.Minimum.X),
                boundsList.Min(b => b.Minimum.Y),
                boundsList.Min(b => b.Minimum.Z)
            );

            var max = new SharpDX.Vector3(
                boundsList.Max(b => b.Maximum.X),
                boundsList.Max(b => b.Maximum.Y),
                boundsList.Max(b => b.Maximum.Z)
            );

            var bounds = new SharpDX.BoundingBox(min, max);

            Viewport.FixedRotationPoint = bounds.Center.ToPoint3D();
            (Viewport.Camera as Helix.PerspectiveCamera).FarPlaneDistance = bounds.Size.Length() * 3;
            (Viewport.Camera as Helix.PerspectiveCamera).NearPlaneDistance = 0.01;

            ZoomToBounds(bounds);

            //var len = bounds.Size.Length();
            //CameraSpeed = Math.Ceiling(len);
            //MaxCameraSpeed = Math.Ceiling(len * 6);
            //MaxPosition = new Point3D(
            //    bounds.XBounds.Max + len * 3,
            //    bounds.YBounds.Max + len * 3,
            //    bounds.ZBounds.Max + len * 3);
            //MinPosition = new Point3D(
            //    bounds.XBounds.Min - len * 3,
            //    bounds.YBounds.Min - len * 3,
            //    bounds.ZBounds.Min - len * 3);

            //MinFarPlaneDistance = 100;
            //FarPlaneDistance = Math.Max(MinFarPlaneDistance, Math.Ceiling(len));
            //MaxFarPlaneDistance = Math.Max(MinFarPlaneDistance, Math.Ceiling(len * 3));
        }

        public void LocateObject(Helix.GroupModel3D m)
        {
            var boundsList = new List<SharpDX.BoundingBox>();
            GetNodeBounds(m.SceneNode, boundsList);

            var min = new SharpDX.Vector3(
                boundsList.Min(b => b.Minimum.X),
                boundsList.Min(b => b.Minimum.Y),
                boundsList.Min(b => b.Minimum.Z)
            );

            var max = new SharpDX.Vector3(
                boundsList.Max(b => b.Maximum.X),
                boundsList.Max(b => b.Maximum.Y),
                boundsList.Max(b => b.Maximum.Z)
            );

            var bounds = new SharpDX.BoundingBox(min, max);
            ZoomToBounds(bounds);
        }

        public void AddChild(Helix.Element3D child)
        {
            children.Add(child);
            Viewport?.Items.Add(child);
        }

        public void RemoveChild(Helix.Element3D child)
        {
            children.Remove(child);
            Viewport?.Items.Remove(child);
        }

        public void ClearChildren()
        {
            children.Clear();
            OnViewportUnset(); //don't use Viewport.Children.Clear() because it will remove the lights
        }

        public void Dispose()
        {
            //timer.Stop();

            if (Viewport != null)
            {
                Viewport.Dispose();

                foreach (var item in Viewport.Items)
                    item.Dispose();

                ClearChildren();
                Viewport.Items.Clear();
            }
        }

        private void ZoomToBounds(SharpDX.BoundingBox bounds)
        {
            Viewport.ZoomExtents(new Media3D.Rect3D(bounds.Minimum.ToPoint3D(), bounds.Size.ToSize3D()), 0);

            if (bounds.Size.X / 2 > bounds.Size.Y)
                Viewport.ChangeDirection(new Media3D.Vector3D(0, -1, 0), Camera.UpDirection, 0);
            else
                Viewport.ChangeDirection(new Media3D.Vector3D(-1, 0, 0), Camera.UpDirection, 0);
        }

        private void ZoomToBounds(RealBounds3D bounds)
        {
            var len = bounds.Length;

            // Viewport.ZoomExtents() ?

            if (bounds.XBounds.Length / 2 > bounds.YBounds.Length) //side view for long models like weapons
            {
                var p = new Media3D.Point3D(
                    bounds.XBounds.Midpoint,
                    bounds.YBounds.Max + len * 0.5,
                    bounds.ZBounds.Midpoint);
                MoveCamera(p, new Media3D.Vector3D(0, 0, -2));
            }
            else //normal camera position
            {
                var p = new Media3D.Point3D(
                    bounds.XBounds.Max + len * 0.5,
                    bounds.YBounds.Midpoint,
                    bounds.ZBounds.Midpoint);
                MoveCamera(p, new Media3D.Vector3D(-1, 0, 0));
            }
        }

        private void NormalizeSet()
        {
            return;
            //var len = LookDirection.Length;
            //LookDirection = new Vector3D(LookDirection.X / len, LookDirection.Y / len, LookDirection.Z / len);
            //Yaw = Math.Atan2(LookDirection.X, LookDirection.Z);
            //Pitch = Math.Atan(LookDirection.Y);
        }

        private void MoveCamera(Media3D.Point3D position, Media3D.Vector3D direction)
        {
            Camera.Position = position;
            Camera.LookDirection = direction;
            NormalizeSet();
        }

        private void UpdateCameraPosition()
        {
            if (!IsMouseCaptured && !IsFocused) return;

            #region Set FOV
            //if (CheckKeyState(Keys.NumPad6)) FieldOfView = ClipValue(FieldOfView + FieldOfView / 100.0, 45, 120);
            //if (CheckKeyState(Keys.NumPad4)) FieldOfView = ClipValue(FieldOfView - FieldOfView / 100.0, 45, 120);
            #endregion

            #region Set FPD
            //if (CheckKeyState(Keys.NumPad8)) FarPlaneDistance = ClipValue(FarPlaneDistance * 1.01, MinFarPlaneDistance, MaxFarPlaneDistance);
            //if (CheckKeyState(Keys.NumPad2)) FarPlaneDistance = ClipValue(FarPlaneDistance * 0.99, MinFarPlaneDistance, MaxFarPlaneDistance);
            #endregion

            if (!IsMouseCaptured) return;

            return;
            //if (CheckKeyState(Keys.W) || CheckKeyState(Keys.A) || CheckKeyState(Keys.S) || CheckKeyState(Keys.D) || CheckKeyState(Keys.R) || CheckKeyState(Keys.F))
            //{
            //    var nextPosition = Position;
            //    var len = LookDirection.Length;
            //    var lookDirection = LookDirection = new Vector3D(LookDirection.X / len, LookDirection.Y / len, LookDirection.Z / len);

            //    var dist = CameraSpeed * SpeedMultipler;
            //    if (CheckKeyState(Keys.ShiftKey)) dist *= 3;
            //    if (CheckKeyState(Keys.Space)) dist /= 3;

            //    #region Check WASD

            //    if (CheckKeyState(Keys.W))
            //    {
            //        nextPosition.X += lookDirection.X * dist;
            //        nextPosition.Y += lookDirection.Y * dist;
            //        nextPosition.Z += lookDirection.Z * dist;
            //    }

            //    if (CheckKeyState(Keys.A))
            //    {
            //        nextPosition.X -= Math.Sin(Yaw + RAD_090) * dist;
            //        nextPosition.Y -= Math.Cos(Yaw + RAD_090) * dist;
            //    }

            //    if (CheckKeyState(Keys.S))
            //    {
            //        nextPosition.X -= lookDirection.X * dist;
            //        nextPosition.Y -= lookDirection.Y * dist;
            //        nextPosition.Z -= lookDirection.Z * dist;
            //    }

            //    if (CheckKeyState(Keys.D))
            //    {
            //        nextPosition.X += Math.Sin(Yaw + RAD_090) * dist;
            //        nextPosition.Y += Math.Cos(Yaw + RAD_090) * dist;
            //    }
            //    #endregion

            //    #region Check RF

            //    if (CheckKeyState(Keys.R))
            //    {
            //        var upAxis = Vector3D.CrossProduct(LookDirection, Vector3D.CrossProduct(LookDirection, UpDirection));
            //        upAxis.Normalize();
            //        nextPosition.X -= upAxis.X * dist;
            //        nextPosition.Y -= upAxis.Y * dist;
            //        nextPosition.Z -= upAxis.Z * dist;
            //    }

            //    if (CheckKeyState(Keys.F))
            //    {
            //        var upAxis = Vector3D.CrossProduct(LookDirection, Vector3D.CrossProduct(LookDirection, UpDirection));
            //        upAxis.Normalize();
            //        nextPosition.X += upAxis.X * dist;
            //        nextPosition.Y += upAxis.Y * dist;
            //        nextPosition.Z += upAxis.Z * dist;
            //    }
            //    #endregion

            //    Position = new Point3D(
            //        ClipValue(nextPosition.X, MinPosition.X, MaxPosition.X),
            //        ClipValue(nextPosition.Y, MinPosition.Y, MaxPosition.Y),
            //        ClipValue(nextPosition.Z, MinPosition.Z, MaxPosition.Z));
            //}
        }

        private void UpdateCameraDirection(Point mousePos)
        {
            if (!IsMouseCaptured) return;
            if (lastPoint.Equals(mousePos)) return;

            var deltaX = mousePos.X - lastPoint.X;
            var deltaY = mousePos.Y - lastPoint.Y;

            Yaw += deltaX * 0.01;
            Pitch -= deltaY * 0.01;

            Yaw %= RAD_360;
            Pitch = ClipValue(Pitch, -RAD_089, RAD_089);

            Camera.LookDirection = new Media3D.Vector3D(Math.Sin(Yaw), Math.Cos(Yaw), Math.Tan(Pitch));
            NativeMethods.SetCursorPos((int)lastPoint.X, (int)lastPoint.Y);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var cursorPos = new System.Drawing.Point();
            NativeMethods.GetCursorPos(out cursorPos);

            UpdateCameraPosition();
            UpdateCameraDirection(new Point(cursorPos.X, cursorPos.Y));
        }

        private static double ClipValue(double val, double min, double max)
        {
            return Math.Min(Math.Max(min, val), max);
        }

        private static bool CheckKeyState(Keys keys)
        {
            return ((NativeMethods.GetAsyncKeyState((int)keys) & 32768) != 0);
        }
    }
}
