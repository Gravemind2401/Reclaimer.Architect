using Adjutant.Spatial;
using Reclaimer.Utilities;
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
using static HelixToolkit.Wpf.SharpDX.ViewportExtensions;
using static HelixToolkit.Wpf.SharpDX.CameraExtensions;

using Numerics = System.Numerics;
using Helix = HelixToolkit.Wpf.SharpDX;
using Media3D = System.Windows.Media.Media3D;
using Reclaimer.Plugins;

namespace Reclaimer.Controls
{
    [TemplatePart(Name = PART_Viewport, Type = typeof(Helix.Viewport3DX))]
    public class DXRenderer : Control, IDisposable
    {
        private const string PART_Viewport = "PART_Viewport";
        private const double SpeedMultipler = 0.0013;

        private static readonly Helix.EffectsManager effectsManager = new Helix.DefaultEffectsManager();

        #region Dependency Properties

        #region Camera
        public static readonly DependencyPropertyKey ViewportPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(Viewport), typeof(Helix.Viewport3DX), typeof(DXRenderer), new PropertyMetadata((Helix.Viewport3DX)null));

        public static readonly DependencyProperty ViewportProperty = ViewportPropertyKey.DependencyProperty;

        public static readonly DependencyProperty CameraSpeedProperty =
            DependencyProperty.Register(nameof(CameraSpeed), typeof(double), typeof(DXRenderer), new PropertyMetadata(0.015));

