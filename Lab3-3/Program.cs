using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Numerics;
using ImGuiNET;
using System.Runtime.InteropServices;

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
        private static Vector3D<float> lightColor = new Vector3D<float>(1.0f, 1.0f, 1.0f);
        private static Vector3D<float> lightPosition = new Vector3D<float>(3.0f, 3.0f, 3.0f);
        private static float shininess = 32.0f;
        private static Vector3D<float> ambientStrength = new Vector3D<float>(0.1f);
        private static Vector3D<float> diffuseStrength = new Vector3D<float>(0.3f);
        private static Vector3D<float> specularStrength = new Vector3D<float>(0.6f);
        private static Vector3D<float> backgroundColor = new Vector3D<float>(0.5f, 0.5f, 0.5f);

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
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(800, 600);
            options.Title = "Rubik's Cube with Phong Illumination";
            options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3));

            window = Window.Create(options);
            window.Load += OnLoad;
            window.Render += OnRenderFrame;
            window.Update += OnUpdateFrame;
            window.KeyDown += OnKeyDown;
            window.Closing += Window_Closing;
            window.Run();
        }

        private static void OnLoad()
        {
            Gl = GL.GetApi(window);
            imGuiController = new ImGuiController(Gl, window, window.CreateInput());

            Gl.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            Gl.Enable(EnableCap.DepthTest);

            // Load and compile shaders
            uint vertexShader = Gl.CreateShader(ShaderType.VertexShader);
            Gl.ShaderSource(vertexShader, VertexShaderSource);
            Gl.CompileShader(vertexShader);

            uint fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fragmentShader, FragmentShaderSource);
            Gl.CompileShader(fragmentShader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vertexShader);
            Gl.AttachShader(program, fragmentShader);
            Gl.LinkProgram(program);

            Gl.DeleteShader(vertexShader);
            Gl.DeleteShader(fragmentShader);

            SetUpRubikCube();
        }

        private static unsafe void SetViewMatrix()
        {
            Vector3D<float> cameraPos = cameraDescriptor.Position;
            Vector3D<float> cameraTarget = cameraDescriptor.Target;
            Vector3D<float> cameraUp = new Vector3D<float>(0, 1, 0);

            var viewMatrix = Matrix4X4.CreateLookAt(
                new Vector3(cameraPos.X, cameraPos.Y, cameraPos.Z),
                new Vector3(cameraTarget.X, cameraTarget.Y, cameraTarget.Z),
                new Vector3(cameraUp.X, cameraUp.Y, cameraUp.Z));

            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView(
                (float)(Math.PI / 4.0),
                (float)window.Size.X / window.Size.Y,
                0.1f,
                100.0f);

            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ProjectionMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
        }

        private static Vector3D<float> GetRotationAxis()
        {
            return sliceAxis switch
            {
                0 => Vector3D<float>.UnitX,
                1 => Vector3D<float>.UnitY,
                2 => Vector3D<float>.UnitZ,
                _ => Vector3D<float>.UnitY
            };
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

        private static void UpdateSliceRotation()
        {
            if (!isRotating) return;

            float delta = rotationSpeed * (float)window.RenderTime;
            if (Math.Abs(targetRotationAngle - currentRotationAngle) <= delta)
            {
                currentRotationAngle = targetRotationAngle;
                isRotating = false;
            }
            else
            {
                currentRotationAngle += delta * Math.Sign(targetRotationAngle - currentRotationAngle);
            }

            for (int i = 0; i < rubikPositions.Count; i++)
            {
                if (cubeInRotatingSlice[i])
                {
                    Vector3D<float> position = rubikPositions[i];
                    Vector3D<float> rotatedPosition = RotatePointAroundAxis(
                        position, 
                        Vector3D<float>.Zero, 
                        GetRotationAxis(), 
                        currentRotationAngle);
                    rubikPositions[i] = rotatedPosition;
                }
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

        private static void Window_Closing()
        {
            foreach (var cube in rubikCubes)
            {
                cube.ReleaseGlCube();
            }
            imGuiController?.Dispose();
            Gl.DeleteProgram(program);
        }
    }
}