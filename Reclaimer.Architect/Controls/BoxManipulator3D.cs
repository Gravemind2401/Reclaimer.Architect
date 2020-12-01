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

namespace Reclaimer.Controls
{
    public class BoxManipulator3D : GroupElement3D
    {
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
                var moveDir = hit - currentHit;
                currentHit = hit;

                switch (manipulationType)
                {
                    case ManipulationType.PosX:
                        Position += new Vector3(moveDir.X, 0, 0);
                        Size -= new Vector3(moveDir.X, 0, 0);
                        break;
                    case ManipulationType.PosY:
                        Position += new Vector3(0, moveDir.Y, 0);
                        Size -= new Vector3(0, moveDir.Y, 0);
                        break;
                    case ManipulationType.PosZ:
                        Position += new Vector3(0, 0, moveDir.Z);
                        Size -= new Vector3(0, 0, moveDir.Z);
                        break;
                    case ManipulationType.SizeX:
                        Size += new Vector3(moveDir.X, 0, 0);
                        break;
                    case ManipulationType.SizeY:
                        Size += new Vector3(0, moveDir.Y, 0);
                        break;
                    case ManipulationType.SizeZ:
                        Size += new Vector3(0, 0, moveDir.Z);
                        break;
                }
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

        private void UpdateTransform()
        {
            var m = Matrix.Scaling(Size);
            m *= Matrix.Translation(Position);
            facesGroup.Transform = new Media3D.MatrixTransform3D(m.ToMatrix3D());
        }
    }
}