        private static readonly DependencyPropertyKey MaxCameraSpeedPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(MaxCameraSpeed), typeof(double), typeof(DXRenderer), new PropertyMetadata(1.5));

        public static readonly DependencyProperty MaxCameraSpeedProperty = MaxCameraSpeedPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey YawPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(Yaw), typeof(double), typeof(DXRenderer), new PropertyMetadata(0.0));

        public static readonly DependencyProperty YawProperty = YawPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey PitchPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(Pitch), typeof(double), typeof(DXRenderer), new PropertyMetadata(0.0));

        public static readonly DependencyProperty PitchProperty = PitchPropertyKey.DependencyProperty;

        public Helix.Viewport3DX Viewport
        {
            get { return (Helix.Viewport3DX)GetValue(ViewportProperty); }
            private set { SetValue(ViewportPropertyKey, value); }
        }

        public double CameraSpeed
        {
            get { return (double)GetValue(CameraSpeedProperty); }
            set { SetValue(CameraSpeedProperty, value); }
        }

        public double MaxCameraSpeed
        {
            get { return (double)GetValue(MaxCameraSpeedProperty); }
            private set { SetValue(MaxCameraSpeedPropertyKey, value); }
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

        #region Manipulation
        public static readonly DependencyProperty ManipulationEnabledProperty =
            DependencyProperty.Register(nameof(ManipulationEnabled), typeof(bool), typeof(DXRenderer), new PropertyMetadata(false));

        public static readonly DependencyProperty ManipulationFlagsProperty =
            DependencyProperty.Register(nameof(ManipulationFlags), typeof(ManipulationFlags), typeof(DXRenderer), new PropertyMetadata(ManipulationFlags.Default, (d, e) =>
            {
                var c = (d as DXRenderer);
                c.manipulator.ManipulationFlags = c.ManipulationFlags;
            }));

        public static readonly DependencyProperty HighlightMaterialProperty =
            DependencyProperty.Register(nameof(HighlightMaterial), typeof(Helix.Material), typeof(DXRenderer), new PropertyMetadata(Helix.DiffuseMaterials.Yellow));

        public static readonly DependencyProperty SelectionMaterialProperty =
            DependencyProperty.Register(nameof(SelectionMaterial), typeof(Helix.Material), typeof(DXRenderer), new PropertyMetadata(Helix.DiffuseMaterials.Jade));

        public static readonly DependencyProperty GlobalManipulationAxesProperty =
            DependencyProperty.Register(nameof(GlobalManipulationAxes), typeof(bool), typeof(DXRenderer), new PropertyMetadata(true, (d, e) =>
            {
                var c = (d as DXRenderer);
                c.manipulator.LocalAxes = !c.GlobalManipulationAxes;
                ArchitectSettingsPlugin.Settings.EditorGlobalAxes = c.GlobalManipulationAxes;
            }));

        public bool ManipulationEnabled
        {
            get { return (bool)GetValue(ManipulationEnabledProperty); }
            set { SetValue(ManipulationEnabledProperty, value); }
        }

        public ManipulationFlags ManipulationFlags
        {
            get { return (ManipulationFlags)GetValue(ManipulationFlagsProperty); }
            set { SetValue(ManipulationFlagsProperty, value); }
        }

        public Helix.Material HighlightMaterial
        {
            get { return (Helix.Material)GetValue(HighlightMaterialProperty); }
            set { SetValue(HighlightMaterialProperty, value); }
        }

        public Helix.Material SelectionMaterial
        {
            get { return (Helix.Material)GetValue(SelectionMaterialProperty); }
            set { SetValue(SelectionMaterialProperty, value); }
        }

        public bool GlobalManipulationAxes
        {
            get { return (bool)GetValue(GlobalManipulationAxesProperty); }
            set { SetValue(GlobalManipulationAxesProperty, value); }
        }

        #endregion

        #endregion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static DXRenderer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DXRenderer), new FrameworkPropertyMetadata(typeof(DXRenderer)));
        }

        private readonly DispatcherTimer timer;

        private Point lastPoint;

        private readonly List<Helix.Element3D> children = new List<Helix.Element3D>();

        private readonly ElementHighlighter3D selector = new ElementHighlighter3D();
        private readonly ElementHighlighter3D highlighter = new ElementHighlighter3D
        {
            HighlightColor = SharpDX.Color.Gold,
            EnableXRayGrid = false
        };

        private readonly TransformManipulatorEx3D manipulator = new TransformManipulatorEx3D
        {
            UniformScaling = true,
            AutoSizeScale = true,
            Visibility = Visibility.Collapsed
        };

        public DXRenderer() : base()
        {
            Loaded += DXRenderer_Loaded;
            Unloaded += DXRenderer_Unloaded;

            timer = new DispatcherTimer(DispatcherPriority.Send) { Interval = new TimeSpan(0, 0, 0, 0, 15) };
            timer.Tick += Timer_Tick;

            GlobalManipulationAxes = ArchitectSettingsPlugin.Settings.EditorGlobalAxes;

            if (!ArchitectSettingsPlugin.Settings.HighlightSelection)
                selector.HighlightColor = SharpDX.Color.Transparent;

            manipulator.ManipulationFlags = ManipulationFlags;
            manipulator.LocalAxes = !GlobalManipulationAxes;
        }

        private IRendererHost GetHost()
        {
            var target = Parent as FrameworkElement;
            while (target != null)
            {
                var host = target as IRendererHost;
                if (host != null) return host;
                target = target.Parent as FrameworkElement;
            }

            return null;
        }

        #region Overrides
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            OnViewportUnset();
            Viewport = Template.FindName(PART_Viewport, this) as Helix.Viewport3DX;
            OnViewportSet();
        }

        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonDown(e);

            Focus();
            CaptureMouse();
            Cursor = Cursors.None;
            lastPoint = PointToScreen(e.GetPosition(this));
            lastPoint = new Point((int)lastPoint.X, (int)lastPoint.Y);

            e.Handled = true;
        }

        protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonUp(e);

            ReleaseMouseCapture();
            Cursor = Cursors.Cross;

            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!ManipulationEnabled || IsMouseCaptured)
                return;

            SetHighlightedElement(Viewport.FindHits(e.GetPosition(this)));
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);

            if (e.Delta > 0) CameraSpeed = ClipValue(Math.Ceiling(CameraSpeed * 1050) / 1000, 0.001, MaxCameraSpeed);
            else CameraSpeed = ClipValue(Math.Floor(CameraSpeed * 0950) / 1000, 0.001, MaxCameraSpeed);

            e.Handled = true;
        }

        private void OnViewportUnset()
        {
            if (Viewport == null) return;

            Viewport.MouseDown3D -= Viewport_MouseDown3D;
            Viewport.OnRendered -= Viewport_OnRendered;

            Viewport.Items.Remove(manipulator);
            Viewport.Items.Remove(highlighter);
            Viewport.Items.Remove(selector);
            foreach (var c in children)
                Viewport.Items.Remove(c);

            Viewport.EffectsManager = null;
        }

        private void OnViewportSet()
        {
            if (Viewport == null) return;

            Viewport.Items.Add(manipulator);
            Viewport.Items.Add(highlighter);
            Viewport.Items.Add(selector);
            foreach (var c in children)
                Viewport.Items.Add(c);

            Viewport.EffectsManager = effectsManager;
            Viewport.OnRendered += Viewport_OnRendered;
            Viewport.MouseDown3D += Viewport_MouseDown3D;
        }

        private void Viewport_OnRendered(object sender, EventArgs e)
        {
            Viewport.OnRendered -= Viewport_OnRendered;
            (Viewport.Camera as Helix.PerspectiveCamera).FieldOfView = ArchitectSettingsPlugin.Settings.DefaultFieldOfView;
            ScaleToContent();
        }

        private void Viewport_MouseDown3D(object sender, RoutedEventArgs e)
        {
            if (ManipulationEnabled)
                SetSelectedElement((e as Helix.Mouse3DEventArgs)?.HitTestResult);
        }
        #endregion

        #region Event Handlers

        private void DXRenderer_Loaded(object sender, RoutedEventArgs e)
        {
            timer.Start();
        }

        private void DXRenderer_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var cursorPos = new System.Drawing.Point();
            NativeMethods.GetCursorPos(out cursorPos);

            UpdateCameraPosition();
            UpdateCameraDirection(new Point(cursorPos.X, cursorPos.Y));
        }

        #endregion

        #region Manipulation

        //for debug purposes
        private static Helix.MeshGeometryModel3D box = new Helix.MeshGeometryModel3D { Material = Helix.DiffuseMaterials.Red };
        public void HighlightBounds(SharpDX.BoundingBox bounds)
        {
            var r3d = new Media3D.Rect3D(bounds.Minimum.ToPoint3D(), bounds.Size.ToSize3D());
            var b = new Helix.MeshBuilder();
            b.AddBoundingBox(r3d, 0.1);
            b.AddSphere(bounds.Center, 0.1);
            var geom = b.ToMeshGeometry3D();

            box.Geometry = geom;
            if (!Viewport.Items.Contains(box))
                Viewport.Items.Add(box);
        }

        private void SetHighlightedElement(IList<Helix.HitTestResult> hits)
        {
            var targeted = hits?.Select(h => (h.ModelHit as Helix.Element3D)?.FindInstanceParent())
                .Where(i => i != null)
                .FirstOrDefault() as Helix.GroupElement3D;

            if (targeted != null && targeted == selector.Target)
                return;

            highlighter.Target = targeted;
        }

        private void SetSelectedElement(Helix.HitTestResult hit)
        {
            var model = hit?.ModelHit as Helix.Element3D;
            SetSelectedElement(model);
        }

        public Helix.Element3D GetSelectedElement()
        {
            return selector.Target;
        }

        public void SetSelectedElement(Helix.Element3D model)
        {
            if (model != null && model.IsDescendentOf(manipulator))
                return;
            else model = model?.FindInstanceParent();

            if (model == selector.Target)
                return;

            if (model != null)
            {
                manipulator.CenterOffset = model.GetTotalBounds(true).Center;
                manipulator.Visibility = Visibility.Visible;
            }
            else
                manipulator.Visibility = Visibility.Collapsed;

            manipulator.Target = selector.Target = model;
            highlighter.Target = null;

            GetHost()?.OnElementSelected(model);
        }

        #endregion

        public void ScaleToContent()
        {
            if (Viewport == null)
                return;

            var bounds = Viewport.Items.GetTotalBounds();

            Viewport.FixedRotationPoint = bounds.Center.ToPoint3D();
            (Viewport.Camera as Helix.PerspectiveCamera).FarPlaneDistance = bounds.Size.Length() * 2;
            (Viewport.Camera as Helix.PerspectiveCamera).NearPlaneDistance = 0.01;

            ZoomToBounds(bounds);

            var len = bounds.Size.Length();
            CameraSpeed = Math.Ceiling(len);
            MaxCameraSpeed = Math.Ceiling(len * 6);
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

        public void ZoomToBounds(SharpDX.BoundingBox bounds, double animationTime = 0)
        {
            if (bounds.Size.IsZero)
                return;

            var pcam = Viewport.Camera as Helix.PerspectiveCamera;
            var center = bounds.Center.ToPoint3D();
            var radius = bounds.Size.Length() / 2;

            double disth = radius / Math.Tan(0.5 * pcam.FieldOfView * Math.PI / 180);
            double vfov = pcam.FieldOfView / Viewport.ActualWidth * Viewport.ActualHeight;
            double distv = radius / Math.Tan(0.5 * vfov * Math.PI / 180);

            double adjust = distv > disth ? 0.75 : 1;
            double dist = Math.Max(disth, distv) * adjust;

            var dir = (bounds.Size.X > bounds.Size.Y * 1.5)
                ? new Media3D.Vector3D(0, -Math.Sign(center.Y), 0)
                : new Media3D.Vector3D(-Math.Sign(center.X), 0, 0);

            pcam.LookAt(center, dir * dist, Viewport.ModelUpDirection, animationTime);
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
            if (Viewport != null)
            {
                Viewport.Dispose();

                foreach (var item in Viewport.Items)
                    item.Dispose();

                ClearChildren();
                Viewport.Items.Clear();
            }
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

            if (CheckKeyState(Keys.W) || CheckKeyState(Keys.A) || CheckKeyState(Keys.S) || CheckKeyState(Keys.D) || CheckKeyState(Keys.R) || CheckKeyState(Keys.F))
            {
                var moveVector = new Media3D.Vector3D();
                var upVector = Viewport.Camera.UpDirection;
                var forwardVector = Viewport.Camera.LookDirection;
                var rightVector = Media3D.Vector3D.CrossProduct(forwardVector, upVector);

                upVector.Normalize();
                forwardVector.Normalize();
                rightVector.Normalize();

                var dist = CameraSpeed * SpeedMultipler;
                if (CheckKeyState(Keys.ShiftKey)) dist *= 3;
                if (CheckKeyState(Keys.Space)) dist /= 3;

                #region Check WASD/RF

                if (CheckKeyState(Keys.W))
                    moveVector += forwardVector * dist;

                if (CheckKeyState(Keys.A))
                    moveVector -= rightVector * dist;

                if (CheckKeyState(Keys.S))
                    moveVector -= forwardVector * dist;

                if (CheckKeyState(Keys.D))
                    moveVector += rightVector * dist;

                if (CheckKeyState(Keys.R))
                    moveVector += upVector * dist;

                if (CheckKeyState(Keys.F))
                    moveVector -= upVector * dist;

                #endregion

                Viewport.Camera.Position += moveVector;
            }
        }

        private void UpdateCameraDirection(Point mousePos)
        {
            if (!IsMouseCaptured || lastPoint.Equals(mousePos))
                return;

            var deltaX = (float)(mousePos.X - lastPoint.X) * (float)SpeedMultipler * 2;
            var deltaY = (float)(mousePos.Y - lastPoint.Y) * (float)SpeedMultipler * 2;

            var upAnchor = Viewport.ModelUpDirection.ToNumericsVector3();
            var upVector = Numerics.Vector3.Normalize(Viewport.Camera.UpDirection.ToNumericsVector3());
            var forwardVector = Numerics.Vector3.Normalize(Viewport.Camera.LookDirection.ToNumericsVector3());
            var rightVector = Numerics.Vector3.Normalize(Numerics.Vector3.Cross(forwardVector, upVector));

            var yaw = Numerics.Matrix4x4.CreateFromAxisAngle(upAnchor, -deltaX);

            forwardVector = Numerics.Vector3.TransformNormal(forwardVector, yaw);
            rightVector = Numerics.Vector3.TransformNormal(rightVector, yaw);

            var pitch = Numerics.Matrix4x4.CreateFromAxisAngle(rightVector, -deltaY);

            forwardVector = Numerics.Vector3.TransformNormal(forwardVector, pitch);
            upVector = Numerics.Vector3.Normalize(Numerics.Vector3.Cross(rightVector, forwardVector));

            Viewport.Camera.LookDirection = new Media3D.Vector3D(forwardVector.X, forwardVector.Y, forwardVector.Z);
            Viewport.Camera.UpDirection = new Media3D.Vector3D(upVector.X, upVector.Y, upVector.Z);

            Yaw = Math.Atan2(forwardVector.X, forwardVector.Y);
            Pitch = Math.Asin(-forwardVector.Z);

            NativeMethods.SetCursorPos((int)lastPoint.X, (int)lastPoint.Y);
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
