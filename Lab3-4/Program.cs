using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Numerics;
using System.Reflection;
using Szeminarium;

namespace GrafikaSzeminarium
{
    internal class Program
    {
        private static IWindow graphicWindow;
        private static GL Gl;
        private static ImGuiController imGuiController;
        
         
        private static ModelObjectDescriptor barrelWithFlatNormals;
        private static ModelObjectDescriptor barrelWithAngledNormals;

        private static CameraDescriptor camera = new CameraDescriptor();
        private static CubeArrangementModel cubeArrangementModel = new CubeArrangementModel();

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";

        private const string ShinenessVariableName = "uShininess";

        private static float shininess = 50;
        private static uint phongProgram;  // Program using Phong shading
        private static uint gouraudProgram; // Program using Gouraud shading
        private static uint activeProgram;  // Currently active program
        private static bool usePhongShading = true; // Default to Phong shading
        private static readonly uint numberOfRectangles = 18;

        private static List<Matrix4X4<float>> rectangleModelMatrices = new List<Matrix4X4<float>>();

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Grafika szeminárium";
            windowOptions.Size = new Vector2D<int>(800, 800);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Closing()
        {
            barrelWithFlatNormals.Dispose();
            barrelWithAngledNormals.Dispose();
            Gl.DeleteProgram(phongProgram);
            Gl.DeleteProgram(gouraudProgram);
        }

        private static void GraphicWindow_Load()
        {
            Gl = graphicWindow.CreateOpenGL();

            var inputContext = graphicWindow.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

             
            graphicWindow.FramebufferResize += s =>
            {
                Gl.Viewport(s);
            };

            imGuiController = new ImGuiController(Gl, graphicWindow, inputContext);

             
            SetUpBarrels();

            Gl.ClearColor(System.Drawing.Color.White);
            
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(TriangleFace.Back);
             

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            // Create both shader programs
            CreateShaderPrograms();
            
            // Set the active program to Phong (default)
            activeProgram = phongProgram;
        }

        private static void CreateShaderPrograms()
        {
            // Create Phong shader program
            uint phongVShader = Gl.CreateShader(ShaderType.VertexShader);
            uint phongFShader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(phongVShader, GetEmbeddedResourceAsString("Shaders.VertexShader.vert"));
            Gl.CompileShader(phongVShader);
            Gl.GetShader(phongVShader, ShaderParameterName.CompileStatus, out int phongVStatus);
            if (phongVStatus != (int)GLEnum.True)
                throw new Exception("Phong vertex shader failed to compile: " + Gl.GetShaderInfoLog(phongVShader));

            Gl.ShaderSource(phongFShader, GetEmbeddedResourceAsString("Shaders.FragmentShader.frag"));
            Gl.CompileShader(phongFShader);
            Gl.GetShader(phongFShader, ShaderParameterName.CompileStatus, out int phongFStatus);
            if (phongFStatus != (int)GLEnum.True)
                throw new Exception("Phong fragment shader failed to compile: " + Gl.GetShaderInfoLog(phongFShader));

            phongProgram = Gl.CreateProgram();
            Gl.AttachShader(phongProgram, phongVShader);
            Gl.AttachShader(phongProgram, phongFShader);
            Gl.LinkProgram(phongProgram);

            Gl.DetachShader(phongProgram, phongVShader);
            Gl.DetachShader(phongProgram, phongFShader);
            Gl.DeleteShader(phongVShader);
            Gl.DeleteShader(phongFShader);

            Gl.GetProgram(phongProgram, GLEnum.LinkStatus, out var phongStatus);
            if (phongStatus == 0)
            {
                Console.WriteLine($"Error linking Phong shader {Gl.GetProgramInfoLog(phongProgram)}");
            }
            
            // Create Gouraud shader program
            uint gouraudVShader = Gl.CreateShader(ShaderType.VertexShader);
            uint gouraudFShader = Gl.CreateShader(ShaderType.FragmentShader);


            Gl.ShaderSource(gouraudVShader, GetEmbeddedResourceAsString("Shaders.gouraudVertexShader.vert"));
            Gl.CompileShader(gouraudVShader);
            Gl.GetShader(gouraudVShader, ShaderParameterName.CompileStatus, out int gouraudVStatus);
            if (gouraudVStatus != (int)GLEnum.True)
                throw new Exception("Gouraud vertex shader failed to compile: " + Gl.GetShaderInfoLog(gouraudVShader));

            Gl.ShaderSource(gouraudFShader,  GetEmbeddedResourceAsString("Shaders.gouraudFragmentShader.frag"));
            Gl.CompileShader(gouraudFShader);
            Gl.GetShader(gouraudFShader, ShaderParameterName.CompileStatus, out int gouraudFStatus);
            if (gouraudFStatus != (int)GLEnum.True)
                throw new Exception("Gouraud fragment shader failed to compile: " + Gl.GetShaderInfoLog(gouraudFShader));

            gouraudProgram = Gl.CreateProgram();
            Gl.AttachShader(gouraudProgram, gouraudVShader);
            Gl.AttachShader(gouraudProgram, gouraudFShader);
            Gl.LinkProgram(gouraudProgram);

            Gl.DetachShader(gouraudProgram, gouraudVShader);
            Gl.DetachShader(gouraudProgram, gouraudFShader);
            Gl.DeleteShader(gouraudVShader);
            Gl.DeleteShader(gouraudFShader);

            Gl.GetProgram(gouraudProgram, GLEnum.LinkStatus, out var gouraudStatus);
            if (gouraudStatus == 0)
            {
                Console.WriteLine($"Error linking Gouraud shader {Gl.GetProgramInfoLog(gouraudProgram)}");
            }
        }

