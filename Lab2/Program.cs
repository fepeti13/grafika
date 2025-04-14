﻿using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Lab2
{
    internal static class Program
    {
        private static IWindow window;
        private static GL Gl;
        private static uint program;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static List<GlCube> rubikCubes = new();
        private static List<Vector3D<float>> rubikPositions = new();

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec4 outCol;
        
        void main()
        {
            outCol = vCol;
            gl_Position = uProjection * uView * uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0);
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
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Rubik's Cube";
            windowOptions.Size = new Vector2D<int>(800, 600);

            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
            Gl = window.CreateOpenGL();
            Gl.ClearColor(0.5f, 0.5f, 0.5f, 1.0f);

            SetUpRubikCube();
            LinkProgram();

            Gl.Enable(EnableCap.CullFace);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
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
            }
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            foreach (var cube in rubikCubes)
            {
                Gl.BindVertexArray(cube.Vao);

                var position = rubikPositions[rubikCubes.IndexOf(cube)];
                var modelMatrix = Matrix4X4.CreateTranslation(position);

                int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
                if (location == -1)
                {
                    throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
                }
                Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);

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
            var cameraPosition = new Vector3D<float>(5.0f, 4.5f, 6.0f);
            var cameraTarget = new Vector3D<float>(0.0f, 0.0f, 0.0f);
            var upVector = new Vector3D<float>(0.0f, 1.0f, 0.0f);

            var viewMatrix = Matrix4X4.CreateLookAt(cameraPosition, cameraTarget, upVector);
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
