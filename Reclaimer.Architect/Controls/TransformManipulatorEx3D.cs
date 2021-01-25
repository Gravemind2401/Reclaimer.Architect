using SharpDX;
using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;

using System.Windows;
using Media3D = System.Windows.Media.Media3D;
using Media = System.Windows.Media;

using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Utilities;
using Reclaimer.Utilities;

namespace Reclaimer.Controls
{
    public class TransformManipulatorEx3D : GroupElement3D
    {
        private const string xrayEffectName = "ManipulatorXRayGrid";

        private static readonly Geometry3D TranslationXGeometry;
        private static readonly Geometry3D TranslationXYGeometry;
        private static readonly Geometry3D RotationXGeometry;
        private static readonly Geometry3D ScalingGeometry;

        static TransformManipulatorEx3D()
        {
            var bd = new MeshBuilder();
            float arrowLength = 1.5f;
            bd.AddArrow(Vector3.UnitX * arrowLength, new Vector3(1.2f * arrowLength, 0, 0), 0.1, 4, 12);
            bd.AddCylinder(Vector3.Zero, Vector3.UnitX * arrowLength, 0.05, 12);
            TranslationXGeometry = bd.ToMesh();

            bd = new MeshBuilder();
            bd.AddCylinder(new Vector3(0, 0, -0.01f), new Vector3(0, 0, 0.01f), arrowLength * 0.3, 32, true, true);
            TranslationXYGeometry = bd.ToMesh();

            bd = new MeshBuilder();
            var circle = MeshBuilder.GetCircle(32, true);
            var path = circle.Select(x => new Vector3(0, x.X, x.Y)).ToArray();
            bd.AddTube(path, 0.06, 8, true);
            RotationXGeometry = bd.ToMesh();

            bd = new MeshBuilder();
            bd.AddBox(Vector3.UnitX * 0.75f, 0.15, 0.15, 0.15);
            bd.AddCylinder(Vector3.Zero, Vector3.UnitX * 0.8f, 0.02, 4);
            ScalingGeometry = bd.ToMesh();

            TranslationXGeometry.OctreeParameter.MinimumOctantSize = 0.01f;
            TranslationXGeometry.UpdateOctree();

            TranslationXYGeometry.OctreeParameter.MinimumOctantSize = 0.01f;
            TranslationXYGeometry.UpdateOctree();

            RotationXGeometry.OctreeParameter.MinimumOctantSize = 0.01f;
            RotationXGeometry.UpdateOctree();

            ScalingGeometry.OctreeParameter.MinimumOctantSize = 0.01f;
            ScalingGeometry.UpdateOctree();
        }

