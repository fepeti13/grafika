using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace PirateShootingGame
{
    internal class Skybox
    {
        private uint skyboxVAO;
        private uint skyboxVBO;
        private uint skyboxTexture;
        private uint skyboxProgram;
        private GL gl;

        private readonly string SkyboxVertexShader = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;

        out vec3 TexCoords;

        uniform mat4 projection;
        uniform mat4 view;

        void main()
        {
            TexCoords = aPos;
            vec4 pos = projection * view * vec4(aPos, 1.0);
            gl_Position = pos.xyww;  // Trick to keep skybox at far plane
        }";

        private readonly string SkyboxFragmentShader = @"
        #version 330 core
        out vec4 FragColor;

        in vec3 TexCoords;

        uniform samplerCube skybox;

        void main()
        {    
            FragColor = texture(skybox, TexCoords);
        }";

        // Skybox cube vertices
        private readonly float[] skyboxVertices = {
            // positions          
            -1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

            -1.0f,  1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f
        };

        public unsafe Skybox(GL gl)
        {
            this.gl = gl;
            SetupSkybox();
            CreateSkyboxShader();
            CreateSkyboxTexture();
        }

        private unsafe void SetupSkybox()
        {
            skyboxVAO = gl.GenVertexArray();
            skyboxVBO = gl.GenBuffer();

            gl.BindVertexArray(skyboxVAO);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, skyboxVBO);
            
            fixed (float* vertices = skyboxVertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(skyboxVertices.Length * sizeof(float)), vertices, BufferUsageARB.StaticDraw);
            }

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);
        }

        private void CreateSkyboxShader()
        {
            uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
            gl.ShaderSource(vertexShader, SkyboxVertexShader);
            gl.CompileShader(vertexShader);

            uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
            gl.ShaderSource(fragmentShader, SkyboxFragmentShader);
            gl.CompileShader(fragmentShader);

            skyboxProgram = gl.CreateProgram();
            gl.AttachShader(skyboxProgram, vertexShader);
            gl.AttachShader(skyboxProgram, fragmentShader);
            gl.LinkProgram(skyboxProgram);

            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);
        }

        private unsafe void CreateSkyboxTexture()
        {
            skyboxTexture = gl.GenTexture();
            gl.BindTexture(TextureTarget.TextureCubeMap, skyboxTexture);

            // Load the actual skybox images
            LoadSkyboxImages();

            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);
        }

        private unsafe void LoadSkyboxImages()
        {
            // Define the face targets and corresponding file names
            var faceData = new[]
            {
                (Target: TextureTarget.TextureCubeMapPositiveX, File: "Daylight Box_Right.bmp"),   // Right
                (Target: TextureTarget.TextureCubeMapNegativeX, File: "Daylight Box_Left.bmp"),    // Left  
                (Target: TextureTarget.TextureCubeMapPositiveY, File: "Daylight Box_Top.bmp"),     // Top
                (Target: TextureTarget.TextureCubeMapNegativeY, File: "Daylight Box_Bottom.bmp"),  // Bottom
                (Target: TextureTarget.TextureCubeMapPositiveZ, File: "Daylight Box_Front.bmp"),   // Front
                (Target: TextureTarget.TextureCubeMapNegativeZ, File: "Daylight Box_Back.bmp")     // Back
            };

            foreach (var face in faceData)
            {
                try
                {
                    string filePath = Path.Combine("Resources", "Skybox", face.File);
                    
                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine($"Skybox image not found: {filePath}");
                        // Create a fallback colored face
                        CreateFallbackFace(face.Target);
                        continue;
                    }

                    Console.WriteLine($"Loading skybox face: {filePath}");
                    var imageData = LoadBMP(filePath);
                    
                    if (imageData != null)
                    {
                        fixed (byte* pixelPtr = imageData.Pixels)
                        {
                            gl.TexImage2D(face.Target, 0, InternalFormat.Rgb, imageData.Width, imageData.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, pixelPtr);
                        }
                        Console.WriteLine($"Successfully loaded: {face.File} ({imageData.Width}x{imageData.Height})");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to load BMP: {face.File}");
                        CreateFallbackFace(face.Target);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading skybox face {face.File}: {ex.Message}");
                    CreateFallbackFace(face.Target);
                }
            }
        }

        private unsafe void CreateFallbackFace(TextureTarget target)
        {
            // Create a simple colored fallback face
            int size = 256;
            byte[] pixels = new byte[size * size * 3];
            
            // Different colors for different faces for debugging
            byte r = 135, g = 206, b = 235; // Default sky blue
            
            switch (target)
            {
                case TextureTarget.TextureCubeMapPositiveY: r = 100; g = 149; b = 237; break; // Top - lighter
                case TextureTarget.TextureCubeMapNegativeY: r = 70; g = 130; b = 180; break;  // Bottom - darker
            }

            for (int i = 0; i < pixels.Length; i += 3)
            {
                pixels[i] = r;
                pixels[i + 1] = g;
                pixels[i + 2] = b;
            }

            fixed (byte* pixelPtr = pixels)
            {
                gl.TexImage2D(target, 0, InternalFormat.Rgb, (uint)size, (uint)size, 0, PixelFormat.Rgb, PixelType.UnsignedByte, pixelPtr);
            }
        }

        private BMPImageData? LoadBMP(string filePath)
        {
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(fileStream);

                // Read BMP header
                var signature = reader.ReadUInt16();
                if (signature != 0x4D42) // "BM" in little endian
                {
                    Console.WriteLine($"Invalid BMP signature in {filePath}");
                    return null;
                }

                reader.ReadUInt32(); // File size
                reader.ReadUInt32(); // Reserved
                var dataOffset = reader.ReadUInt32();

                // Read DIB header
                var dibHeaderSize = reader.ReadUInt32();
                var width = reader.ReadInt32();
                var height = reader.ReadInt32();
                reader.ReadUInt16(); // Planes
                var bitsPerPixel = reader.ReadUInt16();
                var compression = reader.ReadUInt32();

                if (bitsPerPixel != 24 || compression != 0)
                {
                    Console.WriteLine($"Unsupported BMP format in {filePath}: {bitsPerPixel}bpp, compression={compression}");
                    return null;
                }

                // Skip to pixel data
                fileStream.Seek(dataOffset, SeekOrigin.Begin);

                // Calculate row padding
                int rowPadding = (4 - (width * 3) % 4) % 4;
                var pixels = new byte[width * height * 3];

                // Read pixel data (BMP is stored bottom-to-top)
                for (int y = height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // BMP stores as BGR, we need RGB
                        byte b = reader.ReadByte();
                        byte g = reader.ReadByte();
                        byte r = reader.ReadByte();

                        int index = (y * width + x) * 3;
                        pixels[index] = r;
                        pixels[index + 1] = g;
                        pixels[index + 2] = b;
                    }
                    
                    // Skip row padding
                    for (int p = 0; p < rowPadding; p++)
                    {
                        reader.ReadByte();
                    }
                }

                return new BMPImageData
                {
                    Width = (uint)Math.Abs(width),
                    Height = (uint)Math.Abs(height),
                    Pixels = pixels
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception loading BMP {filePath}: {ex.Message}");
                return null;
            }
        }

        private class BMPImageData
        {
            public uint Width { get; set; }
            public uint Height { get; set; }
            public byte[] Pixels { get; set; } = Array.Empty<byte>();
        }

        public unsafe void Render(Matrix4X4<float> view, Matrix4X4<float> projection)
        {
            // Change depth function so depth test passes when values are equal to depth buffer's content
            gl.DepthFunc(DepthFunction.Lequal);
            
            gl.UseProgram(skyboxProgram);
            
            // Remove translation from view matrix (keep only rotation)
            var skyboxView = new Matrix4X4<float>(
                view.M11, view.M12, view.M13, 0,
                view.M21, view.M22, view.M23, 0,
                view.M31, view.M32, view.M33, 0,
                0, 0, 0, 1
            );

            int viewLoc = gl.GetUniformLocation(skyboxProgram, "view");
            gl.UniformMatrix4(viewLoc, 1, false, (float*)&skyboxView);

            int projLoc = gl.GetUniformLocation(skyboxProgram, "projection");
            gl.UniformMatrix4(projLoc, 1, false, (float*)&projection);

            // Skybox cube
            gl.BindVertexArray(skyboxVAO);
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.TextureCubeMap, skyboxTexture);
            gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
            gl.BindVertexArray(0);

            // Set depth function back to default
            gl.DepthFunc(DepthFunction.Less);
        }

        public void Dispose()
        {
            gl.DeleteVertexArray(skyboxVAO);
            gl.DeleteBuffer(skyboxVBO);
            gl.DeleteTexture(skyboxTexture);
            gl.DeleteProgram(skyboxProgram);
        }
    }
}