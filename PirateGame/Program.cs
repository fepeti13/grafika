﻿using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Numerics;

namespace PirateShootingGame
{
    internal static class Program
    {
        private static GameState gameState = new();
        private static IWindow window;
        private static IInputContext inputContext;
        private static GL Gl;
        private static ImGuiController controller;
        private static uint program;

        private static GlObject pirateModel;
        private static GlObject bulletModel;
        private static GlObject houseModel;
        private static GlObject groundModel;
        private static GlObject playerModel;

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

        private static readonly string FragmentShaderSource = @"
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
            float ambientStrength = 0.3;
            vec3 ambient = ambientStrength * lightColor;

            float diffuseStrength = 0.5;
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor * diffuseStrength;

            float specularStrength = 0.4;
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
            options.Title = "Pirate Shooting Game";
            options.Size = new Vector2D<int>(1024, 768);
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
            Gl.ClearColor(0.5f, 0.8f, 1.0f, 1.0f); 

            SetUpObjects();
            LinkProgram();
            gameState.Initialize();

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(TriangleFace.Back);
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
                case Key.W: gameState.Player.MoveForward(); break;
                case Key.S: gameState.Player.MoveBackward(); break;
                case Key.A: gameState.Player.TurnLeft(); break;
                case Key.D: gameState.Player.TurnRight(); break;
                case Key.Space: gameState.ShootBullet(); break;
                case Key.R: gameState.RestartGame(); break;
                case Key.Escape: window.Close(); break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            gameState.Update((float)deltaTime);
            controller.Update((float)deltaTime);
        }

