using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Maths;

namespace GrafikaSzeminarium
{
    internal class CubeArrangementModel
    {
        /// <summary>
        /// Gets or sets wheather the animation should run or it should be frozen.
        /// </summary>
        public bool AnimationEnabled { get; set; } = false;

        /// <summary>
        /// The time of the simulation. It helps to calculate time dependent values.
        /// </summary>
        private double Time { get; set; } = 0;

        /// <summary>
        /// The value by which the center cube is scaled. It varies between 0.8 and 1.2 with respect to the original size.
        /// </summary>
        public double CenterCubeScale { get; private set; } = 1;

        /// <summary>
        /// The angle with which the diamond cube is rotated around the diagonal from bottom right front to top left back.
        /// </summary>
        public double DiamondCubeLocalAngle { get; private set; } = 0;

        /// <summary>
        /// The angle with which the diamond cube is rotated around the global Y axes.
        /// </summary>
        public double DiamondCubeGlobalYAngle { get; private set; } = 0;

        private List<Vector3D<float>> cubePositions = new();
        private List<bool> cubeInRotatingSlice = new();
        private int currentSlice = 1;
        private int sliceAxis = 1;
        private bool isRotating = false;
        private float currentRotationAngle = 0.0f;
        private float targetRotationAngle = 0.0f;
        private float rotationSpeed = 3.0f;

        public void InitializeCubePositions()
        {
            cubePositions.Clear();
            cubeInRotatingSlice.Clear();
            float spacing = 1.1f;

            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                cubePositions.Add(new Vector3D<float>(x * spacing, y * spacing, z * spacing));
                cubeInRotatingSlice.Add(false);
            }
        }

        public List<Vector3D<float>> GetCubePositions() => cubePositions;
        public List<bool> GetCubeInRotatingSlice() => cubeInRotatingSlice;
        public int GetCurrentSlice() => currentSlice;
        public int GetSliceAxis() => sliceAxis;
        public bool IsRotating() => isRotating;
        public float GetCurrentRotationAngle() => currentRotationAngle;
        public float GetTargetRotationAngle() => targetRotationAngle;

        public void SetSliceAxis(int axis)
        {
            sliceAxis = axis;
            currentSlice = 1;
            UpdateSliceMembers();
        }

        public void SetCurrentSlice(int slice)
        {
            currentSlice = slice;
            UpdateSliceMembers();
        }

        public void StartRotation(bool clockwise)
        {
            if (!isRotating)
            {
                isRotating = true;
                currentRotationAngle = 0.0f;
                targetRotationAngle = clockwise ? (float)Math.PI / 2.0f : -(float)Math.PI / 2.0f;
                UpdateSliceMembers();
            }
        }

        public void UpdateRotation(float deltaTime)
        {
            if (!isRotating) return;

            float rotationDelta = rotationSpeed * deltaTime;
            
            if (targetRotationAngle > 0)
            {
                currentRotationAngle += rotationDelta;
                if (currentRotationAngle >= targetRotationAngle)
                {
                    currentRotationAngle = targetRotationAngle;
                    FinishRotation();
                }
            }
            else
            {
                currentRotationAngle -= rotationDelta;
                if (currentRotationAngle <= targetRotationAngle)
                {
                    currentRotationAngle = targetRotationAngle;
                    FinishRotation();
                }
            }
        }

        private void UpdateSliceMembers()
        {
            cubeInRotatingSlice = new List<bool>();
            
            for (int i = 0; i < cubePositions.Count; i++)
            {
                Vector3D<float> position = cubePositions[i];
                bool inSlice = false;
                
                switch (sliceAxis)
                {
                    case 0: 
                        inSlice = Math.Round(position.X) == currentSlice;
                        break;
                    case 1: 
                        inSlice = Math.Round(position.Y) == currentSlice;
                        break;
                    case 2: 
                        inSlice = Math.Round(position.Z) == currentSlice;
                        break;
                }
                
                cubeInRotatingSlice.Add(inSlice);
            }
        }

        private void FinishRotation()
        {
            for (int i = 0; i < cubePositions.Count; i++)
            {
                if (cubeInRotatingSlice[i])
                {
                    Vector3D<float> position = cubePositions[i];
                    Vector3D<float> rotatedPosition = RotatePointAroundAxis(
                        position, 
                        Vector3D<float>.Zero, 
                        GetRotationAxis(), 
                        targetRotationAngle);
                    
                    rotatedPosition.X = (float)Math.Round(rotatedPosition.X * 2) / 2;
                    rotatedPosition.Y = (float)Math.Round(rotatedPosition.Y * 2) / 2;
                    rotatedPosition.Z = (float)Math.Round(rotatedPosition.Z * 2) / 2;
                    
                    cubePositions[i] = rotatedPosition;
                }
            }

            isRotating = false;
            currentRotationAngle = 0;
        }

        private Vector3D<float> GetRotationAxis()
        {
            switch (sliceAxis)
            {
                case 0: return new Vector3D<float>(1, 0, 0); 
                case 1: return new Vector3D<float>(0, 1, 0); 
                case 2: return new Vector3D<float>(0, 0, 1); 
                default: return new Vector3D<float>(0, 1, 0); 
            }
        }

        private Vector3D<float> RotatePointAroundAxis(
            Vector3D<float> point, 
            Vector3D<float> pivot, 
            Vector3D<float> axis, 
            float angle)
        {
            Vector3D<float> translated = point - pivot;
            
            Matrix4X4<float> rotationMatrix;
            
            if (axis.X == 1)
                rotationMatrix = Matrix4X4.CreateRotationX(angle);
            else if (axis.Y == 1)
                rotationMatrix = Matrix4X4.CreateRotationY(angle);
            else 
                rotationMatrix = Matrix4X4.CreateRotationZ(angle);
            
            Vector4D<float> rotated = Vector4D.Transform(
                new Vector4D<float>(translated.X, translated.Y, translated.Z, 1), 
                rotationMatrix);
            
            return new Vector3D<float>(rotated.X, rotated.Y, rotated.Z) + pivot;
        }

        internal void AdvanceTime(double deltaTime)
        {
            // we do not advance the simulation when animation is stopped
            if (!AnimationEnabled)
                return;

            // set a simulation time
            Time += deltaTime;

            // lets produce an oscillating scale in time
            CenterCubeScale = 1 + 0.2 * Math.Sin(1.5 * Time);

            // the rotation angle is time x angular velocity;
            DiamondCubeLocalAngle = Time * 10;

            DiamondCubeGlobalYAngle = -Time;
        }
    }
}
