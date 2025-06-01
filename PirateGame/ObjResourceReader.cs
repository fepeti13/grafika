using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;

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
            List<float[]> faceColors = new(); 

            string currentMaterial = "";
            Dictionary<string, float[]> materialColors = new();
            string mtlFilePath = "";

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
                                    mtlFilePath = Path.Combine(objDirectory, parts[1]);
                                    Console.WriteLine($"Looking for MTL file at: {mtlFilePath}");
                                    Console.WriteLine($"MTL file exists: {File.Exists(mtlFilePath)}");
                                    materialColors = LoadMaterialColors(mtlFilePath, color);
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

                                        
                                        List<int> vertexIndices = new List<int>();
                                        
                                        for (int i = 1; i < parts.Length; i++)
                                        {
                                            if (parts[i].Contains("/"))
                                            {
                                                
                                                var tokens = parts[i].Split('/');
                                                if (tokens.Length >= 1 && int.TryParse(tokens[0], out int vertexIndex))
                                                {
                                                    vertexIndices.Add(vertexIndex);
                                                }
                                            }
                                            else
                                            {
                                                
                                                if (int.TryParse(parts[i], out int vertexIndex))
                                                {
                                                    vertexIndices.Add(vertexIndex);
                                                }
                                            }
                                        }
                                        
                                        
                                        if (vertexIndices.Count >= 3)
                                        {
                                            for (int i = 1; i < vertexIndices.Count - 1; i++)
                                            {
                                                objFaces.Add(new int[] { vertexIndices[0], vertexIndices[i], vertexIndices[i + 1] });
                                                faceColors.Add(faceColor); 
                                            }
                                        }
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

            
            Console.WriteLine($"Material colors loaded: {materialColors.Count}");
            if (materialColors.Count == 0)
            {
                Console.WriteLine("No MTL reference found in OBJ file, trying to find MTL file by naming convention...");
                var objDirectory = Path.GetDirectoryName(path);
                var objNameWithoutExt = Path.GetFileNameWithoutExtension(path);
                var guessedMtlPath = Path.Combine(objDirectory, objNameWithoutExt + ".mtl");
                Console.WriteLine($"Looking for MTL file at: {guessedMtlPath}");
                Console.WriteLine($"MTL file exists: {File.Exists(guessedMtlPath)}");
                
                if (File.Exists(guessedMtlPath))
                {
                    materialColors = LoadMaterialColors(guessedMtlPath, color);
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

            
            Console.WriteLine($"Loaded OBJ: {objVertices.Count} vertices, {objFaces.Count} faces");

            if (objVertices.Count == 0)
            {
                Console.WriteLine("No vertices found in OBJ file!");
                throw new Exception("No vertices found in OBJ file");
            }

            if (objFaces.Count == 0)
            {
                Console.WriteLine("No faces found in OBJ file!");
                throw new Exception("No faces found in OBJ file");
            }

            List<float> glVertices = new();
            List<float> glColors = new();
            List<uint> glIndices = new();

            CreateGlArraysFromObjArrays(faceColors, objVertices, objFaces, objNormals, glVertices, glColors, glIndices);
            
            if (glIndices.Count == 0)
            {
                Console.WriteLine("Warning: No valid geometry found in OBJ file");
                
                return GlCube.CreateCubeWithFaceColors(Gl, color, color, color, color, color, color);
            }

            Console.WriteLine($"Created GL object with {glIndices.Count} indices");
            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static Dictionary<string, float[]> LoadMaterialColors(string mtlFilePath, float[] defaultColor)
        {
            var materialColors = new Dictionary<string, float[]>();
            
            if (!File.Exists(mtlFilePath))
            {
                Console.WriteLine($"MTL file not found: {mtlFilePath}");
                return materialColors;
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

                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 0) continue;

                        switch (parts[0])
                        {
                            case "newmtl":
                                if (parts.Length >= 2)
                                {
                                    currentMaterial = parts[1];
                                    Console.WriteLine($"Found material definition: {currentMaterial}");
                                    
                                    materialColors[currentMaterial] = new float[] { 0.7f, 0.7f, 0.7f, 1.0f }; 
                                }
                                break;
                            case "Kd": 
                                if (parts.Length >= 4 && !string.IsNullOrEmpty(currentMaterial))
                                {
                                    try
                                    {
                                        float r = float.Parse(parts[1], CultureInfo.InvariantCulture);
                                        float g = float.Parse(parts[2], CultureInfo.InvariantCulture);
                                        float b = float.Parse(parts[3], CultureInfo.InvariantCulture);
                                        
                                        
                                        if (r == 0f && g == 0f && b == 0f)
                                        {
                                            
                                            r = g = b = 0.3f;
                                            Console.WriteLine($"Material {currentMaterial}: Converting black to dark gray");
                                        }
                                        else if (r == 1f && g == 1f && b == 1f)
                                        {
                                            
                                            r = g = b = 0.8f;
                                            Console.WriteLine($"Material {currentMaterial}: Converting white to light gray");
                                        }
                                        
                                        materialColors[currentMaterial] = new float[] { r, g, b, 1.0f };
                                        Console.WriteLine($"Material {currentMaterial} diffuse color: ({r:F2}, {g:F2}, {b:F2})");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error parsing diffuse color for {currentMaterial}: {ex.Message}");
                                    }
                                }
                                break;
                            case "Ka": 
                                if (parts.Length >= 4 && !string.IsNullOrEmpty(currentMaterial) && !materialColors.ContainsKey(currentMaterial))
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
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading MTL file {mtlFilePath}: {ex.Message}");
            }

            Console.WriteLine($"Loaded {materialColors.Count} materials from MTL file");
            return materialColors;
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint vertexSize = offsetNormal + (3 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            return new GlObject(vao, vertices, colors, indices, (uint)glIndices.Count, Gl);
        }

        private static unsafe void CreateGlArraysFromObjArrays(List<float[]> faceColors, List<float[]> objVertices, List<int[]> objFaces, List<float[]> objNormals, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, int> glVertexIndices = new();

            for (int faceIndex = 0; faceIndex < objFaces.Count; faceIndex++)
            {
                var face = objFaces[faceIndex];
                var faceColor = faceIndex < faceColors.Count ? faceColors[faceIndex] : new float[] {0.8f, 0.4f, 0.2f, 1f};
                
                
                if (face.All(index => index > 0 && index <= objVertices.Count))
                {
                    var aObjVertex = objVertices[face[0] - 1];
                    var a = new Vector3D<float>(aObjVertex[0], aObjVertex[1], aObjVertex[2]);
                    var bObjVertex = objVertices[face[1] - 1];
                    var b = new Vector3D<float>(bObjVertex[0], bObjVertex[1], bObjVertex[2]);
                    var cObjVertex = objVertices[face[2] - 1];
                    var c = new Vector3D<float>(cObjVertex[0], cObjVertex[1], cObjVertex[2]);
                    
                    var normal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));

                    for (int i = 0; i < 3; i++)
                    {
                        var v = objVertices[face[i] - 1];
                        var glVertex = new List<float>(v)
                        {
                            normal.X, normal.Y, normal.Z
                        };

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