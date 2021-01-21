using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Media = System.Windows.Media;
using Media3D = System.Windows.Media.Media3D;

using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Utilities;
using Reclaimer.Utilities;

namespace Reclaimer.Controls
{
    public class BoxManipulator3D : GroupElement3D, IMeshNode
    {
        #region Dependency Properties
        public static readonly DependencyProperty ForwardVectorProperty =
            DependencyProperty.Register(nameof(ForwardVector), typeof(Vector3), typeof(BoxManipulator3D), new PropertyMetadata(Vector3.UnitX, (d, e) =>
            {
                (d as BoxManipulator3D).UpdateTransform();
            }));

        public static readonly DependencyProperty UpVectorProperty =
            DependencyProperty.Register(nameof(UpVector), typeof(Vector3), typeof(BoxManipulator3D), new PropertyMetadata(Vector3.UnitZ, (d, e) =>
            {
                (d as BoxManipulator3D).UpdateTransform();
            }));

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register(nameof(Position), typeof(Vector3), typeof(BoxManipulator3D), new PropertyMetadata(Vector3.Zero, (d, e) =>
            {
                (d as BoxManipulator3D).UpdateTransform();
            }));

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(Vector3), typeof(BoxManipulator3D), new PropertyMetadata(Vector3.One, (d, e) =>
            {
                (d as BoxManipulator3D).UpdateTransform();
            }));

        public static readonly DependencyProperty DiffuseColorProperty =
            DependencyProperty.Register(nameof(DiffuseColor), typeof(Color), typeof(BoxManipulator3D), new PropertyMetadata(Color.Red, (d, e) =>
            {
                (d as BoxManipulator3D).OnDiffuseColorChanged();
            }));

        [TypeConverter(typeof(Vector3Converter))]
        public Vector3 ForwardVector
        {
            get { return (Vector3)GetValue(ForwardVectorProperty); }
            set { SetValue(ForwardVectorProperty, value); }
        }

        [TypeConverter(typeof(Vector3Converter))]
        public Vector3 UpVector
        {
            get { return (Vector3)GetValue(UpVectorProperty); }
            set { SetValue(UpVectorProperty, value); }
        }

