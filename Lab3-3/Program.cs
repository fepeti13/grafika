using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Numerics;

namespace Lab2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();
        private static CubeArrangementModel cubeArrangementModel = new();
        
        private static IWindow window;
        private static GL Gl;
        private static ImGuiController imGuiController;
        private static uint program;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string SliceRotationMatrixVariableName = "uSliceRotation";
        
        // Lighting uniforms
        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string ShinenessVariableName = "uShininess";
        private const string AmbientStrengthVariableName = "uAmbientStrength";
        private const string DiffuseStrengthVariableName = "uDiffuseStrength";
        private const string SpecularStrengthVariableName = "uSpecularStrength";

        private static List<GlCube> rubikCubes = new();
        private static List<Vector3D<float>> rubikPositions = new();
        private static List<bool> cubeInRotatingSlice = new();
        
        private static int currentSlice = 1;
        private static int sliceAxis = 1;
        private static bool isRotating = false;
        private static float currentRotationAngle = 0.0f;
        private static float targetRotationAngle = 0.0f;
        private static float rotationSpeed = 3.0f;

        // Lighting parameters
        private static Vector3 lightColor = new Vector3(1.0f, 1.0f, 1.0f);
        private static Vector3 lightPosition = new Vector3(3.0f, 3.0f, 3.0f);
        private static float shininess = 32.0f;
        private static Vector3 ambientStrength = new Vector3(0.1f);
        private static Vector3 diffuseStrength = new Vector3(0.3f);
        private static Vector3 specularStrength = new Vector3(0.6f);
        private static Vector3 backgroundColor = new Vector3(0.5f, 0.5f, 0.5f);

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNormal;

        uniform mat4 uModel;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uSliceRotation;

        out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;
        
        void main()
        {
            outCol = vCol;
            vec4 worldPos = uSliceRotation * uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0);
            outWorldPosition = vec3(worldPos);
            outNormal = uNormal * vNormal;
            gl_Position = uProjection * uView * worldPos;
        }
        ";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

        uniform vec3 uLightColor;
        uniform vec3 uLightPos;
        uniform vec3 uViewPos;
        uniform float uShininess;
        uniform vec3 uAmbientStrength;
        uniform vec3 uDiffuseStrength;
        uniform vec3 uSpecularStrength;

        in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        void main()
        {
            // Ambient component
            vec3 ambient = uAmbientStrength * uLightColor;

            // Diffuse component
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(uLightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * uLightColor * uDiffuseStrength;

            // Specular component
            vec3 viewDir = normalize(uViewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
            vec3 specular = uSpecularStrength * spec * uLightColor;

            // Combining all lighting components
            vec3 result = (ambient + diffuse + specular) * outCol.rgb;

            FragColor = vec4(result, outCol.w);
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Rubik's Cube with Phong Shading";
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
            Gl.ClearColor(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1.0f);

            // Initialize ImGui
            imGuiController = new ImGuiController(Gl, window, inputContext);

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
            // Update ImGui controller
            imGuiController.Update((float)deltaTime);
            
            // Update cube rotation animation
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
            Gl.ClearColor(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1.0f);

            Gl.UseProgram(program);

            // Set lighting and camera related uniforms
            SetUniform3(LightColorVariableName, lightColor);
            SetUniform3(LightPositionVariableName, lightPosition);
            SetUniform3(ViewPositionVariableName, new Vector3(
                cameraDescriptor.Position.X, 
                cameraDescriptor.Position.Y, 
                cameraDescriptor.Position.Z));
            SetUniform1(ShinenessVariableName, shininess);
            SetUniform3(AmbientStrengthVariableName, ambientStrength);
            SetUniform3(DiffuseStrengthVariableName, diffuseStrength);
            SetUniform3(SpecularStrengthVariableName, specularStrength);

            SetViewMatrix();
            SetProjectionMatrix();

            for (int i = 0; i < rubikCubes.Count; i++)
            {
                var cube = rubikCubes[i];
                var position = rubikPositions[i];
                bool inSlice = cubeInRotatingSlice[i];
                
                Gl.BindVertexArray(cube.Vao);

                // Set model matrix
                var modelMatrix = Matrix4X4.CreateTranslation(position);
                int modelLocation = Gl.GetUniformLocation(program, ModelMatrixVariableName);
                if (modelLocation == -1)
                {
                    throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
                }
                Gl.UniformMatrix4(modelLocation, 1, false, (float*)&modelMatrix);

                // Set normal matrix (for lighting calculations)
                var normalMatrix = new Matrix3X3<float>(
                    modelMatrix.M11, modelMatrix.M12, modelMatrix.M13,
                    modelMatrix.M21, modelMatrix.M22, modelMatrix.M23,
                    modelMatrix.M31, modelMatrix.M32, modelMatrix.M33);
                normalMatrix = Matrix3X3.Transpose(Matrix3X3.Invert(normalMatrix));
                
                int normalLocation = Gl.GetUniformLocation(program, NormalMatrixVariableName);
                if (normalLocation == -1)
                {
                    throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
                }
                Gl.UniformMatrix3(normalLocation, 1, false, (float*)&normalMatrix);

                // Set slice rotation matrix
                Matrix4X4<float> sliceRotationMatrix = Matrix4X4<float>.Identity;
                if (inSlice && isRotating)
                {
                    Vector3D<float> axis = GetRotationAxis();
                    if (axis.X == 1)
                        sliceRotationMatrix = Matrix4X4.CreateRotationX(currentRotationAngle);
                    else if (axis.Y == 1)
                        sliceRotationMatrix = Matrix4X4.CreateRotationY(currentRotationAngle);
                    else
                        sliceRotationMatrix = Matrix4X4.CreateRotationZ(currentRotationAngle);
                }
                
                int sliceRotationLocation = Gl.GetUniformLocation(program, SliceRotationMatrixVariableName);
                if (sliceRotationLocation == -1)
                {
                    throw new Exception($"{SliceRotationMatrixVariableName} uniform not found on shader.");
                }
                Gl.UniformMatrix4(sliceRotationLocation, 1, false, (float*)&sliceRotationMatrix);

                Gl.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, null);
            }

            // Render ImGui
            RenderImGui();

            imGuiController.Render();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = cameraDescriptor.GetViewMatrix();
            int viewLocation = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            if (viewLocation == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix4(viewLocation, 1, false, (float*)&viewMatrix);
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = cameraDescriptor.GetProjectionMatrix();
            int projectionLocation = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            if (projectionLocation == -1)
            {
                throw new Exception($"{ProjectionMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix4(projectionLocation, 1, false, (float*)&projectionMatrix);
        }

        private static void RenderImGui()
        {
            ImGuiNET.ImGui.Begin("Rubik's Cube Controls");
            
            // Light color controls
            ImGuiNET.ImGui.Text("Light Color:");
            
            float[] lightColorArray = new float[] { lightColor.X, lightColor.Y, lightColor.Z };
            if (ImGuiNET.ImGui.SliderFloat3("RGB", ref lightColorArray[0], 0.0f, 1.0f))
            {
                lightColor = new Vector3(lightColorArray[0], lightColorArray[1], lightColorArray[2]);
            }
            
            // Light position controls
            ImGuiNET.ImGui.Text("Light Position:");
            
            float[] lightPosArray = new float[] { lightPosition.X, lightPosition.Y, lightPosition.Z };
            if (ImGuiNET.ImGui.InputFloat3("XYZ", ref lightPosArray[0], "%.1f"))
            {
                lightPosition = new Vector3(lightPosArray[0], lightPosArray[1], lightPosArray[2]);
            }
            
            // Phong model parameters
            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Phong Shading Parameters:");
            
            float[] ambientArray = new float[] { ambientStrength.X, ambientStrength.Y, ambientStrength.Z };
            if (ImGuiNET.ImGui.SliderFloat3("Ambient", ref ambientArray[0], 0.0f, 1.0f))
            {
                ambientStrength = new Vector3(ambientArray[0], ambientArray[1], ambientArray[2]);
            }
            
            float[] diffuseArray = new float[] { diffuseStrength.X, diffuseStrength.Y, diffuseStrength.Z };
            if (ImGuiNET.ImGui.SliderFloat3("Diffuse", ref diffuseArray[0], 0.0f, 1.0f))
            {
                diffuseStrength = new Vector3(diffuseArray[0], diffuseArray[1], diffuseArray[2]);
            }
            
            float[] specularArray = new float[] { specularStrength.X, specularStrength.Y, specularStrength.Z };
            if (ImGuiNET.ImGui.SliderFloat3("Specular", ref specularArray[0], 0.0f, 1.0f))
            {
                specularStrength = new Vector3(specularArray[0], specularArray[1], specularArray[2]);
            }
            
            ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 1.0f, 128.0f);
            
            // Navigation buttons
            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Slice Selection:");
            
            if (ImGuiNET.ImGui.Button("X Axis"))
            {
                sliceAxis = 0;
            }
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Y Axis"))
            {
                sliceAxis = 1;
            }
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Z Axis"))
            {
                sliceAxis = 2;
            }
            
            ImGuiNET.ImGui.Text("Slice Layer:");
            if (ImGuiNET.ImGui.Button("Layer -1"))
            {
                currentSlice = -1;
            }
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Layer 0"))
            {
                currentSlice = 0;
            }
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Layer 1"))
            {
                currentSlice = 1;
            }
            
            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Rotation:");
            if (ImGuiNET.ImGui.Button("Rotate CW") && !isRotating)
            {
                isRotating = true;
                currentRotationAngle = 0.0f;
                targetRotationAngle = (float)Math.PI / 2.0f;
                UpdateSliceMembers();
            }
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Rotate CCW") && !isRotating)
            {
                isRotating = true;
                currentRotationAngle = 0.0f;
                targetRotationAngle = -(float)Math.PI / 2.0f;
                UpdateSliceMembers();
            }
            
            // Camera controls
            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Camera Controls:");
            if (ImGuiNET.ImGui.Button("Zoom In"))
            {
                cameraDescriptor.DecreaseDistance();
            }
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Zoom Out"))
            {
                cameraDescriptor.IncreaseDistance();
            }
            
            if (ImGuiNET.ImGui.Button("Rotate Left"))
            {
                cameraDescriptor.DecreaseZYAngle();
            }
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Rotate Right"))
            {
                cameraDescriptor.IncreaseZYAngle();
            }
            
            if (ImGuiNET.ImGui.Button("Rotate Up"))
            {
                cameraDescriptor.IncreaseZXAngle();
            }
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Rotate Down"))
            {
                cameraDescriptor.DecreaseZXAngle();
            }

            ImGuiNET.ImGui.End();
        }

        private static void SetUniform1(string name, float value)
        {
            int location = Gl.GetUniformLocation(program, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            Gl.Uniform1(location, value);
        }

        private static void SetUniform3(string name, Vector3 value)
        {
            int location = Gl.GetUniformLocation(program, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            Gl.Uniform3(location, value.X, value.Y, value.Z);
        }

        private static void Window_Closing()
        {
            foreach (var cube in rubikCubes)
            {
                cube.Dispose();
            }
            
            Gl.DeleteProgram(program);
            imGuiController?.Dispose();
        }
    }
}