        private static void SetUpObjects()
        {
            float[] pirateColor = [0.9f, 0.7f, 0.5f, 1f]; 
            float[] bulletColor = [1f, 1f, 0f, 1f]; 
            float[] houseColor = [0.8f, 0.6f, 0.4f, 1f]; 
            float[] groundColor = [0.2f, 0.7f, 0.2f, 1f]; 
            float[] playerColor = [0.2f, 0.2f, 0.8f, 1f]; 

            
            try
            {
                pirateModel = ObjResourceReader.CreateFromObjFile(Gl, "Resources/pirates.obj", pirateColor);
                Console.WriteLine("Successfully loaded pirates.obj");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load pirates.obj: {ex.Message}");
                pirateModel = GlCube.CreateCubeWithFaceColors(Gl, pirateColor, pirateColor, pirateColor, pirateColor, pirateColor, pirateColor);
            }

            try
            {
                bulletModel = ObjResourceReader.CreateFromObjFile(Gl, "Resources/bullets.obj", bulletColor);
            }
            catch
            {
                bulletModel = GlCube.CreateCubeWithFaceColors(Gl, bulletColor, bulletColor, bulletColor, bulletColor, bulletColor, bulletColor);
            }

            try
            {
                houseModel = ObjResourceReader.CreateFromObjFile(Gl, "Resources/houses.obj", houseColor);
                Console.WriteLine("Successfully loaded houses.obj");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load houses.obj: {ex.Message}");
                houseModel = GlCube.CreateCubeWithFaceColors(Gl, houseColor, houseColor, houseColor, houseColor, houseColor, houseColor);
            }

            
            groundModel = GlCube.CreateSquare(Gl, groundColor);

            
            playerModel = GlCube.CreateCubeWithFaceColors(Gl, playerColor, playerColor, playerColor, playerColor, playerColor, playerColor);
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

            
            SetModelMatrix(Matrix4X4.CreateScale(50f));
            Gl.BindVertexArray(groundModel.Vao);
            Gl.DrawElements(GLEnum.Triangles, groundModel.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            
            var playerTransform = Matrix4X4.CreateScale(0.5f) * 
                                Matrix4X4.CreateRotationY(gameState.Player.Rotation) * 
                                Matrix4X4.CreateTranslation(gameState.Player.Position.X, 0.5f, gameState.Player.Position.Z);
            SetModelMatrix(playerTransform);
            Gl.BindVertexArray(playerModel.Vao);
            Gl.DrawElements(GLEnum.Triangles, playerModel.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            
            foreach (var pirate in gameState.Pirates)
            {
                if (pirate.IsAlive)
                {
                    var pirateTransform = Matrix4X4.CreateScale(0.01f) *  
                                        Matrix4X4.CreateRotationX(-MathF.PI / 2) *  
                                        Matrix4X4.CreateRotationY(pirate.Rotation) * 
                                        Matrix4X4.CreateTranslation(pirate.Position.X, 0.5f, pirate.Position.Z);
                    SetModelMatrix(pirateTransform);
                    Gl.BindVertexArray(pirateModel.Vao);
                    Gl.DrawElements(GLEnum.Triangles, pirateModel.IndexArrayLength, GLEnum.UnsignedInt, null);
                    Gl.BindVertexArray(0);
                }
            }

            
            foreach (var bullet in gameState.Bullets)
            {
                if (bullet.IsActive)
                {
                    var bulletTransform = Matrix4X4.CreateScale(0.1f) * 
                                        Matrix4X4.CreateTranslation(bullet.Position.X, bullet.Position.Y, bullet.Position.Z);
                    SetModelMatrix(bulletTransform);
                    Gl.BindVertexArray(bulletModel.Vao);
                    Gl.DrawElements(GLEnum.Triangles, bulletModel.IndexArrayLength, GLEnum.UnsignedInt, null);
                    Gl.BindVertexArray(0);
                }
            }

            
            foreach (var house in gameState.Houses)
            {
                var houseTransform = Matrix4X4.CreateScale(0.02f) *  
                                   Matrix4X4.CreateRotationZ(MathF.PI / 2) *  
                                   Matrix4X4.CreateTranslation(house.X, 0f, house.Z);  
                SetModelMatrix(houseTransform);
                Gl.BindVertexArray(houseModel.Vao);
                Gl.DrawElements(GLEnum.Triangles, houseModel.IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);
            }

            
            DrawUI();
            controller.Render();
        }

        private static void DrawUI()
        {
            ImGui.Begin("Game Stats", ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.Text($"Pirates Defeated: {gameState.PiratesDefeated}");
            ImGui.Text($"Pirates Remaining: {gameState.Pirates.Count(p => p.IsAlive)}");
            ImGui.Text($"Active Bullets: {gameState.Bullets.Count(b => b.IsActive)}");
            ImGui.Separator();
            ImGui.Text("Controls:");
            ImGui.Text("WASD - Move/Turn");
            ImGui.Text("SPACE - Shoot");
            ImGui.Text("R - Restart Game");
            ImGui.Text("ESC - Exit");
            
            if (gameState.Pirates.All(p => !p.IsAlive))
            {
                ImGui.Separator();
                ImGui.TextColored(new Vector4(0, 1, 0, 1), "YOU WIN!");
                ImGui.Text("Press R to restart");
            }
            
            ImGui.End();
        }

        private static void Window_Closing()
        {
            pirateModel?.ReleaseGlObject();
            bulletModel?.ReleaseGlObject();
            houseModel?.ReleaseGlObject();
            groundModel?.ReleaseGlObject();
            playerModel?.ReleaseGlObject();
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> model)
        {
            int loc = Gl.GetUniformLocation(program, "uModel");
            Gl.UniformMatrix4(loc, 1, false, (float*)&model);

            model.M41 = model.M42 = model.M43 = 0;
            Matrix4X4.Invert(model, out var inv);
            Matrix3X3<float> norm = new Matrix3X3<float>(Matrix4X4.Transpose(inv));
            loc = Gl.GetUniformLocation(program, "uNormal");
            Gl.UniformMatrix3(loc, 1, false, (float*)&norm);
        }

        private static unsafe void SetViewMatrix()
        {
            var cameraPos = new Vector3D<float>(
                gameState.Player.Position.X - (float)Math.Sin(gameState.Player.Rotation) * 5f,
                3f,
                gameState.Player.Position.Z - (float)Math.Cos(gameState.Player.Rotation) * 5f
            );
            var target = new Vector3D<float>(gameState.Player.Position.X, 1f, gameState.Player.Position.Z);
            var up = Vector3D<float>.UnitY;

            var view = Matrix4X4.CreateLookAt(cameraPos, target, up);
            int loc = Gl.GetUniformLocation(program, "uView");
            Gl.UniformMatrix4(loc, 1, false, (float*)&view);
        }

        private static unsafe void SetProjectionMatrix()
        {
            var proj = Matrix4X4.CreatePerspectiveFieldOfView((float)Math.PI / 4f, 1024f / 768f, 0.1f, 100f);
            int loc = Gl.GetUniformLocation(program, "uProjection");
            Gl.UniformMatrix4(loc, 1, false, (float*)&proj);
        }

        private static unsafe void SetLightColor()
        {
            int loc = Gl.GetUniformLocation(program, "lightColor");
            Gl.Uniform3(loc, 1f, 1f, 0.9f);
        }

        private static unsafe void SetLightPosition()
        {
            int loc = Gl.GetUniformLocation(program, "lightPos");
            Gl.Uniform3(loc, 0f, 20f, 0f);
        }

        private static unsafe void SetViewerPosition()
        {
            int loc = Gl.GetUniformLocation(program, "viewPos");
            var pos = new Vector3D<float>(
                gameState.Player.Position.X - (float)Math.Sin(gameState.Player.Rotation) * 5f,
                3f,
                gameState.Player.Position.Z - (float)Math.Cos(gameState.Player.Rotation) * 5f
            );
            Gl.Uniform3(loc, pos.X, pos.Y, pos.Z);
        }

        private static unsafe void SetShininess()
        {
            int loc = Gl.GetUniformLocation(program, "shininess");
            Gl.Uniform1(loc, 32f);
        }
    }
}