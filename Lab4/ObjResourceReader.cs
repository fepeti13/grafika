using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;

namespace Szeminarium1_24_02_17_2
{
    internal class ObjResourceReader
    {
        public static unsafe GlObject CreateTeapotWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFaces;
            List<float[]> objNormals;
            List<(int v, int vn)[]> objFacesWithNormals;

            ReadObjDataForTeapot(out objVertices, out objFaces, out objNormals, out objFacesWithNormals);

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objFaces, objFacesWithNormals, objNormals, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        public static unsafe GlObject CreateFromObjFile(GL Gl, string path, float[] color)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices = new();
            List<int[]> objFaces = new();
            List<float[]> objNormals = new();
            List<(int v, int vn)[]> objFacesWithNormals = new();

            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts[0] == "v")
                    {
                        objVertices.Add(parts.Skip(1).Take(3).Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray());
                    }
                    else if (parts[0] == "vn")
                    {
                        objNormals.Add(parts.Skip(1).Take(3).Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray());
                    }
                    else if (parts[0] == "f")
                    {
                        if (parts[1].Contains("//"))
                        {
                            var face = new (int v, int vn)[3];
                            for (int i = 0; i < 3; i++)
                            {
                                var tokens = parts[i + 1].Split("//");
                                face[i] = (int.Parse(tokens[0]), int.Parse(tokens[1]));
                            }
                            objFacesWithNormals.Add(face);
                        }
                        else if (!parts[1].Contains("/"))
                        {
                            objFaces.Add(parts.Skip(1).Take(3).Select(s => int.Parse(s)).ToArray());
                        }
                    }
                }
            }

            List<float> glVertices = new();
            List<float> glColors = new();
            List<uint> glIndices = new();

            CreateGlArraysFromObjArrays(color, objVertices, objFaces, objFacesWithNormals, objNormals, glVertices, glColors, glIndices);
            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
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

        private static unsafe void CreateGlArraysFromObjArrays(float[] faceColor, List<float[]> objVertices, List<int[]> objFaces, List<(int v, int vn)[]> objFacesWithNormals, List<float[]> objNormals, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, int> glVertexIndices = new();

            if (objFacesWithNormals.Count > 0)
            {
                foreach (var face in objFacesWithNormals)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var v = objVertices[face[i].v - 1];
                        var n = objNormals[face[i].vn - 1];

                        var glVertex = new List<float>(v.Concat(n));
                        string key = string.Join(" ", glVertex);

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
            else
            {
                foreach (var face in objFaces)
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

                        string key = string.Join(" ", glVertex);
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

        private static unsafe void ReadObjDataForTeapot(out List<float[]> objVertices, out List<int[]> objFaces, out List<float[]> objNormals, out List<(int v, int vn)[]> objFacesWithNormals)
        {
            objVertices = new();
            objFaces = new();
            objNormals = new();
            objFacesWithNormals = new();

            using var stream = typeof(ObjResourceReader).Assembly.GetManifestResourceStream("Szeminarium1_24_02_17_2.Resources.teapot.obj");
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                switch (parts[0])
                {
                    case "v":
                        objVertices.Add(parts.Skip(1).Take(3).Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray());
                        break;
                    case "vn":
                        objNormals.Add(parts.Skip(1).Take(3).Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray());
                        break;
                    case "f":
                        if (parts[1].Contains("//"))
                        {
                            var face = new (int v, int vn)[3];
                            for (int i = 0; i < 3; i++)
                            {
                                var tokens = parts[i + 1].Split("//");
                                face[i] = (int.Parse(tokens[0]), int.Parse(tokens[1]));
                            }
                            objFacesWithNormals.Add(face);
                        }
                        else if (!parts[1].Contains("/"))
                        {
                            objFaces.Add(parts.Skip(1).Take(3).Select(s => int.Parse(s)).ToArray());
                        }
                        break;
                }
            }
        }
    }
}
