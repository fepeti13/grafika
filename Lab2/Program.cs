﻿using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Lab2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();
        private static CubeArrangementModel cubeArrangementModel = new();
        
        private static IWindow window;
        private static GL Gl;
        private static uint program;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string SliceRotationMatrixVariableName = "uSliceRotation";

        private static List<GlCube> rubikCubes = new();
        private static List<Vector3D<float>> rubikPositions = new();
        private static List<bool> cubeInRotatingSlice = new();
        
        private static int currentSlice = 1;
        private static int sliceAxis = 1;
        private static bool isRotating = false;
        private static float currentRotationAngle = 0.0f;
        private static float targetRotationAngle = 0.0f;
        private static float rotationSpeed = 3.0f;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uSliceRotation;

        out vec4 outCol;
        
        void main()
        {
            outCol = vCol;
            gl_Position = uProjection * uView * uSliceRotation * uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

        in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";// X-axis

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Rubik's Cube";
            windowOptions.Size = new Vector2D<int>(800, 600);

            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
            IInputContext inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();
            Gl.ClearColor(0.5f, 0.5f, 0.5f, 1.0f);

            SetUpRubikCube();
            LinkProgram();

            Gl.Enable(EnableCap.CullFace);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    cameraDescriptor.DecreaseZYAngle();
                    break;
                case Key.Right:
                    cameraDescriptor.IncreaseZYAngle();
                    break;
                case Key.Down:
                    cameraDescriptor.IncreaseDistance();
                    break;
                case Key.Up:
                    cameraDescriptor.DecreaseDistance();
                    break;
                case Key.W:
                    cameraDescriptor.IncreaseZXAngle();
                    break;
                case Key.S:
                    cameraDescriptor.DecreaseZXAngle();
                    break;
                case Key.Space:
                    if (!isRotating)
                    {
                        isRotating = true;
                        currentRotationAngle = 0.0f;
                        targetRotationAngle = (float)Math.PI / 2.0f;
                        UpdateSliceMembers();
                    }
                    break;
                case Key.Backspace:
                    if (!isRotating)
                    {
                        isRotating = true;
                        currentRotationAngle = 0.0f;
                        targetRotationAngle = -(float)Math.PI / 2.0f;
                        UpdateSliceMembers();
                    }
                    break;
                case Key.X:
                    sliceAxis = 0; 
                    currentSlice = 1; 
                    break;
                case Key.Y:
                    sliceAxis = 1; 
                    currentSlice = 1; 
                    break;
                case Key.Z:
                    sliceAxis = 2; 
                    currentSlice = 1; 
                    break;
                case Key.Number1:
                    currentSlice = -1; 
                    break;
                case Key.Number2:
                    currentSlice = 0; 
                    break;
                case Key.Number3:
                    currentSlice = 1; 
                    break;
            }
        }

        private static void UpdateSliceMembers()
        {
            
            cubeInRotatingSlice = new List<bool>();
            
            
            for (int i = 0; i < rubikPositions.Count; i++)
            {
                Vector3D<float> position = rubikPositions[i];
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

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                throw new Exception($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static unsafe void SetUpRubikCube()
        {
            float spacing = 1.1f;

            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                float[] red     = [1.0f, 0.0f, 0.0f, 1.0f];
                float[] orange  = [1.0f, 0.5f, 0.0f, 1.0f];
                float[] white   = [1.0f, 1.0f, 1.0f, 1.0f];
                float[] yellow  = [1.0f, 1.0f, 0.0f, 1.0f];
                float[] blue    = [0.0f, 0.0f, 1.0f, 1.0f];
                float[] green   = [0.0f, 1.0f, 0.0f, 1.0f];
                float[] black   = [0.2f, 0.2f, 0.2f, 1.0f];

                float[] face1 = y == 1 ? white : black;
                float[] face2 = z == 1 ? red : black;
                float[] face3 = x == -1 ? blue : black;
                float[] face4 = y == -1 ? yellow : black;
                float[] face5 = z == -1 ? orange : black;
                float[] face6 = x == 1 ? green : black;

                var cube = GlCube.CreateCubeWithFaceColors(Gl, face1, face2, face3, face4, face5, face6);
                
                rubikCubes.Add(cube);
                rubikPositions.Add(new Vector3D<float>(x * spacing, y * spacing, z * spacing));
                cubeInRotatingSlice.Add(false);
            }
        }

        private static void Window_Update(double deltaTime)
        {
            
            if (isRotating)
            {
                float rotationDelta = (float)(rotationSpeed * deltaTime);
                
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
        }

        private static void FinishRotation()
        {
            
            for (int i = 0; i < rubikPositions.Count; i++)
            {
                if (cubeInRotatingSlice[i])
                {
                    Vector3D<float> position = rubikPositions[i];
                    Vector3D<float> rotatedPosition = RotatePointAroundAxis(
                        position, 
                        Vector3D<float>.Zero, 
                        GetRotationAxis(), 
                        targetRotationAngle);
                    
                    
                    rotatedPosition.X = (float)Math.Round(rotatedPosition.X * 2) / 2;
                    rotatedPosition.Y = (float)Math.Round(rotatedPosition.Y * 2) / 2;
                    rotatedPosition.Z = (float)Math.Round(rotatedPosition.Z * 2) / 2;
                    
                    rubikPositions[i] = rotatedPosition;
                }
            }

            
            isRotating = false;
            currentRotationAngle = 0;
        }

        private static Vector3D<float> GetRotationAxis()
        {
            switch (sliceAxis)
            {
                case 0: return new Vector3D<float>(1, 0, 0); 
                case 1: return new Vector3D<float>(0, 1, 0); 
                case 2: return new Vector3D<float>(0, 0, 1); 
                default: return new Vector3D<float>(0, 1, 0); 
            }
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

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            for (int i = 0; i < rubikCubes.Count; i++)
            {
                var cube = rubikCubes[i];
                var position = rubikPositions[i];
                bool inSlice = cubeInRotatingSlice[i];
                
                Gl.BindVertexArray(cube.Vao);

                
                var modelMatrix = Matrix4X4.CreateTranslation(position);
                int modelLocation = Gl.GetUniformLocation(program, ModelMatrixVariableName);
                if (modelLocation == -1)
                {
                    throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
                }
                Gl.UniformMatrix4(modelLocation, 1, false, (float*)&modelMatrix);

                
                Matrix4X4<float> sliceRotationMatrix;
                
                if (isRotating && inSlice)
                {
                    
                    if (sliceAxis == 0)
                        sliceRotationMatrix = Matrix4X4.CreateRotationX(currentRotationAngle);
                    else if (sliceAxis == 1)
                        sliceRotationMatrix = Matrix4X4.CreateRotationY(currentRotationAngle);
                    else
                        sliceRotationMatrix = Matrix4X4.CreateRotationZ(currentRotationAngle);
                }
                else
                {
                    
                    sliceRotationMatrix = Matrix4X4<float>.Identity;
                }
                
                int sliceRotationLocation = Gl.GetUniformLocation(program, SliceRotationMatrixVariableName);
                if (sliceRotationLocation == -1)
                {
                    throw new Exception($"{SliceRotationMatrixVariableName} uniform not found on shader.");
                }
                Gl.UniformMatrix4(sliceRotationLocation, 1, false, (float*)&sliceRotationMatrix);

                Gl.DrawElements(GLEnum.Triangles, cube.IndexArrayLength, GLEnum.UnsignedInt, null);
            }

            Gl.BindVertexArray(0);
        }

        private static void Window_Closing()
        {
            foreach (var cube in rubikCubes)
            {
                cube.ReleaseGlCube();
            }
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>(
                (float)Math.PI / 4f, 
                800f / 600f,          
                0.1f,                
                100.0f               
            );
            
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ProjectionMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(
                cameraDescriptor.Position, 
                cameraDescriptor.Target, 
                cameraDescriptor.UpVector);
                
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}