using Silk.NET.Maths;
using System.Numerics;

namespace Lab2
{
    public class CameraDescriptor
    {
        public Vector3D<float> Position { get; private set; }
        public Vector3D<float> Target { get; private set; }
        private float zyAngle = 0.0f;
        private float zxAngle = 0.0f;
        private const float Distance = 5.0f;

        public CameraDescriptor()
        {
            UpdatePosition();
        }

        /// <summary>
        /// Gets the up vector of the camera.
        /// </summary>
        public Vector3D<float> UpVector
        {
            get
            {
                return Vector3D<float>.Normalize(GetPointFromAngles(Distance, zyAngle, zxAngle + Math.PI / 2));
            }
        }

        public void IncreaseDistance()
        {
            Distance += 0.1f;
            UpdatePosition();
        }

        public void DecreaseDistance()
        {
            Distance = Math.Max(0.1f, Distance - 0.1f);
            UpdatePosition();
        }

        public void IncreaseZYAngle()
        {
            zyAngle += 0.1f;
            UpdatePosition();
        }

        public void DecreaseZYAngle()
        {
            zyAngle -= 0.1f;
            UpdatePosition();
        }

        public void IncreaseZXAngle()
        {
            zxAngle = Math.Min(zxAngle + 0.1f, Math.PI / 2 - 0.1f);
            UpdatePosition();
        }

        public void DecreaseZXAngle()
        {
            zxAngle = Math.Max(zxAngle - 0.1f, -Math.PI / 2 + 0.1f);
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            float x = Distance * (float)Math.Sin(zyAngle) * (float)Math.Cos(zxAngle);
            float y = Distance * (float)Math.Sin(zxAngle);
            float z = Distance * (float)Math.Cos(zyAngle) * (float)Math.Cos(zxAngle);

            Position = new Vector3D<float>(x, y, z);
            Target = Vector3D<float>.Zero;
        }

        private static Vector3D<float> GetPointFromAngles(float distance, float angleToMinZYPlane, float angleToMinZXPlane)
        {
            var x = distance * (float)Math.Cos(angleToMinZXPlane) * (float)Math.Sin(angleToMinZYPlane);
            var z = distance * (float)Math.Cos(angleToMinZXPlane) * (float)Math.Cos(angleToMinZYPlane);
            var y = distance * (float)Math.Sin(angleToMinZXPlane);

            return new Vector3D<float>(x, y, z);
        }
    }
}
