using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Reclaimer.Models
{
    public class InstancePlacement : BindableBase
    {
        private float transformScale;
        public float TransformScale
        {
            get { return transformScale; }
            set { SetProperty(ref transformScale, value); }
        }

        #region Matrix Properties
        public Matrix4x4 Transform
        {
            get { return GetTransform(); }
            set { SetTransform(value); }
        }

        //these need to be individual properties
        //so the ui can bind to them and also 
        //trigger/receive change events for them

        private float m11;
        public float M11
        {
            get { return m11; }
            set { SetTransformProperty(ref m11, value); }
        }

        private float m12;
        public float M12
        {
            get { return m12; }
            set { SetTransformProperty(ref m12, value); }
        }

        private float m13;
        public float M13
        {
            get { return m13; }
            set { SetTransformProperty(ref m13, value); }
        }

        private float m21;
        public float M21
        {
            get { return m21; }
            set { SetTransformProperty(ref m21, value); }
        }

        private float m22;
        public float M22
        {
            get { return m22; }
            set { SetTransformProperty(ref m22, value); }
        }

        private float m23;
        public float M23
        {
            get { return m23; }
            set { SetTransformProperty(ref m23, value); }
        }

        private float m31;
        public float M31
        {
            get { return m31; }
            set { SetTransformProperty(ref m31, value); }
        }

        private float m32;
        public float M32
        {
            get { return m32; }
            set { SetTransformProperty(ref m32, value); }
        }

        private float m33;
        public float M33
        {
            get { return m33; }
            set { SetTransformProperty(ref m33, value); }
        }

        private float m41;
        public float M41
        {
            get { return m41; }
            set { SetTransformProperty(ref m41, value); }
        }

        private float m42;
        public float M42
        {
            get { return m42; }
            set { SetTransformProperty(ref m42, value); }
        }

        private float m43;
        public float M43
        {
            get { return m43; }
            set { SetTransformProperty(ref m43, value); }
        }
        #endregion

        private int sectionIndex;
        public int SectionIndex
        {
            get { return sectionIndex; }
            set { SetProperty(ref sectionIndex, value); }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private void SetTransformProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (SetProperty(ref storage, value, propertyName))
                RaisePropertyChanged(nameof(Transform));
        }

        public Matrix4x4 GetTransform()
        {
            return new Matrix4x4
            {
                M11 = M11,
                M12 = M12,
                M13 = M13,
                M21 = M21,
                M22 = M22,
                M23 = M23,
                M31 = M31,
                M32 = M32,
                M33 = M33,
                M41 = M41,
                M42 = M42,
                M43 = M43,
                M44 = 1f
            };
        }

        public void SetTransform(Matrix4x4 transform)
        {
            M11 = transform.M11;
            M12 = transform.M12;
            M13 = transform.M13;
            M21 = transform.M21;
            M22 = transform.M22;
            M23 = transform.M23;
            M31 = transform.M31;
            M32 = transform.M32;
            M33 = transform.M33;
            M41 = transform.M41;
            M42 = transform.M42;
            M43 = transform.M43;
        }

        public override string ToString() => Name;
    }
}
