using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;
using StbImageSharp;

namespace PirateShootingGame
{
    internal class ObjResourceReader
    {
        public static unsafe GlObject CreateFromObjFile(GL Gl, string path, float[] color)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices = new();
            List<int[]> objFaces = new();
            List<float[]> objNormals = new();
            List<float[]> objTexCoords = new();
            List<float[]> faceColors = new();
            List<(int v, int vt, int vn)[]> objFacesWithTextures = new();

            string currentMaterial = "";
            Dictionary<string, float[]> materialColors = new();
            Dictionary<string, string> materialTextures = new();

            Console.WriteLine($"Loading OBJ file: {path}");

            try
            {
                using (var reader = new StreamReader(path))
                {
                    int lineNumber = 0;
                    while (!reader.EndOfStream)
                    {
                        lineNumber++;
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 0) continue;

                        switch (parts[0])
                        {
                            case "v":
                                if (parts.Length >= 4)
                                {
                                    try
                                    {
                                        objVertices.Add(parts.Skip(1).Take(3).Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray());
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error parsing vertex at line {lineNumber}: {ex.Message}");
                                    }
                                }
                                break;
                            case "vt":
                                if (parts.Length >= 3)
                                {
                                    try
                                    {
                                        objTexCoords.Add(parts.Skip(1).Take(2).Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray());
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error parsing texture coord at line {lineNumber}: {ex.Message}");
                                    }
                                }
                                break;
                            case "vn":
                                if (parts.Length >= 4)
                                {
                                    try
                                    {
                                        objNormals.Add(parts.Skip(1).Take(3).Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray());
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error parsing normal at line {lineNumber}: {ex.Message}");
                                    }
                                }
                                break;
                            case "mtllib":
                                if (parts.Length >= 2)
                                {
                                    Console.WriteLine($"Found MTL reference: {parts[1]}");
                                    var objDirectory = Path.GetDirectoryName(path);
                                    var mtlFilePath = Path.Combine(objDirectory, parts[1]);
                                    Console.WriteLine($"Looking for MTL file at: {mtlFilePath}");
                                    Console.WriteLine($"MTL file exists: {File.Exists(mtlFilePath)}");
                                    LoadMaterialData(mtlFilePath, color, materialColors, materialTextures);
                                }
                                break;
                            case "usemtl":
                                if (parts.Length >= 2)
                                {
                                    currentMaterial = parts[1];
                                    Console.WriteLine($"Using material: {currentMaterial}");
                                    if (currentMaterial.ToLower().Contains("ground") || 
                                        currentMaterial.ToLower().Contains("plane") ||
                                        currentMaterial.ToLower().Contains("floor"))
                                    {
                                        Console.WriteLine($"Skipping ground material: {currentMaterial}");
                                    }
                                }
                                break;
                            case "f":
                                if (parts.Length >= 4)
                                {
                                    try
                                    {
                                        if (currentMaterial.ToLower().Contains("ground") || 
                                            currentMaterial.ToLower().Contains("plane") ||
                                            currentMaterial.ToLower().Contains("floor"))
                                        {
                                            continue;
                                        }

                                        var faceColor = materialColors.ContainsKey(currentMaterial) 
                                            ? materialColors[currentMaterial] 
                                            : color;

                                        if (!materialColors.ContainsKey(currentMaterial) && !string.IsNullOrEmpty(currentMaterial))
                                        {
                                            Console.WriteLine($"Unknown material: '{currentMaterial}' - using default color");
                                        }

                                        ParseFaceWithTextures(parts, objFacesWithTextures, faceColors, faceColor);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error parsing face at line {lineNumber}: {ex.Message}");
                                    }
                                }
                                break;
                            case "g":
                            case "s":
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading OBJ file {path}: {ex.Message}");
                throw;
            }

            
            TryFallbackMTLLoading(path, color, materialColors, materialTextures);

            Console.WriteLine($"Loaded OBJ: {objVertices.Count} vertices, {objTexCoords.Count} texture coords, {objFacesWithTextures.Count} faces");

            if (objVertices.Count == 0 || objFacesWithTextures.Count == 0)
            {
                Console.WriteLine("No valid geometry found in OBJ file!");
                return GlCube.CreateCubeWithFaceColors(Gl, color, color, color, color, color, color);
            }

            
            uint textureId = LoadFirstTexture(Gl, path, materialTextures);

            
            List<float> glVertices = new();
            List<float> glColors = new();
            List<uint> glIndices = new();

            CreateGlArraysFromObjArraysWithTextures(faceColors, objVertices, objFacesWithTextures, objNormals, objTexCoords, glVertices, glColors, glIndices);
            
            if (glIndices.Count == 0)
            {
                Console.WriteLine("Warning: No valid geometry found in OBJ file");
                return GlCube.CreateCubeWithFaceColors(Gl, color, color, color, color, color, color);
            }

            Console.WriteLine($"Created GL object with {glIndices.Count} indices, texture: {textureId != 0}");
            return CreateOpenGlObjectWithTexture(Gl, vao, glVertices, glColors, glIndices, textureId);
        }

        private static void ParseFaceWithTextures(string[] parts, List<(int v, int vt, int vn)[]> objFacesWithTextures, List<float[]> faceColors, float[] faceColor)
        {
            List<(int v, int vt, int vn)> faceIndices = new();
            
            for (int i = 1; i < parts.Length; i++)
            {
                var tokens = parts[i].Split('/');
                if (tokens.Length >= 1 && int.TryParse(tokens[0], out int vertexIndex))
                {
                    int textureIndex = 0;
                    int normalIndex = 0;

                    if (tokens.Length >= 2 && !string.IsNullOrEmpty(tokens[1]))
                        int.TryParse(tokens[1], out textureIndex);
                    if (tokens.Length >= 3 && !string.IsNullOrEmpty(tokens[2]))
                        int.TryParse(tokens[2], out normalIndex);

                    faceIndices.Add((vertexIndex, textureIndex, normalIndex));
                }
            }
            
            
            if (faceIndices.Count >= 3)
            {
                for (int i = 1; i < faceIndices.Count - 1; i++)
                {
                    var triangle = new (int v, int vt, int vn)[] { 
                        faceIndices[0], 
                        faceIndices[i], 
                        faceIndices[i + 1] 
                    };
                    objFacesWithTextures.Add(triangle);
                    faceColors.Add(faceColor);
                }
            }
        }

        private static void TryFallbackMTLLoading(string objPath, float[] color, Dictionary<string, float[]> materialColors, Dictionary<string, string> materialTextures)
        {
            Console.WriteLine($"Material colors loaded: {materialColors.Count}");
            if (materialColors.Count == 0)
            {
                Console.WriteLine("No MTL reference found in OBJ file, trying to find MTL file by naming convention...");
                var objDirectory = Path.GetDirectoryName(objPath);
                var objNameWithoutExt = Path.GetFileNameWithoutExtension(objPath);
                var guessedMtlPath = Path.Combine(objDirectory, objNameWithoutExt + ".mtl");
                Console.WriteLine($"Looking for MTL file at: {guessedMtlPath}");
                Console.WriteLine($"MTL file exists: {File.Exists(guessedMtlPath)}");
                
                if (File.Exists(guessedMtlPath))
                {
                    LoadMaterialData(guessedMtlPath, color, materialColors, materialTextures);
                }
                else
                {
                    Console.WriteLine("No MTL file found, using default colors");
                }
            }
            else
            {
                Console.WriteLine($"Using {materialColors.Count} materials loaded from MTL file");
            }
        }

        private static void LoadMaterialData(string mtlFilePath, float[] defaultColor, Dictionary<string, float[]> materialColors, Dictionary<string, string> materialTextures)
        {
            if (!File.Exists(mtlFilePath))
            {
                Console.WriteLine($"MTL file not found: {mtlFilePath}");
                return;
            }

            Console.WriteLine($"Loading MTL file: {mtlFilePath}");

            try
            {
                string currentMaterial = "";
                using (var reader = new StreamReader(mtlFilePath))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                        
                        line = line.Trim();

                        
                        Console.WriteLine($"Processing MTL line: '{line}'");

                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 0) continue;

                        switch (parts[0])
                        {
                            case "newmtl":
                                if (parts.Length >= 2)
                                {
                                    currentMaterial = parts[1];
                                    Console.WriteLine($"Found material definition: {currentMaterial}");
                                    
                                    if (currentMaterial.ToLower().Contains("cloth"))
                                        materialColors[currentMaterial] = new float[] { 0.8f, 0.2f, 0.2f, 1.0f }; 
                                    else if (currentMaterial.ToLower().Contains("body") || currentMaterial.ToLower().Contains("skin"))
                                        materialColors[currentMaterial] = new float[] { 0.9f, 0.7f, 0.5f, 1.0f }; 
                                    else if (currentMaterial.ToLower().Contains("bark"))
                                        materialColors[currentMaterial] = new float[] { 0.4f, 0.2f, 0.1f, 1.0f }; 
                                    else
                                        materialColors[currentMaterial] = new float[] { 0.8f, 0.6f, 0.4f, 1.0f }; 
                                }
                                break;
                            case "Kd":
                                if (parts.Length >= 4 && !string.IsNullOrEmpty(currentMaterial))
                                {
                                    ParseDiffuseColor(parts, currentMaterial, materialColors);
                                }
                                break;
                            case "map_Kd":
                                if (parts.Length >= 2 && !string.IsNullOrEmpty(currentMaterial))
                                {
                                    string textureFile = parts[1];
                                    materialTextures[currentMaterial] = textureFile;
                                    Console.WriteLine($"Material '{currentMaterial}' texture file: '{textureFile}'");
                                    
                                    
                                    var mtlDirectory = Path.GetDirectoryName(mtlFilePath);
                                    var fullTexturePath = Path.Combine(mtlDirectory, textureFile);
                                    Console.WriteLine($"Full texture path: '{fullTexturePath}'");
                                    Console.WriteLine($"Texture file exists: {File.Exists(fullTexturePath)}");
                                }
                                else
                                {
                                    Console.WriteLine($"Invalid map_Kd line: '{line}' - parts.Length: {parts.Length}, currentMaterial: '{currentMaterial}'");
                                }
                                break;
                            case "Ka":
                                if (parts.Length >= 4 && !string.IsNullOrEmpty(currentMaterial) && !materialColors.ContainsKey(currentMaterial))
                                {
                                    ParseAmbientColor(parts, currentMaterial, materialColors);
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading MTL file {mtlFilePath}: {ex.Message}");
            }

            Console.WriteLine($"Loaded {materialColors.Count} materials and {materialTextures.Count} textures from MTL file");
        }

        private static void ParseDiffuseColor(string[] parts, string currentMaterial, Dictionary<string, float[]> materialColors)
        {
            try
            {
                float r = float.Parse(parts[1], CultureInfo.InvariantCulture);
                float g = float.Parse(parts[2], CultureInfo.InvariantCulture);
                float b = float.Parse(parts[3], CultureInfo.InvariantCulture);
                
                if (r == 0f && g == 0f && b == 0f)
                {
                    r = g = b = 0.5f;
                    Console.WriteLine($"Material {currentMaterial}: Converting black to medium gray");
                }
                
                
                materialColors[currentMaterial] = new float[] { r, g, b, 1.0f };
                Console.WriteLine($"Material {currentMaterial} diffuse color: ({r:F2}, {g:F2}, {b:F2})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing diffuse color for {currentMaterial}: {ex.Message}");
            }
        }

        private static void ParseAmbientColor(string[] parts, string currentMaterial, Dictionary<string, float[]> materialColors)
        {
            try
            {
                float r = float.Parse(parts[1], CultureInfo.InvariantCulture);
                float g = float.Parse(parts[2], CultureInfo.InvariantCulture);
                float b = float.Parse(parts[3], CultureInfo.InvariantCulture);
                materialColors[currentMaterial] = new float[] { r, g, b, 1.0f };
                Console.WriteLine($"Material {currentMaterial} ambient color: ({r:F2}, {g:F2}, {b:F2})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing ambient color for {currentMaterial}: {ex.Message}");
            }
        }

        private static uint LoadFirstTexture(GL gl, string objPath, Dictionary<string, string> materialTextures)
        {
            Console.WriteLine($"LoadFirstTexture called with {materialTextures.Count} textures available:");
            foreach (var kvp in materialTextures)
            {
                Console.WriteLine($"  Material: '{kvp.Key}' -> Texture: '{kvp.Value}'");
                
                
                var objDirectory = Path.GetDirectoryName(objPath);
                var texturePath = Path.Combine(objDirectory, kvp.Value);
                var textureId = LoadTexture(gl, texturePath);
                Console.WriteLine($"  → Loaded texture ID: {textureId} for material {kvp.Key}");
            }

            
            string textureFile = materialTextures.Values.FirstOrDefault();
            if (string.IsNullOrEmpty(textureFile))
            {
                Console.WriteLine("No texture files found in materials");
                return 0;
            }

            var objDir = Path.GetDirectoryName(objPath);
            var fullPath = Path.Combine(objDir, textureFile);
            return LoadTexture(gl, fullPath);
        }

        private static unsafe uint LoadTexture(GL gl, string texturePath)
        {
            Console.WriteLine($"LoadTexture called with path: '{texturePath}'");
            
            if (!File.Exists(texturePath))
            {
                Console.WriteLine($"ERROR: Texture file not found at: '{texturePath}'");
                
                
                var directory = Path.GetDirectoryName(texturePath);
                var fileName = Path.GetFileName(texturePath);
                
                if (Directory.Exists(directory))
                {
                    Console.WriteLine($"Files in directory '{directory}':");
                    try
                    {
                        var files = Directory.GetFiles(directory);
                        foreach (var file in files)
                        {
                            Console.WriteLine($"  - {Path.GetFileName(file)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error listing directory: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Directory does not exist: '{directory}'");
                }
                
                return 0;
            }

            try
            {
                Console.WriteLine($"File exists, attempting to load: {texturePath}");
                
                StbImage.stbi_set_flip_vertically_on_load(1);
                
                using var stream = File.OpenRead(texturePath);
                Console.WriteLine($"File opened successfully, stream length: {stream.Length}");
                
                var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                Console.WriteLine($"Image loaded: {image.Width}x{image.Height}, {image.Comp} components");
                
                uint textureId = gl.GenTexture();
                gl.BindTexture(TextureTarget.Texture2D, textureId);

                fixed (byte* dataPtr = image.Data)
                {
                    gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, 
                                 (uint)image.Width, (uint)image.Height, 0, 
                                 PixelFormat.Rgba, PixelType.UnsignedByte, dataPtr);
                }

                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
                gl.GenerateMipmap(TextureTarget.Texture2D);

                Console.WriteLine($"SUCCESS: Texture loaded with ID: {textureId}");
                return textureId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR loading texture {texturePath}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 0;
            }
        }

        private static byte[] CreatePlaceholderTexture(int width, int height)
        {
            byte[] data = new byte[width * height * 3];
            for (int i = 0; i < data.Length; i += 3)
            {
                
                int x = (i / 3) % width;
                int y = (i / 3) / width;
                bool checker = ((x / 32) + (y / 32)) % 2 == 0;
                
                data[i] = checker ? (byte)200 : (byte)100;     
                data[i + 1] = checker ? (byte)200 : (byte)100; 
                data[i + 2] = checker ? (byte)200 : (byte)100; 
            }
            return data;
        }

        private static unsafe GlObject CreateOpenGlObjectWithTexture(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices, uint textureId)
        {
            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint offsetTexCoord = offsetNormal + (3 * sizeof(float));
            uint vertexSize = offsetTexCoord + (2 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);
            
            
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            
            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            
            Gl.EnableVertexAttribArray(3);
            Gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetTexCoord);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            
            return new GlObject(vao, vertices, colors, indices, (uint)glIndices.Count, Gl, textureId);
        }

        private static unsafe void CreateGlArraysFromObjArraysWithTextures(List<float[]> faceColors, List<float[]> objVertices, List<(int v, int vt, int vn)[]> objFacesWithTextures, List<float[]> objNormals, List<float[]> objTexCoords, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, int> glVertexIndices = new();

            for (int faceIndex = 0; faceIndex < objFacesWithTextures.Count; faceIndex++)
            {
                var face = objFacesWithTextures[faceIndex];
                var faceColor = faceIndex < faceColors.Count ? faceColors[faceIndex] : new float[] {0.8f, 0.4f, 0.2f, 1f};
                
                for (int i = 0; i < 3; i++)
                {
                    var vertexIndex = face[i].v;
                    var texCoordIndex = face[i].vt;
                    var normalIndex = face[i].vn;

                    if (vertexIndex > 0 && vertexIndex <= objVertices.Count)
                    {
                        var vertex = objVertices[vertexIndex - 1];
                        
                        
                        float[] normal;
                        if (normalIndex > 0 && normalIndex <= objNormals.Count)
                        {
                            normal = objNormals[normalIndex - 1];
                        }
                        else
                        {
                            normal = new float[] { 0, 1, 0 }; 
                        }

                        
                        float[] texCoord;
                        if (texCoordIndex > 0 && texCoordIndex <= objTexCoords.Count)
                        {
                            texCoord = objTexCoords[texCoordIndex - 1];
                        }
                        else
                        {
                            texCoord = new float[] { 0.0f, 0.0f }; 
                        }

                        
                        var glVertex = new List<float>();
                        glVertex.AddRange(vertex);        
                        glVertex.AddRange(normal);        
                        glVertex.AddRange(texCoord);      

                        string key = string.Join(" ", glVertex) + "_" + string.Join("", faceColor);
                        if (!glVertexIndices.ContainsKey(key))
                        {
                            glVertices.AddRange(glVertex);
                            glColors.AddRange(faceColor);
                            glVertexIndices[key] = glVertexIndices.Count;
                        }

                        glIndices.Add((uint)glVertexIndices[key]);
                    }
                }
            }
        }
    }
}