﻿using System.Xml;
using ImGuiNET;
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
        private static GlObject colladaModel;  

        private static float Shininess = 50;
        private static float ColladaModelRotation = 0.0f;  

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
            options.Title = "COLLADA Model Viewer";
            options.Size = new Vector2D<int>(800, 600);
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
                case Key.R: ColladaModelRotation += 0.1f; break;  
                case Key.L: ColladaModelRotation -= 0.1f; break;  
            }
        }

        private static unsafe void Window_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
            controller.Update((float)deltaTime);
            
             
            if (cubeArrangementModel.AnimationEnabeld)
            {
                ColladaModelRotation += (float)(deltaTime * 0.5);
            }
        }

        private static unsafe void SetUpObjects()
        {
            float[] face1Color = [1f, 0f, 0f, 1f];
            float[] tableColor = [0.94f, 1f, 1f, 1f];  
            float[] airboatColor = [0.7f, 0.7f, 1f, 1f];  
            float[] colladaColor = [0.2f, 0.8f, 0.2f, 1f];  

             
            string colladaFilePath = "cube.dae";
            if (!File.Exists(colladaFilePath))
            {
                SaveColladaSampleFile(colladaFilePath);
            }

             
            string complexColladaFilePath = "complex_model.dae";
            string colladaPathToUse = File.Exists(complexColladaFilePath) ? complexColladaFilePath : colladaFilePath;

            teapot = ObjResourceReader.CreateTeapotWithColor(Gl, face1Color);
            table = GlCube.CreateSquare(Gl, tableColor);
            glCubeRotating = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face1Color, face1Color, face1Color, face1Color, face1Color);
            
            try
            {
                airboatModel = ObjResourceReader.CreateFromObjFile(Gl, "airboat.obj", airboatColor);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading airboat model: {ex.Message}");
                 
                airboatModel = GlCube.CreateCubeWithFaceColors(Gl, airboatColor, airboatColor, airboatColor, airboatColor, airboatColor, airboatColor);
            }
            
             
            try
            {
                colladaModel = ColladaResourceReader.CreateFromColladaFile(Gl, colladaPathToUse, colladaColor);
                Console.WriteLine($"Successfully loaded COLLADA model from: {colladaPathToUse}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading COLLADA model: {ex.Message}");
                 
                colladaModel = GlCube.CreateCubeWithFaceColors(Gl, colladaColor, colladaColor, colladaColor, colladaColor, colladaColor, colladaColor);
            }
        }

        private static void SaveColladaSampleFile(string filePath)
        {
             
            string colladaContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<COLLADA xmlns=""http://www.collada.org/2005/11/COLLADASchema"" version=""1.4.1"">
    <asset>
        <contributor>
            <authoring_tool>SceneKit Collada Exporter v1.0</authoring_tool>
        </contributor>
        <created>2018-10-25T16:29:03Z</created>
        <modified>2018-10-25T16:29:03Z</modified>
        <unit meter=""1.000000""/>
        <up_axis>Y_UP</up_axis>
    </asset>
    <library_materials>
        <material id=""Blue"" name=""Blue"">
            <instance_effect url=""#effect_Blue""/>
        </material>
    </library_materials>
    <library_effects>
        <effect id=""effect_Blue"">
            <profile_COMMON>
                <technique sid=""common"">
                    <phong>
                        <ambient>
                            <color>0 0 0 1</color>
                        </ambient>
                        <diffuse>
                            <color>0.137255 0.403922 0.870588 1</color>
                        </diffuse>
                        <specular>
                            <color>0.5 0.5 0.5 1</color>
                        </specular>
                        <shininess>
                            <float>16</float>
                        </shininess>
                        <transparent opaque=""A_ONE"">
                            <color>0 0 0 1</color>
                        </transparent>
                        <transparency>
                            <float>1</float>
                        </transparency>
                        <index_of_refraction>
                            <float>1</float>
                        </index_of_refraction>
                    </phong>
                </technique>
            </profile_COMMON>
        </effect>
    </library_effects>
    <library_geometries>
        <geometry id=""F1"" name=""Face1Geometry"">
            <mesh>
                <source id=""cube-vertex-positions"">
                    <float_array id=""ID2-array"" count=""72"">-50 50 50 -50 -50 50 50 -50 50 50 50 50 -50 50 50 50 50 50 50 50 -50 -50 50 -50 -50 -50 -50 50 -50 -50 50 -50 50 -50 -50 50 -50 50 50 -50 50 -50 -50 -50 -50 -50 -50 50 50 -50 50 50 -50 -50 50 50 -50 50 50 50 50 50 -50 50 -50 -50 -50 -50 -50 -50 50 -50</float_array>
                    <technique_common>
                        <accessor source=""#ID2-array"" count=""24"" stride=""3"">
                            <param name=""X"" type=""float""/>
                            <param name=""Y"" type=""float""/>
                            <param name=""Z"" type=""float""/>
                        </accessor>
                    </technique_common>
                </source>
                <vertices id=""cube-vertices"">
                    <input semantic=""POSITION"" source=""#cube-vertex-positions""/>
                </vertices>
                <triangles count=""12"" material=""geometryElement5"">
                    <input semantic=""VERTEX"" offset=""0"" source=""#cube-vertices""/>
                    <p>0 1 2 0 2 3 4 5 6 4 6 7 8 9 10 8 10 11 12 13 14 12 14 15 16 17 18 16 18 19 20 21 22 20 22 23</p>
                </triangles>
            </mesh>
        </geometry>
    </library_geometries>
    <library_visual_scenes>
        <visual_scene id=""reportScene"">
            <node id=""F1"" name=""Face1"">
                <instance_geometry url=""#F1"">
                    <bind_material>
                        <technique_common>
                            <instance_material symbol=""geometryElement5"" target=""#Blue""/>
                        </technique_common>
                    </bind_material>
                </instance_geometry>
            </node>
        </visual_scene>
    </library_visual_scenes>
    <scene>
        <instance_visual_scene url=""#reportScene""/>
    </scene>
</COLLADA>";
            
            File.WriteAllText(filePath, colladaContent);
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
            DrawAirboatModel();
            DrawColladaModel();  

            ImGuiNET.ImGui.Begin("Rendering Controls", ImGuiWindowFlags.AlwaysAutoResize);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref Shininess, 1, 200);
            ImGuiNET.ImGui.SliderFloat("COLLADA Rotation", ref ColladaModelRotation, 0, 6.28f);
            bool animation = cubeArrangementModel.AnimationEnabeld;
            if (ImGuiNET.ImGui.Checkbox("Animation", ref animation))
            {
                cubeArrangementModel.AnimationEnabeld = animation;
            }
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

        private static unsafe void DrawAirboatModel()
        {
            SetModelMatrix(Matrix4X4.CreateTranslation(-2f, 0f, 0f));
            Gl.BindVertexArray(airboatModel.Vao);
            Gl.DrawElements(GLEnum.Triangles, airboatModel.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawColladaModel()
        {
             
            var rotY = Matrix4X4.CreateRotationY(ColladaModelRotation);
            var trans = Matrix4X4.CreateTranslation(2f, 0f, 2f);
            var scale = Matrix4X4.CreateScale(0.03f);  
            
            var model = scale * rotY * trans;
            SetModelMatrix(model);

            Gl.BindVertexArray(colladaModel.Vao);
            Gl.DrawElements(GLEnum.Triangles, colladaModel.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void Window_Closing()
        {
            teapot.ReleaseGlObject();
            table.ReleaseGlObject();
            glCubeRotating.ReleaseGlObject();
            airboatModel.ReleaseGlObject();
            colladaModel.ReleaseGlObject();  
        }
        
        private static unsafe void SetModelMatrix(Matrix4X4<float> model)
        {
            int loc = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            Gl.UniformMatrix4(loc, 1, false, (float*)&model);

             
            Matrix4X4<float> normalMat = model;
            normalMat.M41 = normalMat.M42 = normalMat.M43 = 0;  
            Matrix4X4.Invert(normalMat, out var inv);
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