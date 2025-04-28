using Silk.NET.Maths;
using System.Numerics;
using System.Collections.Generic;

namespace Lab2
{
    internal class CubeArrangementModel
    {
        /// <summary>
        /// Gets or sets wheather the animation should run or it should be frozen.
        /// </summary>
        public bool AnimationEnabeld { get; set; } = false;

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
        public double DiamondCubeAngleOwnRevolution { get; private set; } = 0;

        /// <summary>
        /// The angle with which the diamond cube is rotated around the diagonal from bottom right front to top left back.
        /// </summary>
        public double DiamondCubeAngleRevolutionOnGlobalY { get; private set; } = 0;

        private readonly List<Vector3D<float>> positions = new();
        private readonly List<bool> inSlice = new();
        private int currentSlice = 1;
        private int sliceAxis = 1;
        private bool isRotating = false;
        private float currentRotationAngle = 0.0f;
        private float targetRotationAngle = 0.0f;
        private float rotationSpeed = 3.0f;

        public CubeArrangementModel()
        {
            float spacing = 1.1f;
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                positions.Add(new Vector3D<float>(x * spacing, y * spacing, z * spacing));
                inSlice.Add(false);
            }
        }

        public IReadOnlyList<Vector3D<float>> Positions => positions;
        public IReadOnlyList<bool> InSlice => inSlice;
        public int CurrentSlice => currentSlice;
        public int SliceAxis => sliceAxis;
        public bool IsRotating => isRotating;
        public float CurrentRotationAngle => currentRotationAngle;
        public float TargetRotationAngle => targetRotationAngle;
        public float RotationSpeed => rotationSpeed;

        public void SetCurrentSlice(int slice)
        {
            currentSlice = slice;
            UpdateSliceMembers();
        }

        public void SetSliceAxis(int axis)
        {
            sliceAxis = axis;
            UpdateSliceMembers();
        }

        public void StartRotation(float angle)
        {
            targetRotationAngle = angle;
            isRotating = true;
        }

        public void Update(float deltaTime)
        {
            if (!isRotating) return;

            float delta = rotationSpeed * deltaTime;
            if (Math.Abs(targetRotationAngle - currentRotationAngle) <= delta)
            {
                currentRotationAngle = targetRotationAngle;
                isRotating = false;
            }
            else
            {
                currentRotationAngle += delta * Math.Sign(targetRotationAngle - currentRotationAngle);
            }

            for (int i = 0; i < positions.Count; i++)
            {
                if (inSlice[i])
                {
                    Vector3D<float> position = positions[i];
                    Vector3D<float> rotatedPosition = RotatePointAroundAxis(
                        position,
                        Vector3D<float>.Zero,
                        GetRotationAxis(),
                        currentRotationAngle);
                    positions[i] = rotatedPosition;
                }
            }
        }

        private void UpdateSliceMembers()
        {
            inSlice.Clear();
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3D<float> position = positions[i];
                bool inCurrentSlice = false;
                
                switch (sliceAxis)
                {
                    case 0:
                        inCurrentSlice = Math.Round(position.X) == currentSlice;
                        break;
                    case 1:
                        inCurrentSlice = Math.Round(position.Y) == currentSlice;
                        break;
                    case 2:
                        inCurrentSlice = Math.Round(position.Z) == currentSlice;
                        break;
                }
                
                inSlice.Add(inCurrentSlice);
            }
        }

        private Vector3D<float> GetRotationAxis()
        {
            return sliceAxis switch
            {
                0 => Vector3D<float>.UnitX,
                1 => Vector3D<float>.UnitY,
                2 => Vector3D<float>.UnitZ,
                _ => Vector3D<float>.UnitY
            };
        }

        private static Vector3D<float> RotatePointAroundAxis(
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
            if (!AnimationEnabeld)
                return;

            // set a simulation time
            Time += deltaTime;

            // lets produce an oscillating scale in time
            CenterCubeScale = 1 + 0.2 * Math.Sin(1.5 * Time);

            DiamondCubeAngleOwnRevolution = Time * 10;

            DiamondCubeAngleRevolutionOnGlobalY = -Time;
        }
    }
}