        #region Dependency Properties
        public Element3D Target
        {
            get { return (Element3D)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(nameof(Target), typeof(Element3D), typeof(TransformManipulatorEx3D), new PropertyMetadata(null, (d, e) =>
            {
                (d as TransformManipulatorEx3D).OnTargetChanged(e.NewValue as Element3D);
            }));

        public bool EnableScaling
        {
            get { return (bool)GetValue(EnableScalingProperty); }
            set { SetValue(EnableScalingProperty, value); }
        }

        public static readonly DependencyProperty EnableScalingProperty =
            DependencyProperty.Register(nameof(EnableScaling), typeof(bool), typeof(TransformManipulatorEx3D), new PropertyMetadata(true, (d, e) =>
            {
                (d as TransformManipulatorEx3D).scaleGroup.IsRendering = (bool)e.NewValue;
            }));

        public bool UniformScaling
        {
            get { return (bool)GetValue(UniformScalingProperty); }
            set { SetValue(UniformScalingProperty, value); }
        }

        public static readonly DependencyProperty UniformScalingProperty =
            DependencyProperty.Register(nameof(UniformScaling), typeof(bool), typeof(TransformManipulatorEx3D), new PropertyMetadata(false));

        public bool EnableTranslation
        {
            get { return (bool)GetValue(EnableTranslationProperty); }
            set { SetValue(EnableTranslationProperty, value); }
        }

        public static readonly DependencyProperty EnableTranslationProperty =
            DependencyProperty.Register(nameof(EnableTranslation), typeof(bool), typeof(TransformManipulatorEx3D), new PropertyMetadata(true, (d, e) =>
            {
                (d as TransformManipulatorEx3D).translationGroup.IsRendering = (bool)e.NewValue;
            }));

        public bool Enable2DTranslation
        {
            get { return (bool)GetValue(Enable2DTranslationProperty); }
            set { SetValue(Enable2DTranslationProperty, value); }
        }

        public static readonly DependencyProperty Enable2DTranslationProperty =
            DependencyProperty.Register(nameof(Enable2DTranslation), typeof(bool), typeof(TransformManipulatorEx3D), new PropertyMetadata(true, (d, e) =>
            {
                (d as TransformManipulatorEx3D).translation2DGroup.IsRendering = (bool)e.NewValue;
            }));

        public bool EnableRotation
        {
            get { return (bool)GetValue(EnableRotationProperty); }
            set { SetValue(EnableRotationProperty, value); }
        }

        public static readonly DependencyProperty EnableRotationProperty =
            DependencyProperty.Register(nameof(EnableRotation), typeof(bool), typeof(TransformManipulatorEx3D), new PropertyMetadata(true, (d, e) =>
            {
                (d as TransformManipulatorEx3D).rotationGroup.IsRendering = (bool)e.NewValue;
            }));

        public bool EnableXRayGrid
        {
            get { return (bool)GetValue(EnableXRayGridProperty); }
            set { SetValue(EnableXRayGridProperty, value); }
        }

        public static readonly DependencyProperty EnableXRayGridProperty =
            DependencyProperty.Register(nameof(EnableXRayGrid), typeof(bool), typeof(TransformManipulatorEx3D), new PropertyMetadata(true, (d, e) =>
            {
                (d as TransformManipulatorEx3D).xrayEffect.IsRendering = (bool)e.NewValue;
            }));

        [TypeConverter(typeof(Vector3Converter))]
        public Vector3 CenterOffset
        {
            get { return (Vector3)GetValue(CenterOffsetProperty); }
            set { SetValue(CenterOffsetProperty, value); }
        }

        public static readonly DependencyProperty CenterOffsetProperty =
            DependencyProperty.Register(nameof(CenterOffset), typeof(Vector3), typeof(TransformManipulatorEx3D), new PropertyMetadata(Vector3.Zero, (d, e) =>
            {
                (d as TransformManipulatorEx3D).centerOffset = (Vector3)e.NewValue;
                (d as TransformManipulatorEx3D).OnUpdateSelfTransform();
            }));

        public double SizeScale
        {
            get { return (double)GetValue(SizeScaleProperty); }
            set { SetValue(SizeScaleProperty, value); }
        }

        public static readonly DependencyProperty SizeScaleProperty =
            DependencyProperty.Register(nameof(SizeScale), typeof(double), typeof(TransformManipulatorEx3D), new PropertyMetadata(1.0, (d, e) =>
            {
                (d as TransformManipulatorEx3D).sizeScale = (double)e.NewValue;
            }));

        public bool AutoSizeScale
        {
            get { return (bool)GetValue(AutoSizeScaleProperty); }
            set { SetValue(AutoSizeScaleProperty, value); }
        }

        public static readonly DependencyProperty AutoSizeScaleProperty =
            DependencyProperty.Register(nameof(AutoSizeScale), typeof(bool), typeof(TransformManipulatorEx3D), new PropertyMetadata(false, (d, e) =>
            {
                (d as TransformManipulatorEx3D).OnUpdateSelfTransform();
            }));

        public ManipulationFlags ManipulationFlags
        {
            get { return (ManipulationFlags)GetValue(ManipulationFlagsProperty); }
            set { SetValue(ManipulationFlagsProperty, value); }
        }

        public static readonly DependencyProperty ManipulationFlagsProperty =
            DependencyProperty.Register(nameof(ManipulationFlags), typeof(ManipulationFlags), typeof(TransformManipulatorEx3D), new PropertyMetadata(ManipulationFlags.Default, (d, e) =>
            {
                (d as TransformManipulatorEx3D).UpdateVisibleGeometry();
            }));

        public bool LocalAxes
        {
            get { return (bool)GetValue(LocalAxesProperty); }
            set { SetValue(LocalAxesProperty, value); }
        }

        public static readonly DependencyProperty LocalAxesProperty =
            DependencyProperty.Register(nameof(LocalAxes), typeof(bool), typeof(TransformManipulatorEx3D), new PropertyMetadata(false, (d, e) =>
            {
                (d as TransformManipulatorEx3D).OnUpdateSelfTransform();
            }));
        #endregion

        #region Variables
        private readonly MeshGeometryModel3D translationX, translationY, translationZ;
        private readonly MeshGeometryModel3D translationXY, translationYZ, translationXZ;
        private readonly MeshGeometryModel3D rotationX, rotationY, rotationZ;
        private readonly MeshGeometryModel3D scaleX, scaleY, scaleZ;
        private readonly GroupModel3D translationGroup, translation2DGroup, rotationGroup, scaleGroup, ctrlGroup;
        private readonly Element3D xrayEffect;
        private Vector3 centerOffset = Vector3.Zero;
        private Vector3 translationVector = Vector3.Zero;
        private Matrix rotationMatrix = Matrix.Identity;
        private Matrix scaleMatrix = Matrix.Identity;
        private Matrix targetMatrix = Matrix.Identity;

        private Element3D target;
        private Viewport3DX currentViewport;
        private Vector3 lastHitPosWS;
        private Vector3 normal;

        private Vector3 direction;
        private Vector3 currentHit;
        private bool isCaptured = false;
        private double sizeScale = 1;
        private Color4 currentColor;

        private Vector3 localCenterOffset
        {
            get
            {
                if ((target as IManipulatable)?.UseLocalOrigin == true)
                    return centerOffset - target.GetTotalBounds(true).Center;
                else return centerOffset;
            }
        }
        #endregion

        private enum ManipulationType
        {
            None,
            TranslationX,
            TranslationY,
            TranslationZ,
            TranslationXY,
            TranslationYZ,
            TranslationXZ,
            RotationX,
            RotationY,
            RotationZ,
            ScaleX,
            ScaleY,
            ScaleZ
        }

        private ManipulationType manipulationType = ManipulationType.None;

        public TransformManipulatorEx3D()
        {
            var rotationYMatrix = Matrix.RotationZ((float)Math.PI / 2);
            var rotationZMatrix = Matrix.RotationY(-(float)Math.PI / 2);
            var rotationYZMatrix = Matrix.RotationY(-(float)Math.PI / 2);
            var rotationXZMatrix = Matrix.RotationX((float)Math.PI / 2);
            ctrlGroup = new GroupModel3D();

            #region Translation Models
            translationX = GetGeometryModel(TranslationXGeometry, DiffuseMaterials.Red);
            translationY = GetGeometryModel(TranslationXGeometry, DiffuseMaterials.Green);
            translationZ = GetGeometryModel(TranslationXGeometry, DiffuseMaterials.Blue);
            translationY.Transform = new Media3D.MatrixTransform3D(rotationYMatrix.ToMatrix3D());
            translationZ.Transform = new Media3D.MatrixTransform3D(rotationZMatrix.ToMatrix3D());
            translationX.Mouse3DDown += Translation_Mouse3DDown;
            translationY.Mouse3DDown += Translation_Mouse3DDown;
            translationZ.Mouse3DDown += Translation_Mouse3DDown;
            translationX.Mouse3DMove += Translation_Mouse3DMove;
            translationY.Mouse3DMove += Translation_Mouse3DMove;
            translationZ.Mouse3DMove += Translation_Mouse3DMove;
            translationX.Mouse3DUp += Manipulation_Mouse3DUp;
            translationY.Mouse3DUp += Manipulation_Mouse3DUp;
            translationZ.Mouse3DUp += Manipulation_Mouse3DUp;

            translationGroup = new GroupModel3D();
            translationGroup.Children.Add(translationX);
            translationGroup.Children.Add(translationY);
            translationGroup.Children.Add(translationZ);

            ctrlGroup.Children.Add(translationGroup);
            #endregion

            #region Translation2D Models
            translationXY = GetGeometryModel(TranslationXYGeometry, DiffuseMaterials.Blue);
            translationYZ = GetGeometryModel(TranslationXYGeometry, DiffuseMaterials.Red);
            translationXZ = GetGeometryModel(TranslationXYGeometry, DiffuseMaterials.Green);
            translationYZ.Transform = new Media3D.MatrixTransform3D(rotationZMatrix.ToMatrix3D());
            translationXZ.Transform = new Media3D.MatrixTransform3D(rotationXZMatrix.ToMatrix3D());
            translationXY.Mouse3DDown += Translation_Mouse3DDown;
            translationYZ.Mouse3DDown += Translation_Mouse3DDown;
            translationXZ.Mouse3DDown += Translation_Mouse3DDown;
            translationXY.Mouse3DMove += Translation_Mouse3DMove;
            translationYZ.Mouse3DMove += Translation_Mouse3DMove;
            translationXZ.Mouse3DMove += Translation_Mouse3DMove;
            translationXY.Mouse3DUp += Manipulation_Mouse3DUp;
            translationYZ.Mouse3DUp += Manipulation_Mouse3DUp;
            translationXZ.Mouse3DUp += Manipulation_Mouse3DUp;

            translation2DGroup = new GroupModel3D();
            translation2DGroup.Children.Add(translationXY);
            translation2DGroup.Children.Add(translationYZ);
            translation2DGroup.Children.Add(translationXZ);

            ctrlGroup.Children.Add(translation2DGroup);
            #endregion

            #region Rotation Models
            rotationX = GetGeometryModel(RotationXGeometry, DiffuseMaterials.Red);
            rotationY = GetGeometryModel(RotationXGeometry, DiffuseMaterials.Green);
            rotationZ = GetGeometryModel(RotationXGeometry, DiffuseMaterials.Blue);
            rotationY.Transform = new Media3D.MatrixTransform3D(rotationYMatrix.ToMatrix3D());
            rotationZ.Transform = new Media3D.MatrixTransform3D(rotationZMatrix.ToMatrix3D());
            rotationX.Mouse3DDown += Rotation_Mouse3DDown;
            rotationY.Mouse3DDown += Rotation_Mouse3DDown;
            rotationZ.Mouse3DDown += Rotation_Mouse3DDown;
            rotationX.Mouse3DMove += Rotation_Mouse3DMove;
            rotationY.Mouse3DMove += Rotation_Mouse3DMove;
            rotationZ.Mouse3DMove += Rotation_Mouse3DMove;
            rotationX.Mouse3DUp += Manipulation_Mouse3DUp;
            rotationY.Mouse3DUp += Manipulation_Mouse3DUp;
            rotationZ.Mouse3DUp += Manipulation_Mouse3DUp;

            rotationGroup = new GroupModel3D();
            rotationGroup.Children.Add(rotationX);
            rotationGroup.Children.Add(rotationY);
            rotationGroup.Children.Add(rotationZ);
            ctrlGroup.Children.Add(rotationGroup);
            #endregion

            #region Scaling Models
            scaleX = GetGeometryModel(ScalingGeometry, DiffuseMaterials.Red);
            scaleY = GetGeometryModel(ScalingGeometry, DiffuseMaterials.Green);
            scaleZ = GetGeometryModel(ScalingGeometry, DiffuseMaterials.Blue);
            scaleY.Transform = new Media3D.MatrixTransform3D(rotationYMatrix.ToMatrix3D());
            scaleZ.Transform = new Media3D.MatrixTransform3D(rotationZMatrix.ToMatrix3D());
            scaleX.Mouse3DDown += Scaling_Mouse3DDown;
            scaleY.Mouse3DDown += Scaling_Mouse3DDown;
            scaleZ.Mouse3DDown += Scaling_Mouse3DDown;
            scaleX.Mouse3DMove += Scaling_Mouse3DMove;
            scaleY.Mouse3DMove += Scaling_Mouse3DMove;
            scaleZ.Mouse3DMove += Scaling_Mouse3DMove;
            scaleX.Mouse3DUp += Manipulation_Mouse3DUp;
            scaleY.Mouse3DUp += Manipulation_Mouse3DUp;
            scaleZ.Mouse3DUp += Manipulation_Mouse3DUp;

            scaleGroup = new GroupModel3D();
            scaleGroup.Children.Add(scaleX);
            scaleGroup.Children.Add(scaleY);
            scaleGroup.Children.Add(scaleZ);
            ctrlGroup.Children.Add(scaleGroup);
            #endregion

            Children.Add(ctrlGroup);
            xrayEffect = new PostEffectMeshXRayGrid()
            {
                EffectName = xrayEffectName,
                DimmingFactor = 0.5,
                BlendingFactor = 0.8,
                GridDensity = 4,
                GridColor = Media.Colors.Gray
            };
            (xrayEffect.SceneNode as NodePostEffectXRayGrid).XRayDrawingPassName = DefaultPassNames.EffectMeshDiffuseXRayGridP3;
            Children.Add(xrayEffect);
            SceneNode.Attached += SceneNode_OnAttached;
            SceneNode.Detached += SceneNode_OnDetached;
        }

        private MeshGeometryModel3D GetGeometryModel(Geometry3D geometry, DiffuseMaterial material) => new MeshGeometryModel3D() { Geometry = geometry, Material = material, CullMode = CullMode.Back, PostEffects = xrayEffectName };

        private void HideAllGeometry()
        {
            translationX.IsRendering = translationY.IsRendering = translationZ.IsRendering = false;
            translationXY.IsRendering = translationYZ.IsRendering = translationXZ.IsRendering = false;
            rotationX.IsRendering = rotationY.IsRendering = rotationZ.IsRendering = false;
            scaleX.IsRendering = scaleY.IsRendering = scaleZ.IsRendering = false;
        }

        private void UpdateVisibleGeometry()
        {
            var flags = ManipulationFlags;

            var obj = target as IManipulatable;
            if (obj != null)
                flags &= obj.ManipulationFlags;

            translationX.IsRendering = flags.HasFlag(ManipulationFlags.TranslateX);
            translationY.IsRendering = flags.HasFlag(ManipulationFlags.TranslateY);
            translationZ.IsRendering = flags.HasFlag(ManipulationFlags.TranslateZ);

            translationXY.IsRendering = flags.HasFlag(ManipulationFlags.TranslateXY);
            translationYZ.IsRendering = flags.HasFlag(ManipulationFlags.TranslateYZ);
            translationXZ.IsRendering = flags.HasFlag(ManipulationFlags.TranslateXZ);

            rotationX.IsRendering = flags.HasFlag(ManipulationFlags.RotateX);
            rotationY.IsRendering = flags.HasFlag(ManipulationFlags.RotateY);
            rotationZ.IsRendering = flags.HasFlag(ManipulationFlags.RotateZ);

            scaleX.IsRendering = flags.HasFlag(ManipulationFlags.ScaleX);
            scaleY.IsRendering = flags.HasFlag(ManipulationFlags.ScaleY);
            scaleZ.IsRendering = flags.HasFlag(ManipulationFlags.ScaleZ);
        }

        private void SceneNode_OnDetached(object sender, EventArgs e)
        {
            //if (target != null)
            //{
            //    target.SceneNode.OnTransformChanged -= SceneNode_OnTransformChanged;
            //}
        }

        private void SceneNode_OnAttached(object sender, EventArgs e)
        {
            OnTargetChanged(target);
        }

        protected virtual bool CanBeginTransform(MouseDown3DEventArgs e)
        {
            return true;
        }

        #region Handle Translation
        private void Translation_Mouse3DDown(object sender, MouseDown3DEventArgs e)
        {
            if (target == null || !CanBeginTransform(e))
                return;

            var overrideNormal = false;

            if (e.HitTestResult.ModelHit == translationX)
            {
                manipulationType = ManipulationType.TranslationX;
                direction = Vector3.UnitX;
            }
            else if (e.HitTestResult.ModelHit == translationY)
            {
                manipulationType = ManipulationType.TranslationY;
                direction = Vector3.UnitY;
            }
            else if (e.HitTestResult.ModelHit == translationZ)
            {
                manipulationType = ManipulationType.TranslationZ;
                direction = Vector3.UnitZ;
            }
            else if (e.HitTestResult.ModelHit == translationXY)
            {
                manipulationType = ManipulationType.TranslationXY;
                direction = Vector3.UnitZ;
                overrideNormal = true;
            }
            else if (e.HitTestResult.ModelHit == translationYZ)
            {
                manipulationType = ManipulationType.TranslationYZ;
                direction = -Vector3.UnitX;
                overrideNormal = true;
            }
            else if (e.HitTestResult.ModelHit == translationXZ)
            {
                manipulationType = ManipulationType.TranslationXZ;
                direction = -Vector3.UnitY;
                overrideNormal = true;
            }
            else
            {
                manipulationType = ManipulationType.None;
                isCaptured = false;
                return;
            }

            if (LocalAxes)
                direction = Vector3.TransformNormal(direction, rotationMatrix);

            HideAllGeometry();
            (e.HitTestResult.ModelHit as GeometryModel3D).IsRendering = true;

            var material = ((e.HitTestResult.ModelHit as MeshGeometryModel3D).Material as DiffuseMaterial);
            currentColor = material.DiffuseColor;
            material.DiffuseColor = Color.Yellow;
            currentViewport = e.Viewport;

            if (overrideNormal)
                normal = direction;
            else
            {
                var cameraNormal = Vector3.Normalize(e.Viewport.Camera.CameraInternal.LookDirection);
                lastHitPosWS = e.HitTestResult.PointHit;
                var up = Vector3.Cross(cameraNormal, direction);
                normal = Vector3.Cross(up, direction);
            }

            Vector3 hit;
            if (currentViewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWS, normal, out hit))
            {
                currentHit = hit;
                isCaptured = true;
            }
        }

        private void Translation_Mouse3DMove(object sender, MouseMove3DEventArgs e)
        {
            if (!isCaptured)
                return;

            Vector3 hit;
            if (currentViewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWS, normal, out hit))
            {
                var moveDir = hit - currentHit;
                currentHit = hit;

                if (LocalAxes)
                    moveDir = Vector3.TransformNormal(moveDir, rotationMatrix.Inverted());

                var moveVector = Vector3.Zero;
                switch (manipulationType)
                {
                    case ManipulationType.TranslationX:
                        moveVector = new Vector3(moveDir.X, 0, 0);
                        break;
                    case ManipulationType.TranslationY:
                        moveVector = new Vector3(0, moveDir.Y, 0);
                        break;
                    case ManipulationType.TranslationZ:
                        moveVector = new Vector3(0, 0, moveDir.Z);
                        break;
                    case ManipulationType.TranslationXY:
                        moveVector = new Vector3(moveDir.X, moveDir.Y, 0);
                        break;
                    case ManipulationType.TranslationYZ:
                        moveVector = new Vector3(0, moveDir.Y, moveDir.Z);
                        break;
                    case ManipulationType.TranslationXZ:
                        moveVector = new Vector3(moveDir.X, 0, moveDir.Z);
                        break;
                }

                if (LocalAxes)
                    moveVector = Vector3.TransformNormal(moveVector, rotationMatrix);

                translationVector += moveVector;

                OnUpdateSelfTransform();
                OnUpdateTargetMatrix();
            }
        }
        #endregion