        [TypeConverter(typeof(Vector3Converter))]
        public Vector3 Position
        {
            get { return (Vector3)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        [TypeConverter(typeof(Vector3Converter))]
        public Vector3 Size
        {
            get { return (Vector3)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        public Color DiffuseColor
        {
            get { return (Color)GetValue(DiffuseColorProperty); }
            set { SetValue(DiffuseColorProperty, value); }
        }
        #endregion

        private readonly MeshGeometryModel3D negX, negY, negZ;
        private readonly MeshGeometryModel3D posX, posY, posZ;
        private readonly GroupModel3D facesGroup;

        private Viewport3DX currentViewport;
        private Vector3 lastHitPosWS;
        private Vector3 normal;

        private Vector3 direction;
        private Vector3 currentHit;
        private bool isCaptured = false;

        private enum ManipulationType
        {
            None, PosX, PosY, PosZ, SizeX, SizeY, SizeZ
        }

        private ManipulationType manipulationType = ManipulationType.None;

        public BoxManipulator3D()
        {
            facesGroup = new GroupModel3D();

            var mb = new MeshBuilder();
            mb.AddFaceNX();
            negX = CreateMesh(mb);

            mb = new MeshBuilder();
            mb.AddFaceNY();
            negY = CreateMesh(mb);

            mb = new MeshBuilder();
            mb.AddFaceNZ();
            negZ = CreateMesh(mb);

            mb = new MeshBuilder();
            mb.AddFacePX();
            posX = CreateMesh(mb);

            mb = new MeshBuilder();
            mb.AddFacePY();
            posY = CreateMesh(mb);

            mb = new MeshBuilder();
            mb.AddFacePZ();
            posZ = CreateMesh(mb);

            negX.Mouse3DDown += Manipulation_Mouse3DDown;
            negY.Mouse3DDown += Manipulation_Mouse3DDown;
            negZ.Mouse3DDown += Manipulation_Mouse3DDown;
            posX.Mouse3DDown += Manipulation_Mouse3DDown;
            posY.Mouse3DDown += Manipulation_Mouse3DDown;
            posZ.Mouse3DDown += Manipulation_Mouse3DDown;

            negX.Mouse3DMove += Manipulation_Mouse3DMove;
            negY.Mouse3DMove += Manipulation_Mouse3DMove;
            negZ.Mouse3DMove += Manipulation_Mouse3DMove;
            posX.Mouse3DMove += Manipulation_Mouse3DMove;
            posY.Mouse3DMove += Manipulation_Mouse3DMove;
            posZ.Mouse3DMove += Manipulation_Mouse3DMove;

            negX.Mouse3DUp += Manipulation_Mouse3DUp;
            negY.Mouse3DUp += Manipulation_Mouse3DUp;
            negZ.Mouse3DUp += Manipulation_Mouse3DUp;
            posX.Mouse3DUp += Manipulation_Mouse3DUp;
            posY.Mouse3DUp += Manipulation_Mouse3DUp;
            posZ.Mouse3DUp += Manipulation_Mouse3DUp;

            facesGroup.Children.Add(negX);
            facesGroup.Children.Add(negY);
            facesGroup.Children.Add(negZ);
            facesGroup.Children.Add(posX);
            facesGroup.Children.Add(posY);
            facesGroup.Children.Add(posZ);

            Children.Add(facesGroup);
        }

        private MeshGeometryModel3D CreateMesh(MeshBuilder mb) => new MeshGeometryModel3D { Material = new DiffuseMaterial { DiffuseColor = DiffuseColor }, Geometry = mb.ToMesh() };

        private void OnDiffuseColorChanged()
        {
            (negX.Material as DiffuseMaterial).DiffuseColor = DiffuseColor;
            (negY.Material as DiffuseMaterial).DiffuseColor = DiffuseColor;
            (negZ.Material as DiffuseMaterial).DiffuseColor = DiffuseColor;
            (posX.Material as DiffuseMaterial).DiffuseColor = DiffuseColor;
            (posY.Material as DiffuseMaterial).DiffuseColor = DiffuseColor;
            (posZ.Material as DiffuseMaterial).DiffuseColor = DiffuseColor;

            var isTransparent = DiffuseColor.A < byte.MaxValue;
            negX.IsTransparent = isTransparent;
            negY.IsTransparent = isTransparent;
            negZ.IsTransparent = isTransparent;
            posX.IsTransparent = isTransparent;
            posY.IsTransparent = isTransparent;
            posZ.IsTransparent = isTransparent;
        }

        private void Manipulation_Mouse3DDown(object sender, MouseDown3DEventArgs e)
        {
            if (e.HitTestResult.ModelHit == negX)
            {
                manipulationType = ManipulationType.PosX;
                direction = Vector3.UnitX;
            }
            else if (e.HitTestResult.ModelHit == negY)
            {
                manipulationType = ManipulationType.PosY;
                direction = Vector3.UnitY;
            }
            else if (e.HitTestResult.ModelHit == negZ)
            {
                manipulationType = ManipulationType.PosZ;
                direction = Vector3.UnitZ;
            }
            else if (e.HitTestResult.ModelHit == posX)
            {
                manipulationType = ManipulationType.SizeX;
                direction = Vector3.UnitX;
            }
            else if (e.HitTestResult.ModelHit == posY)
            {
                manipulationType = ManipulationType.SizeY;
                direction = Vector3.UnitY;
            }
            else if (e.HitTestResult.ModelHit == posZ)
            {
                manipulationType = ManipulationType.SizeZ;
                direction = Vector3.UnitZ;
            }
            else
            {
                manipulationType = ManipulationType.None;
                isCaptured = false;
                return;
            }

            var material = ((e.HitTestResult.ModelHit as MeshGeometryModel3D).Material as DiffuseMaterial);
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

        private void Manipulation_Mouse3DMove(object sender, MouseMove3DEventArgs e)
        {
            if (!isCaptured)
                return;

            Vector3 hit;
            if (currentViewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWS, normal, out hit))
            {
                var rotationMatrix = GetRotationMatrix();

                var moveDir = hit - currentHit;
                currentHit = hit;

                moveDir = Vector3.TransformNormal(moveDir, rotationMatrix.Inverted());

                var sizeDir = Vector3.Zero;
                switch (manipulationType)
                {
                    case ManipulationType.PosX:
                        moveDir = new Vector3(moveDir.X, 0, 0);
                        sizeDir -= moveDir;
                        break;
                    case ManipulationType.PosY:
                        moveDir = new Vector3(0, moveDir.Y, 0);
                        sizeDir -= moveDir;
                        break;
                    case ManipulationType.PosZ:
                        moveDir = new Vector3(0, 0, moveDir.Z);
                        sizeDir -= moveDir;
                        break;
                    case ManipulationType.SizeX:
                        sizeDir = new Vector3(moveDir.X, 0, 0);
                        moveDir = Vector3.Zero;
                        break;
                    case ManipulationType.SizeY:
                        sizeDir = new Vector3(0, moveDir.Y, 0);
                        moveDir = Vector3.Zero;
                        break;
                    case ManipulationType.SizeZ:
                        sizeDir = new Vector3(0, 0, moveDir.Z);
                        moveDir = Vector3.Zero;
                        break;
                }

                moveDir = Vector3.TransformNormal(moveDir, rotationMatrix);
                //dont re-transform sizeDir as it should always be axis aligned

                Position += moveDir;
                Size += sizeDir;
            }
        }

        private void Manipulation_Mouse3DUp(object sender, MouseUp3DEventArgs e)
        {
            if (isCaptured)
            {
                var material = ((e.HitTestResult.ModelHit as MeshGeometryModel3D).Material as DiffuseMaterial);
                material.DiffuseColor = DiffuseColor;
            }
            manipulationType = ManipulationType.None;
            isCaptured = false;
        }

        private Matrix GetRotationMatrix()
        {
            var rightVector = Vector3.Cross(ForwardVector, UpVector);

            var yaw = Vector3.UnitX.AngleBetween(ForwardVector);
            if (ForwardVector.Y < 0) yaw *= -1;

            var pitch = Vector3.UnitZ.AngleBetween(UpVector);
            if (ForwardVector.X > 0) pitch *= -1;

            return Matrix.RotationZ(yaw) * Matrix.RotationAxis(rightVector, pitch);
        }

        private void UpdateTransform()
        {
            var m = Matrix.Scaling(Size);
            m *= GetRotationMatrix();
            m *= Matrix.Translation(Position);
            facesGroup.Transform = new Media3D.MatrixTransform3D(m.ToMatrix3D());
        }

        #region IMeshNode
        string IMeshNode.Name => Name;

        bool IMeshNode.IsVisible
        {
            get { return IsRendering; }
            set { IsRendering = value; }
        }

        BoundingBox IMeshNode.GetNodeBounds()
        {
            return this.GetTotalBounds();
        } 
        #endregion
    }
}