        private static unsafe void SetUpBarrels()
        {
            float height = 2.0f;  
            float width = 1.0f;   
            float depth = 0.1f;   
             
            float angleStep = 360f / numberOfRectangles;
            float halfAngleRad = (angleStep / 2) * MathF.PI / 180f;  
             
            float radius = (width / 2) / MathF.Sin(halfAngleRad);

            barrelWithFlatNormals = ModelObjectDescriptor.CreateRectangleWithFlatNormals(Gl, width, height, depth);
            
            barrelWithAngledNormals = ModelObjectDescriptor.CreateRectangleWithAngledNormals(Gl, width, height, depth);

            rectangleModelMatrices.Clear();
            for (int i = 0; i < numberOfRectangles; i++)
            {
                float currentAngleDeg = i * angleStep;
                float currentAngleRad = MathF.PI * currentAngleDeg / 180f;

                float x = radius * MathF.Sin(currentAngleRad);
                float z = radius * MathF.Cos(currentAngleRad);

                var modelMatrixRectangle =
                    Matrix4X4.CreateRotationY(currentAngleRad) *
                    Matrix4X4.CreateTranslation(x, 0f, z);

                rectangleModelMatrices.Add(modelMatrixRectangle);
            }
        }

        private static string GetEmbeddedResourceAsString(string resourceRelativePath)
        {
            string resourceFullPath = Assembly.GetExecutingAssembly().GetName().Name + "." + resourceRelativePath;

            using (var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullPath))
            using (var resStreamReader = new StreamReader(resStream))
            {
                var text = resStreamReader.ReadToEnd();
                return text;
            }
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    camera.DecreaseZYAngle();
                    break;
                case Key.Right:
                    camera.IncreaseZYAngle();
                    break;
                case Key.Down:
                    camera.IncreaseDistance();
                    break;
                case Key.Up:
                    camera.DecreaseDistance();
                    break;
                case Key.U:
                    camera.IncreaseZXAngle();
                    break;
                case Key.D:
                    camera.DecreaseZXAngle();
                    break;
                case Key.Space:
                    cubeArrangementModel.AnimationEnabled = !cubeArrangementModel.AnimationEnabled;
                    break;
            }
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
            imGuiController.Update((float)deltaTime);
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            // Select active program based on user choice
            activeProgram = usePhongShading ? phongProgram : gouraudProgram;
            Gl.UseProgram(activeProgram);

             
            SetUniform3(LightColorVariableName, new Vector3(1f, 1f, 1f));
            
             
            Vector3 lightPosition = new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z);
            SetUniform3(LightPositionVariableName, lightPosition);
            
            SetUniform3(ViewPositionVariableName, new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
            SetUniform1(ShinenessVariableName, shininess);

             
            var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Target, camera.UpVector);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 2), 
                graphicWindow.Size.X / (float)graphicWindow.Size.Y, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);

             
            Matrix4X4<float> upperBarrelMatrix = Matrix4X4.CreateTranslation(0f, 2f, 0f);
            for (int i = 0; i < numberOfRectangles; i++)
            {
                var modelMatrix = rectangleModelMatrices[i] * upperBarrelMatrix;
                SetModelMatrix(modelMatrix);
                DrawModelObject(barrelWithFlatNormals);
            }


            Matrix4X4<float> lowerBarrelMatrix = Matrix4X4.CreateTranslation(0f, -2f, 0f);
            for (int i = 0; i < numberOfRectangles; i++)
            {
                var modelMatrix = rectangleModelMatrices[i] * lowerBarrelMatrix;
                SetModelMatrix(modelMatrix);
                DrawModelObject(barrelWithAngledNormals);
            }

             
            ImGuiNET.ImGui.Begin("Lighting Controls", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize | ImGuiNET.ImGuiWindowFlags.NoCollapse);
            ImGuiNET.ImGui.Text("Top barrel: Flat normals (perpendicular to face)");
            ImGuiNET.ImGui.Text("Bottom barrel: Angled normals (10° outward)");
            
            // Add radio buttons for shading model selection
            if (ImGuiNET.ImGui.RadioButton("Phong Shading", usePhongShading))
            {
                usePhongShading = true;
            }
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.RadioButton("Gouraud Shading", !usePhongShading))
            {
                usePhongShading = false;
            }
            
            //ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 5, 100);
            ImGuiNET.ImGui.End();

            imGuiController.Render();
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            SetMatrix(modelMatrix, ModelMatrixVariableName);

             
            int location = Gl.GetUniformLocation(activeProgram, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }

             
            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));

            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUniform1(string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(activeProgram, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform1(location, uniformValue);
            CheckError();
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(activeProgram, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform3(location, uniformValue);
            CheckError();
        }

        private static unsafe void DrawModelObject(ModelObjectDescriptor modelObject)
        {
            Gl.BindVertexArray(modelObject.Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, modelObject.Indices);
            Gl.DrawElements(GLEnum.Triangles, (uint)modelObject.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetMatrix(Matrix4X4<float> mx, string uniformName)
        {
            int location = Gl.GetUniformLocation(activeProgram, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&mx);
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