        #region Handle Rotation
        private void Rotation_Mouse3DDown(object sender, MouseDown3DEventArgs e)
        {
            if (target == null || !CanBeginTransform(e))
                return;

            if (e.HitTestResult.ModelHit == rotationX)
            {
                manipulationType = ManipulationType.RotationX;
                direction = new Vector3(1, 0, 0);
            }
            else if (e.HitTestResult.ModelHit == rotationY)
            {
                manipulationType = ManipulationType.RotationY;
                direction = new Vector3(0, 1, 0);
            }
            else if (e.HitTestResult.ModelHit == rotationZ)
            {
                manipulationType = ManipulationType.RotationZ;
                direction = new Vector3(0, 0, 1);
            }
            else
            {
                manipulationType = ManipulationType.None;
                isCaptured = false;
                return;
            }

            HideAllGeometry();
            (e.HitTestResult.ModelHit as GeometryModel3D).IsRendering = true;

            var material = ((e.HitTestResult.ModelHit as MeshGeometryModel3D).Material as DiffuseMaterial);
            currentColor = material.DiffuseColor;
            material.DiffuseColor = Color.Yellow;
            currentViewport = e.Viewport;
            normal = Vector3.Normalize(e.Viewport.Camera.CameraInternal.LookDirection);
            this.lastHitPosWS = e.HitTestResult.PointHit;
            //var up = Vector3.Cross(cameraNormal, direction);
            //normal = Vector3.Cross(up, direction);

            Vector3 hit;
            if (currentViewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWS, normal, out hit))
            {
                currentHit = hit;
                isCaptured = true;
            }
        }

