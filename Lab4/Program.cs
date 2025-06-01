﻿using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();
        private static CubeArrangementModel cubeArrangementModel = new();
        private static IWindow window;
        private static IInputContext inputContext;
        private static GL Gl;
        private static ImGuiController controller;
        private static uint program;

        private static GlObject teapot;
        private static GlObject table;
        private static GlCube glCubeRotating;
        private static GlObject airboatModel;

        private static float Shininess = 50;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNorm;

        uniform mat4 uModel;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;

        void main()
        {
            outCol = vCol;
            gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
            outNormal = uNormal * vNorm;
            outWorldPosition = vec3(uModel * vec4(vPos, 1.0));
        }
        ";

        private const string FragmentShaderSource = @"
        #version 330 core

        uniform vec3 lightColor;
        uniform vec3 lightPos;
        uniform vec3 viewPos;
        uniform float shininess;

        out vec4 FragColor;

        in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        void main()
        {
            float ambientStrength = 0.2;
            vec3 ambient = ambientStrength * lightColor;

            float diffuseStrength = 0.3;
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor * diffuseStrength;

            float specularStrength = 0.5;
            vec3 viewDir = normalize(viewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess);
            vec3 specular = specularStrength * spec * lightColor;

            vec3 result = (ambient + diffuse + specular) * outCol.xyz;
            FragColor = vec4(result, outCol.w);
        }
        ";

        static void Main(string[] args)
        {
            var options = WindowOptions.Default;
            options.Title = "2 szeminárium";
            options.Size = new Vector2D<int>(500, 500);
            options.PreferredDepthBufferBits = 24;

            window = Window.Create(options);
            window.Load += Window_Load;
            window.Render += Window_Render;
            window.Update += Window_Update;
            window.Closing += Window_Closing;

            window.Run();
        }
        private static void Window_Load()
        {
            inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
                keyboard.KeyDown += Keyboard_KeyDown;

            Gl = window.CreateOpenGL();
            controller = new ImGuiController(Gl, window, inputContext);

            window.FramebufferResize += s => Gl.Viewport(s);
            Gl.ClearColor(System.Drawing.Color.Black);

            SetUpObjects();
            LinkProgram();

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int status);
            if (status == 0)
                throw new Exception("Vertex shader error: " + Gl.GetShaderInfoLog(vshader));

            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out status);
            if (status == 0)
                throw new Exception("Program link error: " + Gl.GetProgramInfoLog(program));

            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }
        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left: cameraDescriptor.DecreaseZYAngle(); break;
                case Key.Right: cameraDescriptor.IncreaseZYAngle(); break;
                case Key.Down: cameraDescriptor.IncreaseDistance(); break;
                case Key.Up: cameraDescriptor.DecreaseDistance(); break;
                case Key.U: cameraDescriptor.IncreaseZXAngle(); break;
                case Key.D: cameraDescriptor.DecreaseZXAngle(); break;
                case Key.Space: cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld; break;
            }
        }

        private static unsafe void Window_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
            controller.Update((float)deltaTime);
        }

        private static unsafe void SetUpObjects()
        {
            float[] face1Color = [1f, 0f, 0f, 1f];
            float[] tableColor = [0.94f, 1f, 1f, 1f]; // Azure
            float[] airboatColor = [0.7f, 0.7f, 1f, 1f]; // világoskék

            teapot = ObjResourceReader.CreateTeapotWithColor(Gl, face1Color);
            table = GlCube.CreateSquare(Gl, tableColor);
            glCubeRotating = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face1Color, face1Color, face1Color, face1Color, face1Color);
            airboatModel = ObjResourceReader.CreateFromObjFile(Gl, "Resources/airboat.obj", airboatColor);
        }
        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();
            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();

            DrawPulsingTeapot();
            DrawRevolvingCube();

            SetModelMatrix(Matrix4X4.CreateTranslation(-2f, 0f, 0f));
            Gl.BindVertexArray(airboatModel.Vao);
            Gl.DrawElements(GLEnum.Triangles, airboatModel.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            ImGuiNET.ImGui.Begin("Lighting", ImGuiWindowFlags.AlwaysAutoResize);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref Shininess, 1, 200);
            ImGuiNET.ImGui.End();

            controller.Render();
        }

        private static unsafe void DrawPulsingTeapot()
        {
            var scale = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            SetModelMatrix(scale);
            Gl.BindVertexArray(teapot.Vao);
            Gl.DrawElements(GLEnum.Triangles, teapot.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            SetModelMatrix(Matrix4X4<float>.Identity);
            Gl.BindVertexArray(table.Vao);
            Gl.DrawElements(GLEnum.Triangles, table.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawRevolvingCube()
        {
            var rot = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleRevolutionOnGlobalY);
            var trans = Matrix4X4.CreateTranslation(4f, 4f, 0f);
            var model = Matrix4X4.CreateScale(1f) * rot * trans;
            SetModelMatrix(model);

            Gl.BindVertexArray(glCubeRotating.Vao);
            Gl.DrawElements(GLEnum.Triangles, glCubeRotating.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void Window_Closing()
        {
            teapot.ReleaseGlObject();
            table.ReleaseGlObject();
            glCubeRotating.ReleaseGlObject();
            airboatModel.ReleaseGlObject();
        }
        private static unsafe void SetModelMatrix(Matrix4X4<float> model)
        {
            int loc = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            Gl.UniformMatrix4(loc, 1, false, (float*)&model);

            model.M41 = model.M42 = model.M43 = 0;
            Matrix4X4.Invert(model, out var inv);
            Matrix3X3<float> norm = new Matrix3X3<float>(Matrix4X4.Transpose(inv));
            loc = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            Gl.UniformMatrix3(loc, 1, false, (float*)&norm);
        }

        private static unsafe void SetViewMatrix()
        {
            var view = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int loc = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            Gl.UniformMatrix4(loc, 1, false, (float*)&view);
        }

        private static unsafe void SetProjectionMatrix()
        {
            var proj = Matrix4X4.CreatePerspectiveFieldOfView((float)Math.PI / 4f, 1f, 0.1f, 100f);
            int loc = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            Gl.UniformMatrix4(loc, 1, false, (float*)&proj);
        }

        private static unsafe void SetLightColor()
        {
            int loc = Gl.GetUniformLocation(program, "lightColor");
            Gl.Uniform3(loc, 1f, 1f, 1f);
        }

        private static unsafe void SetLightPosition()
        {
            int loc = Gl.GetUniformLocation(program, "lightPos");
            Gl.Uniform3(loc, 0f, 10f, 0f);
        }

        private static unsafe void SetViewerPosition()
        {
            int loc = Gl.GetUniformLocation(program, "viewPos");
            var pos = cameraDescriptor.Position;
            Gl.Uniform3(loc, pos.X, pos.Y, pos.Z);
        }

        private static unsafe void SetShininess()
        {
            int loc = Gl.GetUniformLocation(program, "shininess");
            Gl.Uniform1(loc, Shininess);
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL error: " + error);
        }
    }
}