        private void Rotation_Mouse3DMove(object sender, MouseMove3DEventArgs e)
        {
            if (!isCaptured)
                return;

            Vector3 hit;
            if (currentViewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWS, normal, out hit))
            {
                var position = translationVector + localCenterOffset;
                var v = Vector3.Normalize(currentHit - position);
                var u = Vector3.Normalize(hit - position);
                var currentAxis = Vector3.Cross(u, v);
                var axis = Vector3.UnitX;
                currentHit = hit;

                if (LocalAxes)
                    currentAxis = Vector3.TransformNormal(currentAxis, rotationMatrix.Inverted());

                switch (manipulationType)
                {
                    case ManipulationType.RotationX:
                        axis = Vector3.UnitX;
                        break;
                    case ManipulationType.RotationY:
                        axis = Vector3.UnitY;
                        break;
                    case ManipulationType.RotationZ:
                        axis = Vector3.UnitZ;
                        break;
                }

                var rotateAxis = axis;
                if (LocalAxes)
                    rotateAxis = Vector3.TransformNormal(rotateAxis, rotationMatrix);

                var sign = -Vector3.Dot(axis, currentAxis);
                var theta = (float)(Math.Sign(sign) * Math.Asin(currentAxis.Length()));
                switch (manipulationType)
                {
                    case ManipulationType.RotationX:
                        rotationMatrix *= Matrix.RotationAxis(rotateAxis, theta);
                        break;
                    case ManipulationType.RotationY:
                        rotationMatrix *= Matrix.RotationAxis(rotateAxis, theta);
                        break;
                    case ManipulationType.RotationZ:
                        rotationMatrix *= Matrix.RotationAxis(rotateAxis, theta);
                        break;
                }
                OnUpdateTargetMatrix();
            }

        }
        #endregion

        #region Handle Scaling
        private void Scaling_Mouse3DDown(object sender, MouseDown3DEventArgs e)
        {
            if (target == null || !CanBeginTransform(e))
                return;

            if (e.HitTestResult.ModelHit == scaleX)
            {
                manipulationType = ManipulationType.ScaleX;
                direction = Vector3.UnitX;
            }
            else if (e.HitTestResult.ModelHit == scaleY)
            {
                manipulationType = ManipulationType.ScaleY;
                direction = Vector3.UnitY;
            }
            else if (e.HitTestResult.ModelHit == scaleZ)
            {
                manipulationType = ManipulationType.ScaleZ;
                direction = Vector3.UnitZ;
            }
            else
            {
                manipulationType = ManipulationType.None;
                isCaptured = false;
                return;
            }

            HideAllGeometry();
            (e.HitTestResult.ModelHit as GeometryModel3D).IsRendering = true;

            var material = ((e.HitTestResult.ModelHit as MeshGeometryModel3D).Material as DiffuseMaterial);
            currentColor = material.DiffuseColor;
            material.DiffuseColor = Color.Yellow;
            currentViewport = e.Viewport;
            var cameraNormal = Vector3.Normalize(e.Viewport.Camera.CameraInternal.LookDirection);
            this.lastHitPosWS = e.HitTestResult.PointHit;
            var up = Vector3.Cross(cameraNormal, direction);
            normal = Vector3.Cross(up, direction);

            Vector3 hit;
            if (currentViewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWS, normal, out hit))
            {
                currentHit = hit;
                isCaptured = true;
            }
        }

        private void Scaling_Mouse3DMove(object sender, MouseMove3DEventArgs e)
        {
            if (!isCaptured)
                return;

            Vector3 hit;
            if (currentViewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWS, normal, out hit))
            {
                var moveDir = hit - currentHit;
                currentHit = hit;

                if (LocalAxes)
                    moveDir = Vector3.TransformNormal(moveDir, rotationMatrix.Inverted());

                var scaleVector = Vector3.Zero;
                float scale = 1;
                switch (manipulationType)
                {
                    case ManipulationType.ScaleX:
                        scaleVector = Vector3.UnitX;
                        scale = moveDir.X;
                        break;
                    case ManipulationType.ScaleY:
                        scaleVector = Vector3.UnitY;
                        scale = moveDir.Y;
                        break;
                    case ManipulationType.ScaleZ:
                        scaleVector = Vector3.UnitZ;
                        scale = moveDir.Z;
                        break;
                }

                if ((target as IManipulatable)?.UniformScaling ?? UniformScaling)
                    scaleVector = Vector3.One;

                var axisX = Vector3.TransformNormal(Vector3.UnitX, rotationMatrix);
                var axisY = Vector3.TransformNormal(Vector3.UnitY, rotationMatrix);
                var axisZ = Vector3.TransformNormal(Vector3.UnitZ, rotationMatrix);
                var dotX = Vector3.Dot(axisX, scaleVector);
                var dotY = Vector3.Dot(axisY, scaleVector);
                var dotZ = Vector3.Dot(axisZ, scaleVector);
                scaleMatrix.M11 += scale * Math.Abs(dotX);
                scaleMatrix.M22 += scale * Math.Abs(dotY);
                scaleMatrix.M33 += scale * Math.Abs(dotZ);

                if (AutoSizeScale)
                    OnUpdateSelfTransform();

                OnUpdateTargetMatrix();
            }
        }
        #endregion

        private void Manipulation_Mouse3DUp(object sender, MouseUp3DEventArgs e)
        {
            if (isCaptured)
            {
                var material = ((e.HitTestResult.ModelHit as MeshGeometryModel3D).Material as DiffuseMaterial);
                material.DiffuseColor = currentColor;
                UpdateVisibleGeometry();
            }
            manipulationType = ManipulationType.None;
            isCaptured = false;
        }

        private void ResetTransforms()
        {
            scaleMatrix = rotationMatrix = targetMatrix = Matrix.Identity;
            translationVector = Vector3.Zero;
            OnUpdateSelfTransform();
        }
        /// <summary>
        /// Called when [target changed]. Use target boundingbox center as Manipulator center
        /// </summary>
        /// <param name="newTarget">The target.</param>
        private void OnTargetChanged(Element3D newTarget)
        {
            Debug.WriteLine("OnTargetChanged");
            if (target != null)
                target.SceneNode.TransformChanged -= SceneNode_TransformChanged;
            target = newTarget;
            if (newTarget == null)
                ResetTransforms();
            else
            {
                target.SceneNode.TransformChanged += SceneNode_TransformChanged;
                UpdateMatrixFromTarget();
            }

            UpdateVisibleGeometry();
        }

        private void SceneNode_TransformChanged(object sender, TransformArgs e)
        {
            //only do this when the change came from an external source
            if (isCaptured)
                return;

            UpdateMatrixFromTarget();
        }

        private void UpdateMatrixFromTarget()
        {
            Vector3 scale;
            Quaternion rotation;
            Vector3 translation;

            var m = target.SceneNode.ModelMatrix;
            m.Decompose(out scale, out rotation, out translation);
            scaleMatrix = Matrix.Scaling(scale);
            rotationMatrix = Matrix.RotationQuaternion(rotation);
            if (localCenterOffset != Vector3.Zero)
            {
                var org = Matrix.Translation(-localCenterOffset) * scaleMatrix * rotationMatrix * Matrix.Translation(localCenterOffset);
                translationVector = translation - org.TranslationVector;
            }
            else translationVector = m.TranslationVector;

            OnUpdateSelfTransform();
            //OnUpdateTargetMatrix();
        }

        private void OnUpdateTargetMatrix()
        {
            if (target == null)
                return;

            targetMatrix = Matrix.Translation(-localCenterOffset) * scaleMatrix * rotationMatrix * Matrix.Translation(localCenterOffset) * Matrix.Translation(translationVector);
            target.Transform = new Media3D.MatrixTransform3D(targetMatrix.ToMatrix3D());

            if (LocalAxes)
                OnUpdateSelfTransform();
        }

        private void OnUpdateSelfTransform()
        {
            var scale = sizeScale;
            if (target != null && AutoSizeScale)
            {
                Vector3 targetScale;
                Quaternion r;
                Vector3 t;

                target.TotalModelMatrix.Decompose(out targetScale, out r, out t);

                //we want to transform the bounds with scale but not rotation
                //otherwise diagonal rotations cause the box to get larger when the scale hasn't changed
                var bounds = target.GetTotalBounds(true);
                bounds = bounds.Transform(Matrix.Scaling(targetScale));

                //minBound results in tiny manipulator for big thin things like walls
                //var minBound = Math.Min(Math.Min(bounds.Width, bounds.Height), bounds.Depth);

                scale = Math.Max(0.35, bounds.Size.Length() * 0.25);
            }

            scale *= (target as IManipulatable)?.ScaleMultiplier ?? 1f;

            var m = Matrix.Translation(localCenterOffset + translationVector);
            m.M11 = m.M22 = m.M33 = (float)scale;

            if (LocalAxes)
                m = rotationMatrix * m;

            ctrlGroup.Transform = new Media3D.MatrixTransform3D(m.ToMatrix3D());
        }

        protected override SceneNode OnCreateSceneNode()
        {
            return new AlwaysHitGroupNode(this);
        }

        private sealed class AlwaysHitGroupNode : GroupNode
        {
            private readonly HashSet<object> models = new HashSet<object>();
            private readonly TransformManipulatorEx3D manipulator;
            public AlwaysHitGroupNode(TransformManipulatorEx3D manipulator)
            {
                this.manipulator = manipulator;
            }

            protected override bool OnAttach(IRenderHost host)
            {
                models.Add(manipulator.translationX);
                models.Add(manipulator.translationY);
                models.Add(manipulator.translationZ);
                models.Add(manipulator.translationXY);
                models.Add(manipulator.translationYZ);
                models.Add(manipulator.translationXZ);
                models.Add(manipulator.rotationX);
                models.Add(manipulator.rotationY);
                models.Add(manipulator.rotationZ);
                models.Add(manipulator.scaleX);
                models.Add(manipulator.scaleY);
                models.Add(manipulator.scaleZ);
                return base.OnAttach(host);
            }
            protected override bool OnHitTest(RenderContext context, Matrix totalModelMatrix, ref Ray ray, ref List<HitTestResult> hits)
            {
                //Set hit distance to 0 so event manipulator is inside the model, hit test still works
                if (base.OnHitTest(context, totalModelMatrix, ref ray, ref hits))
                {
                    if (hits.Count > 0)
                    {
                        HitTestResult res = new HitTestResult() { Distance = float.MaxValue };
                        foreach (var hit in hits)
                        {
                            if (models.Contains(hit.ModelHit))
                            {
                                if (hit.Distance < res.Distance)
                                { res = hit; }
                            }
                        }
                        res.Distance = 0;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
